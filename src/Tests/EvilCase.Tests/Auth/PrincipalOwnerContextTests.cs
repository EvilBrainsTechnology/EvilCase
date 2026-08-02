using System.Security.Claims;
using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// The seam M8 filters through. What matters is that ownership is resolved here and nowhere else, so
/// these pin the reading of the claim rather than any query built on it.
/// </summary>
public class PrincipalOwnerContextTests
{
    [Test]
    public void TheOwnerIsTheSubjectClaim()
    {
        var context = NewContext(("sub", "42"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OwnerId, Is.EqualTo(42));
            Assert.That(context.RequireOwnerId(), Is.EqualTo(42));
        }
    }

    [Test]
    public void NoCallerIsNoOwnerRatherThanAnError()
    {
        var anonymous = NewContext();
        var noRequest = new PrincipalOwnerContext(new HttpContextAccessor());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anonymous.OwnerId, Is.Null, "a health probe and the sign-in endpoint both reach here");
            Assert.That(noRequest.OwnerId, Is.Null, "so does a migration at startup, with no request at all");
        }
    }

    [Test]
    public void ASubjectThatIsNotAnIdentifierIsNoOwner()
    {
        var nonsense = NewContext(("sub", "not-a-number"));
        var negative = NewContext(("sub", "-1"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nonsense.OwnerId, Is.Null, "the pipeline has already decided the request may proceed; this only answers whose it is");
            Assert.That(negative.OwnerId, Is.Null, "no identifier is negative, so a sign is a malformed token rather than a user");
        }
    }

    [Test]
    public void CodeThatCannotWorkWithoutAnOwnerSaysSo()
    {
        var anonymous = NewContext();

        Assert.That(
            anonymous.RequireOwnerId,
            Throws.InvalidOperationException,
            "a query that would otherwise return another owner's rows, or none at all, is a bug either way");
    }

    [Test]
    public void TheClaimIsTheOneTheTokenActuallyCarries()
    {
        var context = NewContext((AuthClaims.Subject, "7"));

        Assert.That(context.OwnerId, Is.EqualTo(7), "named from AuthClaims, so the token and the reader cannot drift apart");
    }

    private static PrincipalOwnerContext NewContext(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)));
        var httpContext = new DefaultHttpContext { User = new(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return new PrincipalOwnerContext(accessor);
    }
}
