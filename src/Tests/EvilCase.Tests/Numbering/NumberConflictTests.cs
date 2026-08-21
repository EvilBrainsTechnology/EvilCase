using EvilBrains.EvilCase.Business.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberConflictTests
{
    [Test]
    public void AUniqueViolationOfTheNumberIsARaceWorthRetrying()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberConflict.IsTakenNumber("23505", "IX_Cases_TenantId_CaseNumber", "CaseNumber"), Is.True);
            Assert.That(NumberConflict.IsTakenNumber("23505", "IX_Acts_TenantId_ActNumber", "ActNumber"), Is.True);
        }
    }

    [Test]
    public void AnyOtherFailedWriteIsNotOurs()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(NumberConflict.IsTakenNumber("23505", "IX_ExternalCaseNumbers_CaseId_Value", "CaseNumber"), Is.False);
            Assert.That(NumberConflict.IsTakenNumber("23505", constraintName: null, "CaseNumber"), Is.False);
            Assert.That(NumberConflict.IsTakenNumber("23503", "IX_Cases_TenantId_CaseNumber", "CaseNumber"), Is.False, "a foreign key violation is not a race for the number");
            Assert.That(NumberConflict.IsTakenNumber(sqlState: null, "IX_Cases_TenantId_CaseNumber", "CaseNumber"), Is.False);
        }
    }
}
