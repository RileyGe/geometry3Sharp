namespace g3
{
    public enum VertexWelderStrategies
    {
        NoProcessing = 0,           // return triangle soup
        IdenticalVertexWeld = 1,    // merge identical vertices. Logically sensible but doesn't always work on ASCII STL.
        TolerantVertexWeld = 2,     // merge vertices within .WeldTolerance
        AutoBestResult = 3          // try identical weld first, if there are holes then try tolerant weld, and return "best" result
                                    // ("best" is not well-defined...)
    }
}