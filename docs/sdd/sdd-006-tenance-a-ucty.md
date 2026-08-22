# SDD-006 — Tenance a účty

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [003](sdd-003-testovani.md), [005](sdd-005-api-konvence.md),
  [007](sdd-007-domenovy-model.md)

## Rozsah

Účty, tenanti, uživatelé, izolace dat mezi tenanty a rozšíření autentizace o tenanta.
Doménové entity popisuje SDD-007.

## Popis

### Model

- **Account** — id a název. Zastřešuje N tenantů.
- **Tenant** — id, název, `AccountId`. Hranice izolace dat.
- **User** — patří právě jednomu tenantu (`User.TenantId`). Dnešní sloupce (e-mail, hash
  hesla, role, lockout) zůstávají; přibývá `TenantId` a povinný `DefaultContactId` (SDD-011).

Každá tenantová entita nese `TenantId`. Vlastníka `UserId` nese každá kromě kontaktu; kontakt
patří tenantu (SDD-011). Obojí plní zápis, ne volající. Viditelná je v celém tenantu, zapsat a
změnit ji může jen její uživatel.

Account, Tenant a první administrátor vznikají jen seedem při startu
(`EvilBrains__EvilCase__Auth__Seed__*`, jen do prázdné tabulky uživatelů). Žádné UI pro
správu účtů, žádná registrace.

### Izolace

Data nesmí utéct mezi tenanty; únik je kritická chyba.

- Každá tenantová entita má EF global query filter na `TenantId`; tenant i uživatele dodává
  `IUserContext`.
- Access token nese tenant claim i subject claim; `IUserContext` je čte z principalu.
- `SaveChanges` doplní `TenantId` nové tenantové entitě z kontextu a zápis do cizího tenanta
  odmítne.
- `SaveChanges` doplní `UserId` nové uživatelské entitě z `IUserContext` a zápis, změnu i
  smazání řádku jiného uživatele odmítne.
- Seed běží pod explicitním scope `IUserContext`, takže prochází stejnou kontrolou jako
  požadavek; mimo požadavek se tenant a uživatel nastavují jen společně.
- Unikátní indexy tenantových entit jsou kompozitní s `TenantId`.
- Konvenční test hlídá, že žádná tenantová entita filtr nepostrádá (SDD-003).

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
- Plnění UserId: volající / interceptor. Platí interceptor; jen prázdnou hodnotu na novém řádku.
- Zápis cizího řádku uvnitř tenantu: povolený / odmítnutý. Platí odmítnutý — řádek jiného
  uživatele je v tenantu vidět, ale zapsat, změnit ani smazat ho nelze.

## Dopady

- `IOwnerContext` a `PrincipalOwnerContext` zanikají; nahrazuje je `IUserContext`.
- Sloupce `OwnerId` zanikají; nahrazuje je `TenantId` + `UserId` (SDD-007).
- `.claude/rules/business.md` (Ownership) se mění s kódem M2.
