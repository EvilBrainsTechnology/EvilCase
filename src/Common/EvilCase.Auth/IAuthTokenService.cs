using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

public interface IAuthTokenService
{
    public string GenerateToken(User user);
}
