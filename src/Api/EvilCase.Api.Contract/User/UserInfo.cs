using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Api.Contract.User;

public record UserInfo
{
    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}
