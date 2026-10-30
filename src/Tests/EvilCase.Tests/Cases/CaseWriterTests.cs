using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseWriterTests
{
    [Test]
    public void ABlankDescriptionIsFiledAsNothing()
    {
        var blank = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "   " };
        var withText = blank with { Description = "text" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseWriter.BuildCase(blank, "EC/20260821-001").Description, Is.Null);
            Assert.That(CaseWriter.BuildCase(withText, "EC/20260821-001").Description, Is.EqualTo("text"));
        }
    }

    [Test]
    public void ATitleAndDescriptionAreStoredTrimmed()
    {
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "  Přestupek  ", Description = "  text  " };

        var @case = CaseWriter.BuildCase(request, "EC/20260821-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.Title, Is.EqualTo("Přestupek"), "a created case stores its title trimmed");
            Assert.That(@case.Description, Is.EqualTo("text"), "a created case stores its description trimmed");
        }
    }
}
