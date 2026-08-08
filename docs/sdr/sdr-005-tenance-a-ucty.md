# SDR-005 — Tenance a účty

- **Stav:** platí
- **Milníky:** M2
- **Související SDR:** [002](sdr-002-testovani.md), [004](sdr-004-api-konvence.md),
  [006](sdr-006-domenovy-model.md)

## Rozsah

Účty, tenanti, uživatelé, izolace dat mezi tenanty a rozšíření autentizace o tenanta.
Doménové entity popisuje SDR-006.

## Popis

### Model

- **Account** — id a název. Zastřešuje N tenantů.
- **Tenant** — id, název, `AccountId`. Hranice izolace dat.
- **User** — patří právě jednomu tenantu (`User.TenantId`). Dnešní sloupce (e-mail, hash
  hesla, role, lockout) zůstávají; přibývá `TenantId` a `DefaultContactId` (SDR-010).

Každá tenantová entita nese `TenantId` a `CreatedBy` (id uživatele, který ji založil).

Account, Tenant a první administrátor vznikají jen seedem při startu
(`EvilBrains__EvilCase__Auth__Seed__*`, jen do prázdné tabulky uživatelů). Žádné UI pro
správu účtů, žádná registrace.

### Izolace

Data nesmí utéct mezi tenanty; únik je kritická chyba.

- Každá tenantová entita má EF global query filter na `TenantId`; tenant dodává
  `ITenantContext`, který nahrazuje `IOwnerContext`.
- Access token nese tenant claim; `ITenantContext` ho čte z principalu.
- `SaveChanges` kontroluje, že každý zapisovaný řádek patří tenantu z kontextu.
- Seed běží pod explicitním tenant scope; kontrola v `SaveChanges` porovnává proti tenantu
  dodanému seederem, ne proti principalu požadavku.
- Unikátní indexy tenantových entit jsou kompozitní s `TenantId`.
- Konvenční test hlídá, že žádná tenantová entita filtr nepostrádá (SDR-002).

### Autentizace

Beze změny: JWT access token v paměti, rotující refresh token v `__Host-` cookie,
`PasswordHasher` (PBKDF2), pravidla v `.claude/rules/auth.md`. Jediné rozšíření je tenant
claim v access tokenu. Tenant claim se odvozuje z řádku uživatele při každém vydání tokenu,
včetně refresh.

## Rozhodnutí

- Uživatelé v tenantu: více uživatelů sdílí tenant / 1 uživatel = 1 tenant. Platí
  1 uživatel = 1 tenant; sdílení je non-goal.
- Vynucení izolace: jen ruční scope v dotazech / query filtry + kontrola zápisu. Platí query
  filtry a kontrola v `SaveChanges`.
- Vznik účtů: registrace v UI / jen seed. Platí jen seed.

## Dopady

- `IOwnerContext` a `PrincipalOwnerContext` zanikají; nahrazuje je `ITenantContext`.
- Sloupce `OwnerId` zanikají; nahrazuje je `TenantId` + `CreatedBy` (SDR-006).
- `.claude/rules/business.md` (Ownership) se mění s kódem M2.
