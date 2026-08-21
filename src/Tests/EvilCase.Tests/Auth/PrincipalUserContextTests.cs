using System.Security.Claims;
using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Tests.Auth;

public class PrincipalUserContextTests
{
    [Test]
    public void TheUserIsTheSubjectClaim()
    {
        var userId = Guid.CreateVersion7();
        var context = NewContext((AuthClaims.Subject, userId.ToString("D", CultureInfo.InvariantCulture)));

        Assert.That(context.UserId, Is.EqualTo(userId), "so the token and the reader cannot drift apart");
    }

    [Test]
    public void AClaimThatIsNotAnIdentifierIsNoUser()
    {
        var context = NewContext(("sub", "not-a-guid"));

        Assert.That(() => context.UserId, Throws.InvalidOperationException);
    }

    [Test]
    public void NoCallerIsNoUser()
    {
        var anonymous = NewContext();
        var noRequest = new PrincipalUserContext(new HttpContextAccessor());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => anonymous.UserId, Throws.InvalidOperationException);
            Assert.That(() => noRequest.UserId, Throws.InvalidOperationException);
        }
    }

    private static PrincipalUserContext NewContext(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)));
        var httpContext = new DefaultHttpContext { User = new(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return new PrincipalUserContext(accessor);
    }
}
