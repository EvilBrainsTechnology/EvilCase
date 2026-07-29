namespace EvilBrains.ApiClient.Generator;

internal enum ParameterKind
{
    Route = 0,

    Query = 1,

    QueryObject = 2,

    Header = 3,

    Body = 4,

    Token = 5,

    Skipped = 6,
}
