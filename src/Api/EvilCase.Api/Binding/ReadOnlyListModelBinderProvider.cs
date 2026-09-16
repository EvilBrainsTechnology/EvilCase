using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Api.Binding;

/// <summary>
/// Binds a <see cref="IReadOnlyList{T}"/> or <see cref="IReadOnlyCollection{T}"/> the way MVC binds
/// a list; without it such a property takes no value and the filter behind it narrows nothing.
/// </summary>
internal sealed class ReadOnlyListModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var modelType = context.Metadata.ModelType;
        if (!modelType.IsGenericType)
            return null;

        var definition = modelType.GetGenericTypeDefinition();
        if (definition != typeof(IReadOnlyList<>) && definition != typeof(IReadOnlyCollection<>))
            return null;

        var elementType = modelType.GenericTypeArguments[0];
        var elementBinder = context.CreateBinder(context.MetadataProvider.GetMetadataForType(elementType));
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();

        return (IModelBinder)Activator.CreateInstance(
            typeof(ReadOnlyListModelBinder<>).MakeGenericType(elementType),
            elementBinder,
            loggerFactory)!;
    }
}
