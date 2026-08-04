using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// Two cases created in the same second must not take the same <c>{seq}</c>. What stops them is that
/// the counter is read, raised and returned by one statement, so PostgreSQL serialises the second
/// caller on the row the first one is holding.
/// </summary>
public class NumberSequenceSqlTests
{
    [Test]
    public void TakingTheNextValueIsOneStatement()
    {
        const string sql = NumberSequenceSql.TakeNext;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Not.Contain(";"), "a second statement is a second round trip, and two callers interleave between them");
            Assert.That(sql, Does.Contain("INSERT INTO \"NumberSequences\""));
            Assert.That(sql, Does.Contain("ON CONFLICT (\"OwnerId\", \"Scope\")"), "the unique index is what turns the race into a wait");
            Assert.That(sql, Does.Contain("DO UPDATE SET \"LastValue\" = \"NumberSequences\".\"LastValue\" + 1"), "the value is raised from what the row holds, never from what the caller read");
            Assert.That(sql, Does.Contain("RETURNING \"LastValue\" AS \"Value\""), "the statement that raises the counter is the one that says what it took, under the name a scalar query reads");
        }
    }

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
