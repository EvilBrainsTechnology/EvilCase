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

- Transakci otevírá `IDbSession.BeginTransaction` ten, kdo drží celou jednotku práce.
  Jednotlivý zápis transakci neotevírá a `SaveChanges` volá sám za sebe.
- Zápisy, které musí platit společně, běží v jedné transakci nad jedním kontextem.
- Seed vzorových dat je celá jednotka práce: transakci i scope `IUserContext` si otevírá sám
  (SDD-017).

### Interceptory

- `UserWriteInterceptor` doplní tenanta a uživatele z `IUserContext` a odmítne zápis, změnu i
  smazání entity, jejíž tenant nebo uživatel se s kontextem neshoduje (SDD-006).
- `ExecuteUpdate` a `ExecuteDelete` jdou mimo interceptory: co jinak plní interceptor, nastavuje
  takový zápis sám. Razítek se to netýká — ty plní trigger i jim.

### Razítka Created a Updated

- `Created` a `Updated` plní trigger v databázi, ne zápis: vloženému řádku nastaví `Created`
  a `Updated` nechá prázdné, změněnému nastaví `Updated` a `Created` nechá být. Obojí z hodin
  databáze, v okamžiku řádku.
- Trigger visí na každé tabulce, jejíž entita ta dvě pole nese; nová tabulka ho dostává
  v migraci, která ji zakládá.
- Model obě pole mapuje jako generovaná databází: zápis je neposílá a po uložení si je čte zpět.

### Migrace

Migrace nejsou přístup k datům aplikace: `DatabaseMigrator` drží `ApplicationDbContext` přímo
a běží ve vlastním scope dřív, než se obslouží první požadavek.

## Rozhodnutí

- Přístup k datům: `DbContext` přímo ve službách / accessor nad DI scope. Platí accessor.
- Životnost `DbContext`: ruční správa / scoped z DI. Platí scoped z DI.
- Transakce: uvnitř každého zápisu / o úroveň výš. Platí o úroveň výš.
- Transakce seedu: volající / seed sám. Platí seed sám.
- Repository per entita: ano / ne. Ne — typovaný `DbSet` na `IDbSession.Current` stačí.
- Razítka: interceptor / trigger. Platí trigger.
- Zdroj času triggeru: `now()` / `clock_timestamp()`. Platí `clock_timestamp()`, protože
  `Created` rozhoduje pořadí a seed píše celý strom v jedné transakci.
- `Updated`: jen při skutečné změně / při každém UPDATE. Platí každý UPDATE.

## Dopady

`ApplicationDbContext` přestává být závislostí služeb v `Business` a `Auth`. SDD-001 jmenuje
`EvilCase.Data` jako model i přístup k databázi; `.claude/rules/data.md` nese invariant.
Čas drží databáze: `TimeProvider` patří `EvilCase.Auth`, ne `EvilCase.Data`.
