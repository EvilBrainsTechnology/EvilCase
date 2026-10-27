using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignatureDisplayTests
{
    [Test]
    public void EveryProblemHasItsOwnReason()
    {
        var reasons = Enum.GetValues<FileSignatureProblem>().Select(FileSignatureDisplay.Reason).ToList();

        Assert.That(reasons, Is.Unique, "each problem tells the user something different");
    }
}
