namespace Jewelix.Logging.Tests.Helper;

/// <summary>
/// xUnit collection definition that enforces sequential execution for all tests in the
/// "Serilog" collection. Required because those tests mutate the static
/// <c>Log.Logger</c> — parallel execution would race on the global sink.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerilogTestCollection
{
    public const string Name = "Serilog";
}
