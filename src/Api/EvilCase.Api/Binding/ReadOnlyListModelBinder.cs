using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EvilBrains.EvilCase.Api.Binding;

/// <summary>
/// Binds the values into a new list. MVC hands a property's current value to a collection binder to
/// fill in place, which a read-only list refuses without a word.
/// </summary>
internal sealed class ReadOnlyListModelBinder(IModelBinder collectionBinder) : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        bindingContext.Model = null;

        await collectionBinder.BindModelAsync(bindingContext);
    }
}
