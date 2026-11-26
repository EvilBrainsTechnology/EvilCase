using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.Business.Labels;

public readonly union LabelCreateResult(LabelItem, LabelCreateFailure);
