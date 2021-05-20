using System.Collections.Generic;

namespace g3
{
    internal sealed class IdenticalVertexWelder : VertexWelderBase
    {
        public override void BuildMesh(DVectorArray3f vertices, DMesh3Builder builder)
        {
            int[] mappedVertices = new int[vertices.Count];

            Dictionary<Vector3f, int> uniqueVertices = new Dictionary<Vector3f, int>();

            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                Vector3f vertex = vertices[vertexIndex];
                int existingVertexIndex;

                if (uniqueVertices.TryGetValue(vertex, out existingVertexIndex))
                {
                    mappedVertices[vertexIndex] = existingVertexIndex;
                }
                else
                {
                    int newVertexIndex = builder.AppendVertex(vertex.x, vertex.y, vertex.z);
                    uniqueVertices[vertex] = newVertexIndex;
                    mappedVertices[vertexIndex] = newVertexIndex;
                }
            }

            AppendMappedTriangles(vertices, builder, mappedVertices);
        }
    }
}