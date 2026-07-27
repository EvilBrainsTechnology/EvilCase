using EvilBrains.AI.Data.Entities;

namespace EvilBrains.AI.Auth;

public interface IAuthTokenService
{
    public string GenerateToken(User user);
}
