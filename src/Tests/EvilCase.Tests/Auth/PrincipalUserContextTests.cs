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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.UserId, Is.EqualTo(userId), "so the token and the reader cannot drift apart");
            Assert.That(context.UserIdOrDefault, Is.EqualTo(userId));
        }
    }

    [Test]
    public void AClaimThatIsNotAnIdentifierIsNoUser()
    {
        var context = NewContext(("sub", "not-a-guid"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => context.UserId, Throws.InvalidOperationException);
            Assert.That(context.UserIdOrDefault, Is.Null);
        }
    }

    [Test]
    public void NoCallerIsNoUserRatherThanAnError()
    {
        var anonymous = NewContext();
        var noRequest = new PrincipalUserContext(new HttpContextAccessor());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anonymous.UserIdOrDefault, Is.Null, "a health probe and the sign-in endpoint both reach here");
            Assert.That(noRequest.UserIdOrDefault, Is.Null, "so does a migration at startup, with no request at all");
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
