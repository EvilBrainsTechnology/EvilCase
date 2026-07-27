using EvilCase.Data.Entities;

namespace EvilCase.Auth;

public interface IAuthTokenService
{
    public string GenerateToken(User user);
}
