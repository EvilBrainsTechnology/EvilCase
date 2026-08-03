using System.Security.Claims;
using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Tests.Auth;

public class PrincipalOwnerContextTests
{
    [Test]
    public void TheOwnerIsTheSubjectClaim()
    {
        var context = NewContext(("sub", "42"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.OwnerId, Is.EqualTo(42));
            Assert.That(context.OwnerIdOrDefault, Is.EqualTo(42));
        }
    }

    [Test]
    public void NoCallerIsNoOwnerRatherThanAnError()
    {
        var anonymous = NewContext();
        var noRequest = new PrincipalOwnerContext(new HttpContextAccessor());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anonymous.OwnerIdOrDefault, Is.Null, "a health probe and the sign-in endpoint both reach here");
            Assert.That(noRequest.OwnerIdOrDefault, Is.Null, "so does a migration at startup, with no request at all");
        }
    }

    [Test]
    public void ASubjectThatIsNotAnIdentifierIsNoOwner()
    {
        var nonsense = NewContext(("sub", "not-a-number"));
        var negative = NewContext(("sub", "-1"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nonsense.OwnerIdOrDefault, Is.Null);
            Assert.That(negative.OwnerIdOrDefault, Is.Null, "no identifier is negative, so a sign is a malformed token rather than a user");
        }
    }

    [Test]
    public void CodeThatCannotWorkWithoutAnOwnerSaysSo()
    {
        var anonymous = NewContext();

        Assert.That(() => anonymous.OwnerId, Throws.InvalidOperationException);
    }

    [Test]
    public void TheClaimIsTheOneTheTokenActuallyCarries()
    {
        var context = NewContext((AuthClaims.Subject, "7"));

        Assert.That(context.OwnerId, Is.EqualTo(7), "so the token and the reader cannot drift apart");
    }

    private static PrincipalOwnerContext NewContext(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)));
        var httpContext = new DefaultHttpContext { User = new(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return new PrincipalOwnerContext(accessor);
    }
}
