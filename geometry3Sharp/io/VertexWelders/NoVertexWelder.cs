namespace g3
{
    internal sealed class NoVertexWelder : VertexWelderBase
    {
        public override void BuildMesh(DVectorArray3f vertices, DMesh3Builder builder)
        {
            int triangleCount = vertices.Count / 3;

            for (int triangleIndex = 0; triangleIndex < triangleCount; ++triangleIndex)
            {
                Vector3f vertexA = vertices[3 * triangleIndex];
                int indexA = builder.AppendVertex(vertexA.x, vertexA.y, vertexA.z);

                Vector3f vertexB = vertices[3 * triangleIndex + 1];
                int indexB = builder.AppendVertex(vertexB.x, vertexB.y, vertexB.z);

                Vector3f vertexC = vertices[3 * triangleIndex + 2];
                int indexC = builder.AppendVertex(vertexC.x, vertexC.y, vertexC.z);

                builder.AppendTriangle(indexA, indexB, indexC);
            }
        }
    }
}