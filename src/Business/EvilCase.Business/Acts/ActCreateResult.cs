using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

public readonly union ActCreateResult(ActListItem, ActCreateFailure);
