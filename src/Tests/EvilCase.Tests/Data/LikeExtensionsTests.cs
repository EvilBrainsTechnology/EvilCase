using EvilBrains.EvilCase.Data;

namespace EvilBrains.EvilCase.Tests.Data;

public class LikeExtensionsTests
{
    [Test]
    public void EveryWildcardBecomesALiteral()
    {
        Assert.That("50%_a\\b".EscapeLikeWildcards(), Is.EqualTo(@"50\%\_a\\b"), "a percent, an underscore and a backslash all escape");
    }

    [Test]
    public void TheEscapeItselfIsEscapedFirst()
    {
        Assert.That("\\%".EscapeLikeWildcards(), Is.EqualTo(@"\\\%"), "the backslash escapes before the percent it precedes, so the percent is not swallowed");
    }

    [Test]
    public void ATermWithoutAWildcardIsUntouched()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That("EC/20260807-001".EscapeLikeWildcards(), Is.EqualTo("EC/20260807-001"), "a term without a wildcard is untouched");
            Assert.That("".EscapeLikeWildcards(), Is.EqualTo(""), "an empty term stays empty");
        }
    }
}
