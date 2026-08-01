using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IAuthTokenService
{
    public AccessToken Generate(User user, Guid sessionId);
}
