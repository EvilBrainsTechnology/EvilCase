using System.Collections.Immutable;
using EvilBrains.Analyzers;
using EvilBrains.Utils.Tests.ApiClient;
using Microsoft.CodeAnalysis;

namespace EvilBrains.Utils.Tests.Analyzers;

public class DbSetAccessAnalyzerTests
{
    [Test]
    public async Task SetOutsideTheContextIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public class Reader
            {
                public DbSet<Entity> Read(DbContext context)
                {
                    return context.Set<Entity>();
                }
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0009"]), "an entity must be reached through its typed DbSet");
    }

    [Test]
    public async Task SetWithANameIsReportedTest()
    {
        var diagnostics = await Analyze("""
            public class Reader
            {
                public DbSet<Entity> Read(DbContext context)
                {
                    return context.Set<Entity>("entities");
                }
            }
            """);

        Assert.That(diagnostics.Select(x => x.Id), Is.EqualTo(["EB0009"]), "the named overload is Set<TEntity>() all the same");
    }

    [Test]
    public async Task SetInsideTheContextHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class ApplicationContext : DbContext
            {
                public DbSet<Entity> Entities
                {
                    get
                    {
                        return this.Set<Entity>();
                    }
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "the context's own declaration is where Set<TEntity>() belongs");
    }

    [Test]
    public async Task SetInsideADerivedContextHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class BaseContext : DbContext
            {
            }

            public class ApplicationContext : BaseContext
            {
                public DbSet<Entity> All()
                {
                    return this.Set<Entity>();
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "a context derived through a base context is still the context's own declaration");
    }

    [Test]
    public async Task TypedDbSetPropertyHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class ApplicationContext : DbContext
            {
                public DbSet<Entity> Entities
                {
                    get
                    {
                        return this.Set<Entity>();
                    }
                }
            }

            public class Reader
            {
                public DbSet<Entity> Read(ApplicationContext context)
                {
                    return context.Entities;
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "reading a typed DbSet must not be reported");
    }

    [Test]
    public async Task SetOnAnUnrelatedTypeHasNoDiagnosticsTest()
    {
        var diagnostics = await Analyze("""
            public class Registry
            {
                public string Set<TValue>()
                {
                    return "";
                }
            }

            public class Reader
            {
                public string Read(Registry registry)
                {
                    return registry.Set<int>();
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty, "only a DbContext's Set<TEntity>() is in scope");
    }

    private static Task<ImmutableArray<Diagnostic>> Analyze(string body)
    {
        return AnalyzerTestHost.Analyze(new DbSetAccessAnalyzer(), Fixture(body));
    }

    private static string Fixture(string body)
    {
        return $$"""
            namespace Microsoft.EntityFrameworkCore
            {
                public class DbSet<TEntity>
                {
                }

                public class DbContext
                {
                    public DbSet<TEntity> Set<TEntity>()
                    {
                        return new DbSet<TEntity>();
                    }

                    public DbSet<TEntity> Set<TEntity>(string name)
                    {
                        return new DbSet<TEntity>();
                    }
                }
            }

            namespace Fake
            {
                using Microsoft.EntityFrameworkCore;

                public class Entity
                {
                }

            {{body}}
            }
            """;
    }
}
