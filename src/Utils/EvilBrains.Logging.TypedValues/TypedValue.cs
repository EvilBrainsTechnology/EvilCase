namespace EvilBrains.Logging.TypedValues;

public abstract record TypedValue<TValue>(TValue Value)
{
    public static implicit operator TValue(TypedValue<TValue> typedValue)
    {
        ArgumentNullException.ThrowIfNull(typedValue);

        return typedValue.Value;
    }
}
