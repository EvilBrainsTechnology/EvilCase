using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

// ValidateDataAnnotations alone would not see the nested Jwt members: Validator.TryValidateObject does
// not recurse and [ValidateObjectMembers] is inert without this generated validator.
[OptionsValidator]
internal sealed partial class AuthSettingsValidator : IValidateOptions<AuthSettings>;
