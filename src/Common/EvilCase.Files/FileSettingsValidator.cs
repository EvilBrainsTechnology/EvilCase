using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Files;

[OptionsValidator]
internal sealed partial class FileSettingsValidator : IValidateOptions<FileSettings>;
