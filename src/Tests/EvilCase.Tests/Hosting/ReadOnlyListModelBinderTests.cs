using EvilBrains.EvilCase.Api.Binding;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class ReadOnlyListModelBinderTests
{
    [Test]
    public void TheBinderLogsUnderMvcCategoryNotUnderOurs()
    {
        var loggerFactory = new CategoryRecorder();
        var metadataProvider = new EmptyModelMetadataProvider();
        var context = Substitute.For<ModelBinderProviderContext>();
        context.Metadata.Returns(metadataProvider.GetMetadataForType(typeof(IReadOnlyList<Guid>)));
        context.MetadataProvider.Returns(metadataProvider);
        context.Services.Returns(new ServiceCollection().AddSingleton<ILoggerFactory>(loggerFactory).BuildServiceProvider());
        context.CreateBinder(Arg.Any<ModelMetadata>()).Returns(Substitute.For<IModelBinder>());

        var binder = new ReadOnlyListModelBinderProvider().GetBinder(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(binder, Is.Not.Null);
            Assert.That(
                loggerFactory.Categories,
                Is.Not.Empty.And.All.StartsWith("Microsoft.AspNetCore.Mvc.ModelBinding"),
                "MVC's binding messages stay out of the category our Debug level covers");
        }
    }

    private sealed class CategoryRecorder : ILoggerFactory
    {
        public List<string> Categories { get; } = [];

        public ILogger CreateLogger(string categoryName)
        {
            this.Categories.Add(categoryName);

            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        { }

        public void Dispose()
        { }
    }
}
