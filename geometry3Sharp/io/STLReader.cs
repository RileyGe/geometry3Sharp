using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace g3
{
    /// <summary>
    /// Read ASCII/Binary STL file and produce set of meshes.
    /// 
    /// Since STL is just a list of disconnected triangles, by default we try to
    /// merge vertices together. Use .WeldStrategy to disable this and/or configure
    /// which algorithm is used. If you are using via StandardMeshReader, you can add
    /// .StrategyFlag to ReadOptions.CustomFlags to set this flag.
    /// 
    /// TODO: document welding strategies. There is no "best" one, they all fail
    /// in some cases, because STL is a stupid and horrible format.
    /// 
    /// STL Binary supports a per-triangle short-int that is usually used to specify color.
    /// However since we do not support per-triangle color in DMesh3, this color
    /// cannot be directly used. Instead of hardcoding behavior, we return the list of shorts
    /// if requested via IMeshBuilder Metadata. Set .WantPerTriAttribs=true or attach flag .PerTriAttribFlag.
    /// After the read finishes you can get the face color list via:
    ///    DVector<short> colors = Builder.Metadata[0][STLReader.PerTriAttribMetadataName] as DVector<short>;
    /// (for DMesh3Builder, which is the only builder that supports Metadata)
    /// </summary>
    public class STLReader : IMeshReader
    {
        /// <summary>
        /// Which algorithm is used to try to reconstruct mesh topology from STL triangle soup
        /// </summary>
        public VertexWelderStrategies WeldStrategy { get; set; } = VertexWelderStrategies.AutoBestResult;

        /// <summary>
        /// Vertices within this distance are considered "the same" by welding strategies.
        /// </summary>
        public double WeldTolerance { get; set; } = MathUtil.ZeroTolerancef;

        /// <summary>
        /// Binary STL supports per-triangle integer attribute, which is often used
        /// to store face colors. If this flag is true, we will attach these face
        /// colors to the returned mesh via IMeshBuilder.AppendMetaData
        /// </summary>
        public bool WantPerTriAttribs = false;

        /// <summary>
        /// name argument passed to IMeshBuilder.AppendMetaData
        /// </summary>
        public static string PerTriAttribMetadataName = "tri_attrib";


        /// <summary> connect to this event to get warning messages </summary>
		public event ParsingMessagesHandler warningEvent;


        //int nWarningLevel = 0;      // 0 == no diagnostics, 1 == basic, 2 == crazy
        Dictionary<string, int> warningCount = new Dictionary<string, int>();



        /// <summary> ReadOptions.CustomFlags flag for configuring .RebuildStrategy </summary>
        public const string StrategyFlag = "-stl-weld-strategy";

        /// <summary> ReadOptions.CustomFlags flag for configuring .WantPerTriAttribs </summary>
        public const string PerTriAttribFlag = "-want-tri-attrib";

        void ParseArguments(CommandArgumentSet args)
        {
            if ( args.Integers.ContainsKey(StrategyFlag) ) {
                WeldStrategy = (VertexWelderStrategies)args.Integers[StrategyFlag];
            }
            if (args.Flags.ContainsKey(PerTriAttribFlag)) {
                WantPerTriAttribs = true;
            }
        }




        protected class STLSolid
        {
            public string Name;
            public DVectorArray3f Vertices = new DVectorArray3f();
            public DVector<short> TriAttribs = null;
        }


        List<STLSolid> Objects;

        void append_vertex(float x, float y, float z)
        {
            Objects.Last().Vertices.Append(x, y, z);
        }

        public IOReadResult Read(BinaryReader reader, ReadOptions options, IMeshBuilder builder)
        {
            if ( options.CustomFlags != null )
                ParseArguments(options.CustomFlags);

            // Advance past header
            reader.ReadBytes(80);

            int totalTris = reader.ReadInt32();

            Objects = new List<STLSolid>();
            Objects.Add(new STLSolid());

            int tri_size = 50;      // bytes

            try
            {
                int nChunkSize = 1024;
                float ax, ay, az, bx, by, bz, cx, cy, cz;
                int nChunks = totalTris / nChunkSize;

                byte[] tri_bytes;
                int tri_start;

                for (int i = 0; i < nChunks; ++i)
                {
                    tri_bytes = reader.ReadBytes(tri_size * nChunkSize);

                    for (int k = 0; k < nChunkSize; ++k)
                    {
                        tri_start = tri_size * k;

                        ax = BitConverter.ToSingle(tri_bytes, tri_start + 12);
                        ay = BitConverter.ToSingle(tri_bytes, tri_start + 16);
                        az = BitConverter.ToSingle(tri_bytes, tri_start + 20);

                        bx = BitConverter.ToSingle(tri_bytes, tri_start + 24);
                        by = BitConverter.ToSingle(tri_bytes, tri_start + 28);
                        bz = BitConverter.ToSingle(tri_bytes, tri_start + 32);

                        cx = BitConverter.ToSingle(tri_bytes, tri_start + 36);
                        cy = BitConverter.ToSingle(tri_bytes, tri_start + 40);
                        cz = BitConverter.ToSingle(tri_bytes, tri_start + 44);

                        append_vertex(ax, ay, az);
                        append_vertex(bx, by, bz);
                        append_vertex(cx, cy, cz);
                    }
                }
            }
            catch (Exception e)
            {
                return new IOReadResult(IOCode.GenericReaderError, "exception: " + e.Message);
            }

            foreach (STLSolid solid in Objects)
                 BuildMesh(solid, builder);

            return new IOReadResult(IOCode.Ok, "");
        }




        public IOReadResult Read(TextReader reader, ReadOptions options, IMeshBuilder builder)
        {
            if ( options.CustomFlags != null )
                ParseArguments(options.CustomFlags);

            // format is just this, with facet repeated N times:
            //solid "stl_ascii"
            //  facet normal 0.722390830517 -0.572606861591 0.387650430202
            //    outer loop
            //      vertex 0.00659640412778 4.19127035141 -0.244179025292
            //      vertex -0.0458636470139 4.09951019287 -0.281960010529
            //      vertex 0.0286951716989 4.14693021774 -0.350856184959
            //    endloop
            //  endfacet
            //endsolid

            bool in_solid = false;
            //bool in_facet = false;
            //bool in_loop = false;
            //int vertices_in_loop = 0;

            Objects = new List<STLSolid>();

            int nLines = 0;
            while (reader.Peek() >= 0) {

                string line = reader.ReadLine();
                nLines++;
                string[] tokens = line.Split( (char[])null , StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                if (tokens[0].Equals("vertex", StringComparison.OrdinalIgnoreCase)) {
                    float x = (tokens.Length > 1) ? Single.Parse(tokens[1]) : 0;
                    float y = (tokens.Length > 2) ? Single.Parse(tokens[2]) : 0;
                    float z = (tokens.Length > 3) ? Single.Parse(tokens[3]) : 0;
                    append_vertex(x, y, z);

                // [RMS] we don't really care about these lines...
                //} else if (tokens[0].Equals("outer", StringComparison.OrdinalIgnoreCase)) {
                //    in_loop = true;
                //    vertices_in_loop = 0;

                //} else if (tokens[0].Equals("endloop", StringComparison.OrdinalIgnoreCase)) {
                //    in_loop = false;
                        

                } else if (tokens[0].Equals("facet", StringComparison.OrdinalIgnoreCase)) {
                    if ( in_solid == false ) {      // handle bad STL
                        Objects.Add(new STLSolid() { Name = "unknown_solid" });
                        in_solid = true;
                    }
                    //in_facet = true;
                    // ignore facet normal

                // [RMS] also don't really need to do anything for this one
                //} else if (tokens[0].Equals("endfacet", StringComparison.OrdinalIgnoreCase)) {
                    //in_facet = false;


                } else if (tokens[0].Equals("solid", StringComparison.OrdinalIgnoreCase)) {
                    STLSolid newObj = new STLSolid();
                    if (tokens.Length == 2)
                        newObj.Name = tokens[1];
                    else
                        newObj.Name = "object_" + Objects.Count;
                    Objects.Add(newObj);
                    in_solid = true;


                } else if (tokens[0].Equals("endsolid", StringComparison.OrdinalIgnoreCase)) {
                    // do nothing, done object
                    in_solid = false;
                }
            }

            foreach (STLSolid solid in Objects)
                BuildMesh(solid, builder);

            return new IOReadResult(IOCode.Ok, "");
        }

        protected virtual void BuildMesh(STLSolid solid, IMeshBuilder builder)
        {
            var vertexWelder = VertexWelderFactory.Create(WeldStrategy);
            builder.AppendNewMesh(vertexWelder.Weld(solid.Vertices));

            if (WantPerTriAttribs && solid.TriAttribs != null && builder.SupportsMetaData)
                builder.AppendMetaData(PerTriAttribMetadataName, solid.TriAttribs);
        }
    }
}
