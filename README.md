# EvilCase

## Local Development

### Secrets Access

Secrets are saved in [Infisical](https://infisical.com/)  here: https://infisical.vdolek.cz/.

- Obtain your own client secret in Infisical [here](https://infisical.vdolek.cz/organization/identities/1fee778e-ad7f-450a-b927-0f9e49c3d022).
- Add this secret as `EvilBrains:EvilCase:Infisical:ClientSecret` configuration to your local secrets.
  - You can use `dotnet r add-secret` command.