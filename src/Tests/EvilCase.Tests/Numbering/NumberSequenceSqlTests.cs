using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The statement as text: a scope reaching it as a parameter rather than concatenated in, and the
/// value coming from the row it conflicts with. What it does under two callers is
/// <see cref="NumberSequenceAllocatorTests"/>.
/// </summary>
public class NumberSequenceSqlTests
{
    [Test]
    public void TheOwnerAndTheScopeArriveAsParametersAndTheValueIsTheRowsOwnRaisedByOne()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberSequenceSql.TakeNext, Does.Contain("VALUES ({0}, {1}, 1)"), "a scope concatenated into the text is an injection");
            Assert.That(
                NumberSequenceSql.TakeNext,
                Does.Contain(@"DO UPDATE SET ""LastValue"" = ""NumberSequences"".""LastValue"" + 1"),
                "raising EXCLUDED's value instead hands every caller of a running series the number one");
        }
    }
}
