using System.Collections.Concurrent;
using System.Net;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class ListRequestBindingTests
{
    private readonly List<CaseListRequest> bound = [];

    private readonly List<ActListRequest> boundActs = [];

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [SetUp]
    public void SetUp()
    {
        this.bound.Clear();
        this.boundActs.Clear();

        var reader = Substitute.For<ICaseReader>();
        reader
            .ListCases(Arg.Any<CaseListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CaseListResponse { Items = [], TotalCount = 0 })
            .AndDoes(call => this.bound.Add(call.Arg<CaseListRequest>()));

        var actReader = Substitute.For<IActReader>();
        actReader
            .ListActs(Arg.Any<ActListRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ActListResponse { Items = [], TotalCount = 0 })
            .AndDoes(call => this.boundActs.Add(call.Arg<ActListRequest>()));

        this.host = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton(reader);
            services.AddSingleton(actReader);
        });
        this.client = this.host.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task ARepeatedLabelParameterBindsAsEveryLabelTheFilterAsksFor()
    {
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();

        using var response = await this.Get($"/api/cases?take=20&labelIds={first}&labelIds={second}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(this.bound.Single().LabelIds, Is.EqualTo([first, second]), "a query parameter repeated on the wire reaches the filter as every value it carried");
        }
    }

    [Test]
    public async Task AListWithoutAPageSizeIsRefused()
    {
        using var response = await this.Get("/api/cases");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "the page size is required, so a list call omitting it never reaches the reader");
            Assert.That(this.bound, Is.Empty);
        }
    }

    [Test]
    public async Task APageSizeAboveTheHundredIsRefused()
    {
        using var response = await this.Get("/api/cases?take=101");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "the page limit holds at the wire, not only on the record");
    }

    [Test]
    public async Task TheSortAndItsDirectionBindFromTheWireThoughTheSharedRequestDeclaresThem()
    {
        using var response = await this.Get("/api/acts?take=20&sort=Title&sortDirection=Descending");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(this.boundActs.Single().Sort, Is.EqualTo(ActSortKey.Title));
            Assert.That(
                this.boundActs.Single().SortDirection,
                Is.EqualTo(ListSortDirection.Descending),
                "a list takes the sort its caller asks for, though the shared request declares both");
        }
    }

    [Test]
    public async Task TheBindingOfAListLogsUnderMvcCategoryNotUnderOurs()
    {
        var categories = new ConcurrentBag<string>();
        await using var logHost = new EvilCaseHost(configureServices: services =>
        {
            services.AddSingleton(Substitute.For<IActReader>());
            services.Configure<LoggerFilterOptions>(static options =>
            {
                options.Rules.Clear();
                options.MinLevel = LogLevel.Trace;
            });
            services.AddSingleton<ILoggerProvider>(new CategoryRecorder(categories));
        });
        using var logClient = logHost.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/acts?take=20", UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(logHost) } };

        using var response = await logClient.SendAsync(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(categories, Has.Some.StartsWith("Microsoft.AspNetCore.Mvc.ModelBinding"), "the binding messages are logged");
            Assert.That(
                categories
                    .Where(static category => category.StartsWith("EvilBrains", StringComparison.Ordinal))
                    .Where(static category => category.Contains("ModelBinder", StringComparison.Ordinal)),
                Is.Empty,
                "a binder of ours does not file MVC's messages under our category");
        }
    }

    private async Task<HttpResponseMessage> Get(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host) } };

        return await this.client.SendAsync(request);
    }

    private sealed class CategoryRecorder(ConcurrentBag<string> categories) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new Recorder(categoryName, categories);
        }

        public void Dispose()
        { }

        private sealed class Recorder(string category, ConcurrentBag<string> categories) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                categories.Add(category);
            }
        }
    }
}
