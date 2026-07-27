using EvilBrains.Logging.TypedValues;

namespace EvilBrains.Utils.Tests.TypedValues;

public class TypedValueTests
{
    [Test]
    public void Test()
    {
        var a1 = new StringValueA("a1");
        var a2 = new StringValueA("a2");
        var a3 = new StringValueA("b");
        var b1 = new StringValueB("b");
        var b2 = new StringValueB("b");

        string str = a1;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a1.Value, Is.EqualTo("a1"));
            Assert.That(a2.Value, Is.EqualTo("a2"));
            Assert.That(a3.Value, Is.EqualTo("b"));
            Assert.That(b1.Value, Is.EqualTo("b"));
            Assert.That(b2.Value, Is.EqualTo("b"));

            Assert.That(str, Is.EqualTo("a1"));

            Assert.That(a1, Is.Not.EqualTo(a2));
            Assert.That<TypedValue<string>>(a3, Is.Not.EqualTo(b1));
            Assert.That(b1, Is.EqualTo(b2));
        }
    }

    public record StringValueA(string Value) : TypedValue<string>(Value);

    public record StringValueB(string Value) : TypedValue<string>(Value);
}
