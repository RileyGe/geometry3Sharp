namespace g3
{
    internal sealed class TolerantVertexWelder : VertexWelderBase
    {
        public override void BuildMesh(DVectorArray3f vertices, DMesh3Builder builder)
        {
            int N = vertices.Count;
            int[] mapV = new int[N];

            AxisAlignedBox3d bounds = AxisAlignedBox3d.Empty;
            for (int i = 0; i < N; ++i)
                bounds.Contain(vertices[i]);

            // [RMS] because we are only searching within tiny radius, there is really no downside to
            // using lots of bins here, except memory usage. If we don't, and the mesh has a ton of triangles
            // very close together (happens all the time on big meshes!), then this step can start
            // to take an *extremely* long time!
            int num_bins = 256;
            if (N > 100000) num_bins = 512;
            if (N > 1000000) num_bins = 1024;
            if (N > 2000000) num_bins = 2048;
            if (N > 5000000) num_bins = 4096;

            PointHashGrid3d<int> uniqueV = new PointHashGrid3d<int>(bounds.MaxDim / (float)num_bins, -1);
            Vector3f[] pos = new Vector3f[N];
            for (int vi = 0; vi < N; ++vi)
            {
                Vector3f v = vertices[vi];

                var pair = uniqueV.FindNearestInRadius(v, WeldTolerance, (vid) =>
                {
                    return v.Distance(pos[vid]);
                });
                if (pair.Key == -1)
                {
                    int vid = builder.AppendVertex(v.x, v.y, v.z);
                    uniqueV.InsertPoint(vid, v);
                    mapV[vi] = vid;
                    pos[vid] = v;
                }
                else
                {
                    mapV[vi] = pair.Key;
                }
            }

            AppendMappedTriangles(vertices, builder, mapV);
        }
    }
}