# SDR-001 — Tenance a účty

- **Stav:** platí
- **Milníky:** M2
- **Související SDR:** [002](sdr-002-domenovy-model.md), [013](sdr-013-api-konvence.md),
  [015](sdr-015-testovani.md)

## Rozsah

Účty, tenanti, uživatelé, izolace dat mezi tenanty a rozšíření autentizace o tenanta.
Doménové entity popisuje SDR-002.

## Popis

### Model

- **Account** — id a název. Zastřešuje N tenantů.
- **Tenant** — id, název, `AccountId`. Hranice izolace dat.
- **User** — patří právě jednomu tenantu (`User.TenantId`). Dnešní sloupce (e-mail, hash
  hesla, role, lockout) zůstávají; přibývá `TenantId` a `DefaultContactId` (SDR-006).

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
- Unikátní indexy tenantových entit jsou kompozitní s `TenantId`.
- Konvenční test hlídá, že žádná tenantová entita filtr nepostrádá (SDR-015).

### Autentizace

Beze změny: JWT access token v paměti, rotující refresh token v `__Host-` cookie,
`PasswordHasher` (PBKDF2), pravidla v `.claude/rules/auth.md`. Jediné rozšíření je tenant
claim v access tokenu.

## Rozhodnutí

- Uživatelé v tenantu: více uživatelů sdílí tenant / 1 uživatel = 1 tenant. Platí
  1 uživatel = 1 tenant; sdílení je non-goal.
- Vynucení izolace: jen ruční scope v dotazech / query filtry + kontrola zápisu. Platí query
  filtry a kontrola v `SaveChanges`.
- Vznik účtů: registrace v UI / jen seed. Platí jen seed.

## Dopady

- `IOwnerContext` a `PrincipalOwnerContext` zanikají; nahrazuje je `ITenantContext`.
- Sloupce `OwnerId` zanikají; nahrazuje je `TenantId` + `CreatedBy` (SDR-002).
- `.claude/rules/business.md` (Ownership) se mění s kódem M2.
