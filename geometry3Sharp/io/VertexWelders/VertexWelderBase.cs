namespace g3
{
    internal abstract class VertexWelderBase : IVertexWelder
    {
        /// <summary>
        /// Vertices within this distance are considered "the same" by welding strategies.
        /// </summary>
        public double WeldTolerance { get; } = MathUtil.ZeroTolerancef;

        public abstract void BuildMesh(DVectorArray3f vertices, DMesh3Builder builder);

        public DMesh3 Weld(DVectorArray3f vertices)
        {
            var builder = new DMesh3Builder();
            builder.AppendNewMesh(false, false, false, false);
            BuildMesh(vertices, builder);
            return builder.Meshes[0];
        }

        protected void AppendMappedTriangles(DVectorArray3f vertices, IMeshBuilder builder, int[] mappedVertices)
        {
            int triangleCount = vertices.Count / 3;
            for (int triangleIndex = 0; triangleIndex < triangleCount; ++triangleIndex)
            {
                int vertexA = mappedVertices[3 * triangleIndex];
                int vertexB = mappedVertices[3 * triangleIndex + 1];
                int vertexC = mappedVertices[3 * triangleIndex + 2];

                // Don't try to add degenerate triangles
                if (vertexA == vertexB || vertexA == vertexC || vertexB == vertexC)
                    continue;

                builder.AppendTriangle(vertexA, vertexB, vertexC);
            }
        }
    }
}