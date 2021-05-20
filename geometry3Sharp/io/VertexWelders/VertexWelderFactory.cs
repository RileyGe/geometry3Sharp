using System;

namespace g3
{
    internal static class VertexWelderFactory
    {
        public static IVertexWelder Create(VertexWelderStrategies strategy)
        {
            return strategy switch
            {
                VertexWelderStrategies.NoProcessing => new NoVertexWelder(),
                VertexWelderStrategies.IdenticalVertexWeld => new IdenticalVertexWelder(),
                VertexWelderStrategies.TolerantVertexWeld => new TolerantVertexWelder(),
                VertexWelderStrategies.AutoBestResult => new AutoBestVertexWelder(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}