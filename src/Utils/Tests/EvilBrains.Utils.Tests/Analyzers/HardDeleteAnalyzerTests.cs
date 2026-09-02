using System.Collections.Immutable;
using EvilBrains.Analyzers;
using EvilBrains.Utils.Tests.ApiClient;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.Analyzers;

public class HardDeleteAnalyzerTests
{
    [Test]
    public async Task ExecuteDeleteOnASoftDeleteEntityIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public class Writer
            {
                public int Delete(IQueryable<Stamped> rows)
                {
                    return rows.ExecuteDelete();
                }
            }
            """);

        Assert.That(diagnostics.Select(static x => x.Id), Is.EqualTo(["EB0011"]), "a soft-delete entity must be stamped, not removed");
    }

    [Test]
    public async Task ExecuteDeleteAsyncOnASoftDeleteEntityIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public class Writer
            {
                public Task<int> Delete(IQueryable<Stamped> rows)
                {
                    return rows.ExecuteDeleteAsync();
                }
            }
            """);

        Assert.That(diagnostics.Select(static x => x.Id), Is.EqualTo(["EB0011"]), "the asynchronous overload takes the rows just the same");
    }

    [Test]
    public async Task ExecuteDeleteOnAnEntityThatIsRemovedOutrightHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class Writer
            {
                public int Delete(IQueryable<Plain> rows)
                {
                    return rows.ExecuteDelete();
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "an entity carrying no stamp is deleted by removing it");
    }

    [Test]
    public async Task AnotherCallOnASoftDeleteEntityHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class Writer
            {
                public int Count(IQueryable<Stamped> rows)
                {
                    return rows.Count();
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "only ExecuteDelete is in scope");
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string body)
    {
        return await AnalyzerTestHost.Analyze(new HardDeleteAnalyzer(), Fixture(body));
    }

    private static string Fixture(string body)
    {
        return $$"""
            namespace Microsoft.EntityFrameworkCore
            {
                using System.Linq;
                using System.Threading.Tasks;

                public static class EntityFrameworkQueryableExtensions
                {
                    public static int ExecuteDelete<TSource>(this IQueryable<TSource> source)
                    {
                        return 0;
                    }

                    public static Task<int> ExecuteDeleteAsync<TSource>(this IQueryable<TSource> source)
                    {
                        return Task.FromResult(0);
                    }
                }
            }

            namespace Fake
            {
                using System.Linq;
                using System.Threading.Tasks;
                using Microsoft.EntityFrameworkCore;

                public interface ISoftDeleteEntity
                {
                }

                public class Stamped : ISoftDeleteEntity
                {
                }

                public class Plain
                {
                }

            {{body}}
            }
            """;
    }
}
