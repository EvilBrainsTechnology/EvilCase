using EvilBrains.EvilCase.Api.Contract.ApiTokens;

namespace EvilBrains.EvilCase.Auth;

public readonly union ApiTokenCreateResult(ApiTokenCreated, ApiTokenCreateFailure);
