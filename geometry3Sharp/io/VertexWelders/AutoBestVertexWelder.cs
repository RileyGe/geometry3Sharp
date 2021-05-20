namespace g3
{
    internal sealed class AutoBestVertexWelder : VertexWelderBase
    {
        private readonly IdenticalVertexWelder identicalVertexWeld;
        private readonly TolerantVertexWelder toleranceVertexWeld;

        public AutoBestVertexWelder()
        {
            identicalVertexWeld = new IdenticalVertexWelder();
            toleranceVertexWeld = new TolerantVertexWelder();
        }

        public override void BuildMesh(DVectorArray3f vertices, DMesh3Builder builder)
        {
            DMesh3 identicalWeldMesh = identicalVertexWeld.Weld(vertices);

            if (CheckForCracks(identicalWeldMesh, out int identicalWeldMesh_boundaryCount, WeldTolerance))
            {
                DMesh3 toleranceWeldMesh = toleranceVertexWeld.Weld(vertices);
                int toleranceWeldMeshBoundaryCount = CountBoundaryEdges(toleranceWeldMesh);

                if (toleranceWeldMeshBoundaryCount < identicalWeldMesh_boundaryCount)
                    builder.AppendNewMesh(toleranceWeldMesh);
                else
                    builder.AppendNewMesh(identicalWeldMesh);
            }

            builder.AppendNewMesh(identicalWeldMesh);
        }

        private bool CheckForCracks(DMesh3 mesh, out int boundaryEdgeCount, double crackTolerance = MathUtil.ZeroTolerancef)
        {
            boundaryEdgeCount = 0;
            MeshVertexSelection boundaryVertices = new MeshVertexSelection(mesh);

            foreach (int edgeIndex in mesh.BoundaryEdgeIndices())
            {
                Index2i edgeVertices = mesh.GetEdgeV(edgeIndex);
                boundaryVertices.Select(edgeVertices.a); boundaryVertices.Select(edgeVertices.b);
                boundaryEdgeCount++;
            }

            if (boundaryVertices.Count == 0)
                return false;

            AxisAlignedBox3d bounds = mesh.CachedBounds;
            PointHashGrid3d<int> borderV = new PointHashGrid3d<int>(bounds.MaxDim / 128, -1);
            foreach (int vid in boundaryVertices)
            {
                Vector3d v = mesh.GetVertex(vid);
                var result = borderV.FindNearestInRadius(v, crackTolerance, (existing_vid) =>
                {
                    return v.Distance(mesh.GetVertex(existing_vid));
                });
                if (result.Key != -1)
                    return true;            // we found a crack vertex!
                borderV.InsertPoint(vid, v);
            }

            // found no cracks
            return false;
        }

        private int CountBoundaryEdges(DMesh3 mesh)
        {
            int boundary_edge_count = 0;
            foreach (int eid in mesh.BoundaryEdgeIndices())
            {
                boundary_edge_count++;
            }
            return boundary_edge_count;
        }
    }
}