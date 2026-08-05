using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The one thing about the statement no server can show: a scope reaching it as text rather than as a
/// parameter. What it does under two callers is <see cref="NumberSequenceAllocatorTests"/>.
/// </summary>
public class NumberSequenceSqlTests
{
    [Test]
    public void TheOwnerAndTheScopeArriveAsParameters()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberSequenceSql.TakeNext, Does.Contain("VALUES ({0}, {1}, 1)"), "a scope concatenated into the text is an injection");
            Assert.That(NumberSequenceSql.TakeNext, Does.Not.Contain("{2}"), "the value taken is the database's to compute");
        }
    }
}
