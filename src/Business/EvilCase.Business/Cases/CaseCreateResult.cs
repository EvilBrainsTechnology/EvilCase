using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

public readonly union CaseCreateResult(CaseListItem, CaseCreateFailure);
