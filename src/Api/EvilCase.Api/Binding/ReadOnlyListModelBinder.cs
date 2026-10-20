using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Api.Binding;

/// <summary>
/// Binds the values into a new list. MVC hands a property's current value to a collection binder to
/// fill in place, which a read-only list refuses without a word.
/// </summary>
internal sealed class ReadOnlyListModelBinder<TElement>(IModelBinder elementBinder, ILoggerFactory loggerFactory)
    : CollectionModelBinder<TElement>(elementBinder, new MvcCategoryLoggerFactory(loggerFactory))
{
    public override async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        bindingContext.Model = null;

        await base.BindModelAsync(bindingContext);
    }

    /// <summary>
    /// The base binder names its logger after the runtime type; this files its messages under MVC's own category instead of ours.
    /// </summary>
    private sealed class MvcCategoryLoggerFactory(ILoggerFactory inner) : ILoggerFactory
    {
        public ILogger CreateLogger(string categoryName)
        {
            return inner.CreateLogger<CollectionModelBinder<TElement>>();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        { }
    }
}
