using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Lists;

namespace EvilBrains.EvilCase.Tests.Lists;

public class ListRequestValidationTests
{
    [Test]
    public void ATakeAboveTheHundredIsRefused()
    {
        var refused = Validate(new CaseListRequest { Take = 101 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ListRequest.Take)), "a page never holds more than a hundred rows");
    }

    [Test]
    public void ATakeBelowOneIsRefused()
    {
        var refused = Validate(new CaseListRequest { Take = 0 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ListRequest.Take)), "a page holds at least one row");
    }

    [Test]
    public void ANegativeSkipIsRefused()
    {
        var refused = Validate(new CaseListRequest { Skip = -1, Take = 20 });

        Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ListRequest.Skip)), "a page starts at the first row or later");
    }

    [Test]
    public void TheLimitDeclaredOnTheSharedRequestBindsTheActListToo()
    {
        var refused = Validate(new ActListRequest { Take = 101 });
        var accepted = Validate(new ActListRequest { Skip = 40, Take = 100 });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(refused.Single().MemberNames, Does.Contain(nameof(ListRequest.Take)), "every list takes its page limit from the shared request");
            Assert.That(accepted, Is.Empty);
        }
    }

    private static List<ValidationResult> Validate(ListRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }
}
