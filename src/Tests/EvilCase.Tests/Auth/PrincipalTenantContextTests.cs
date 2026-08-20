using System.Security.Claims;
using EvilBrains.EvilCase.Api.Auth;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Tests.Auth;

public class PrincipalTenantContextTests
{
    [Test]
    public void TheTenantIsTheTenantClaim()
    {
        var tenantId = Guid.CreateVersion7();
        var context = NewContext(("tenant", tenantId.ToString("D", CultureInfo.InvariantCulture)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.TenantId, Is.EqualTo(tenantId));
            Assert.That(context.TenantIdOrDefault, Is.EqualTo(tenantId));
        }
    }

    [Test]
    public void NoCallerIsNoTenantRatherThanAnError()
    {
        var anonymous = NewContext();
        var noRequest = new PrincipalTenantContext(new HttpContextAccessor());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(anonymous.TenantIdOrDefault, Is.Null, "a health probe and the sign-in endpoint both reach here");
            Assert.That(noRequest.TenantIdOrDefault, Is.Null, "so does a migration at startup, with no request at all");
        }
    }

    [Test]
    public void AClaimThatIsNotAnIdentifierIsNoTenant()
    {
        var context = NewContext(("tenant", "not-a-guid"));

        Assert.That(context.TenantIdOrDefault, Is.Null);
    }

    [Test]
    public void CodeThatCannotWorkWithoutATenantSaysSo()
    {
        var anonymous = NewContext();

        Assert.That(() => anonymous.TenantId, Throws.InvalidOperationException);
    }

    [Test]
    public void TheClaimIsTheOneTheTokenActuallyCarries()
    {
        var tenantId = Guid.CreateVersion7();
        var context = NewContext((AuthClaims.Tenant, tenantId.ToString("D", CultureInfo.InvariantCulture)));

        Assert.That(context.TenantId, Is.EqualTo(tenantId), "so the token and the reader cannot drift apart");
    }

    [Test]
    public void EnteringATenantOverridesThePrincipalAndRestoresOnDispose()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        var context = NewContext((AuthClaims.Tenant, tenantA.ToString("D", CultureInfo.InvariantCulture)));

        using (context.Enter(tenantB))
            Assert.That(context.TenantId, Is.EqualTo(tenantB));

        Assert.That(context.TenantId, Is.EqualTo(tenantA), "the seeder names its tenant and the request keeps its own");
    }

    private static PrincipalTenantContext NewContext(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)));
        var httpContext = new DefaultHttpContext { User = new(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return new PrincipalTenantContext(accessor);
    }
}
