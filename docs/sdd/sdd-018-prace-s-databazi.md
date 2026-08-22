# SDD-018 — Práce s databází

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [001](sdd-001-architektura.md), [003](sdd-003-testovani.md),
  [006](sdd-006-tenance-a-ucty.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Jak aplikace čte a zapisuje: přístup ke kontextu, jeho životnost, transakce a interceptory.
Schéma drží SDD-007, izolaci tenantů SDD-006.

## Popis

### Přístup ke kontextu

- Aplikace čte a zapisuje přes `IDbSession.Current`; nic jiného mezi ní a
  `ApplicationDbContext` nestojí.
- `Current` vrací `ApplicationDbContext` aktuálního DI scope. Vzniká až při prvním použití.
- `ApplicationDbContext` umírá s DI scope. Mezi scopy — tedy mezi požadavky — neuniká nikdy.
- Kdo potřebuje vlastní scope (seed, úloha na pozadí), zakládá ho sám a dostane vlastní kontext.
- Čte i zapisuje se přes typovaný `DbSet` entity (`dbSession.Current.Users`); `Set<TEntity>()`
  ani `Add` nad kontextem se nepoužívají.

### Transakce

- Transakci otevírá vrstva nad kontextem: ta, která zakládá DI scope. Jednotlivý zápis
  transakci neotevírá a `SaveChanges` volá sám za sebe.
- Zápisy, které musí platit společně, běží v jedné transakci nad jedním kontextem.

### Interceptory

- `TimestampInterceptor` plní `Created` a `Updated` nad `TimeProvider`.
- `TenantWriteInterceptor` doplní tenanta z `ITenantContext` nové tenantové entitě, která ho
  nemá, a odmítne zápis entity, jejíž tenant se s kontextem neshoduje (SDD-006). Stejně doplní
  `UserId` z `IUserContext` nové entitě `IUserOwnedEntity`; zápis bez přihlášeného uživatele,
  který `UserId` nenese, skončí chybou a zápis pod jiným uživatelem je odmítnut.
- `ExecuteUpdate` a `ExecuteDelete` jdou mimo interceptory: co jinak plní interceptor, nastavuje
  takový zápis sám.

### Migrace

Migrace nejsou přístup k datům aplikace: `DatabaseMigrator` drží `ApplicationDbContext` přímo
a běží ve vlastním scope dřív, než se obslouží první požadavek.

## Rozhodnutí

- Přístup k datům: `DbContext` přímo ve službách / accessor nad DI scope. Platí accessor.
- Životnost `DbContext`: ruční správa / scoped z DI. Platí scoped z DI.
- Transakce: uvnitř každého zápisu / o úroveň výš. Platí o úroveň výš.
- Repository per entita: ano / ne. Ne — typovaný `DbSet` na `IDbSession.Current` stačí.

## Dopady

`ApplicationDbContext` přestává být závislostí služeb v `Business` a `Auth`. SDD-001 jmenuje
`EvilCase.Data` jako model i přístup k databázi; `.claude/rules/data.md` nese invariant.
