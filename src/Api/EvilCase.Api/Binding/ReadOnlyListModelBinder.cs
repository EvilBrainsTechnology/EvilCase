using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Api.Binding;

/// <summary>
/// Binds the values into a new list. MVC hands a property's current value to a collection binder to
/// fill in place, which a read-only list refuses without a word.
/// </summary>
internal sealed class ReadOnlyListModelBinder<TElement>(IModelBinder elementBinder, ILoggerFactory loggerFactory)
    : CollectionModelBinder<TElement>(elementBinder, loggerFactory)
{
    public override async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        bindingContext.Model = null;

        await base.BindModelAsync(bindingContext);
    }
}
