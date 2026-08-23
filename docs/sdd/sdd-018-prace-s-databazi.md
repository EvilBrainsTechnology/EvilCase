# SDD-018 — Práce s databází

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [003](sdd-003-testovani.md), [006](sdd-006-tenance-a-ucty.md),
  [007](sdd-007-domenovy-model.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Jak aplikace čte a zapisuje: přístup ke kontextu, transakce a interceptory.
Schéma drží SDD-007, izolaci tenantů SDD-006.

## Popis

### Přístup ke kontextu

- Aplikace čte a zapisuje přes `IDbSession.Current`; nic jiného mezi ní a
  `ApplicationDbContext` nestojí. Invarianty přístupu drží `.claude/rules/data.md`.
- Kdo potřebuje vlastní scope (seed, úloha na pozadí), zakládá ho sám a dostane vlastní
  kontext.

### Transakce

- Transakci otevírá `IDbSession.BeginTransaction` ten, kdo drží celou jednotku práce;
  jednotlivý zápis ji neotevírá.
- Seed vzorových dat je celá jednotka práce: transakci i scope `IUserContext` si otevírá sám
  (SDD-017).

### Interceptory

- Zápis doplňuje a hlídá interceptor (SDD-006). `ExecuteUpdate` a `ExecuteDelete` jdou mimo
  interceptory: co jinak plní interceptor, nastavuje takový zápis sám; razítka plní trigger
  i jim.

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
- Čas razítka: čas transakce / čas řádku. Platí čas řádku — `Created` rozhoduje pořadí a seed
  píše celý strom v jedné transakci.
- `Updated`: jen při skutečné změně / při každém UPDATE. Platí každý UPDATE.

## Dopady

Čas drží databáze: `TimeProvider` patří `EvilCase.Auth`, ne `EvilCase.Data`.
