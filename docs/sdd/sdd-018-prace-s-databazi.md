# SDD-018 — Práce s databází

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [001](sdd-001-architektura.md), [003](sdd-003-testovani.md),
  [006](sdd-006-tenance-a-ucty.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Jak aplikace čte a zapisuje: session, životnost `DbContext`, transakce a interceptory. Schéma
drží SDD-007, izolaci tenantů SDD-006.

## Popis

### Session

- Aplikace pracuje s `IApplicationDbSession`, ne s `ApplicationDbContext`. Rozhraní i jeho
  implementace žijí v `EvilCase.Data`.
- Session umí čtení (`Query<TEntity>()`), zápis (`Add`, `SaveChanges`) a transakci
  (`BeginTransaction`). Nic jiného aplikace nad databází nedělá.
- `Query<TEntity>()` vrací `IQueryable` pod query filtry; dotaz se skládá z kroků v `Business`.

### Životnost DbContextu

- `IApplicationDbContextAccessor` vrací v `Current` `ApplicationDbContext` aktuálního DI scope.
  Vzniká až při prvním použití.
- `ApplicationDbContext` umírá s DI scope. Mezi scopy — tedy mezi požadavky — neuniká nikdy.
- Kdo potřebuje vlastní scope (seed, úloha na pozadí), zakládá ho sám a dostane vlastní kontext.

### Transakce

- Transakci otevírá vrstva nad session: ta, která zakládá DI scope. Jednotlivý zápis transakci
  neotevírá a `SaveChanges` volá sám za sebe.
- Zápisy, které musí platit společně, běží v jedné transakci nad jednou session — účet, tenant,
  administrátor a jeho defaultní kontakt při seedu (SDD-006).

### Interceptory

- `TimestampInterceptor` plní `Created` a `Updated` nad `TimeProvider`.
- `TenantWriteInterceptor` ověřuje při každém `SaveChanges`, že každý zapisovaný řádek tenantové
  entity nese tenanta z `ITenantContext` (SDD-006).
- `ExecuteUpdate` a `ExecuteDelete` jdou mimo interceptory: co jinak plní interceptor, nastavuje
  takový zápis sám.

### Migrace

Migrace nejsou přístup k datům aplikace: `DatabaseMigrator` drží `ApplicationDbContext` přímo
a běží ve vlastním scope dřív, než se obslouží první požadavek.

## Rozhodnutí

- Přístup k datům: `DbContext` přímo ve službách / session nad accessorem. Platí session.
- Životnost `DbContext`: ruční správa / scoped z DI. Platí scoped z DI.
- Transakce: uvnitř každého zápisu / o úroveň výš. Platí o úroveň výš.
- Repository per entita: ano / ne. Ne — `Query<TEntity>()` a skládané kroky dotazu stačí.

## Dopady

`ApplicationDbContext` přestává být závislostí služeb v `Business` a `Auth`. SDD-001 jmenuje
`EvilCase.Data` jako model i přístup k databázi; `.claude/rules/data.md` nese invariant.
