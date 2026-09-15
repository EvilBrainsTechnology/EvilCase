# SDD-018 — Práce s databází

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [003](sdd-003-testovani.md), [006](sdd-006-tenance-a-ucty.md),
  [007](sdd-007-domenovy-model.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Jak aplikace čte a zapisuje: přístup ke kontextu, transakce a razítka. Schéma drží SDD-007,
izolaci tenantů SDD-006. Invarianty přístupu drží `.claude/rules/data.md`.

## Popis

### Přístup ke kontextu

Aplikace čte a zapisuje přes jeden kontext na požadavek; nic jiného mezi ní a databází
nestojí. Kdo potřebuje vlastní rozsah — seed, úloha na pozadí — dostane vlastní kontext.

### Transakce

- Transakci otevírá ten, kdo drží celou jednotku práce; jednotlivý zápis ji neotevírá.
- Seed vzorových dat je celá jednotka práce a běží v jedné transakci (SDD-017).

### Hromadný zápis

Hromadná změna a hromadné smazání jdou mimo doplňování a kontroly, které jinak zápis provádí:
čím je řádka omezená, to musí takový zápis jmenovat sám. Nad tenantovou entitou zbývá filtr
tenantu a dopsat se musí uživatel — dnes to dělá jen komentář (SDD-013); zápisy přihlášení
a refresh tokeny filtr nemají a jmenují řádku jejím id.

### Razítka Created a Updated

- `Created` a `Updated` plní databáze, ne zápis: vloženému řádku nastaví `Created`
  a `Updated` nechá prázdné, změněnému nastaví `Updated` a `Created` nechá být. Obojí z hodin
  databáze, v okamžiku řádku.
- Razítko dostane každá tabulka, jejíž entita ta dvě pole nese; nová tabulka ho dostává
  v migraci, která ji zakládá.
- Zápis obě pole neposílá a po uložení si je čte zpět.

### Historizace

- Každý zápis do historizované tabulky — `Accounts`, `Tenants`, `Users`, `Contacts`, `Cases`,
  `Acts`, `FileAssets`, `Comments` — zůstává navždy jako řádek se svým operátorem a časem, i po
  smazání původního řádku. `RefreshTokens` historii nemá: rotují při každém refreshi a nesou hash
  tokenu.
- Historický řádek je jen k čtení, žádný zápis ho nezmění ani nesmaže.
- Operátor je ten, kdo byl přihlášený v okamžiku zápisu, ne vlastník řádku.

### Migrace

Migrace nejsou přístup k datům aplikace: běží ve vlastním rozsahu dřív, než se obslouží první
požadavek. Přepínač `EvilBrains__EvilCase__Database__MigrateOnStartup` je zapnutý a vypíná se
tam, kde se schéma vydává zvlášť nebo kde startuje víc instancí najednou. Neúspěšná migrace
aplikaci nespustí.

## Rozhodnutí

- Transakce: uvnitř každého zápisu / o úroveň výš. Platí o úroveň výš.
- Transakce seedu: volající / seed sám. Platí seed sám.
- Repository per entita: ano / ne. Ne.
- Razítka: doplňuje aplikace / doplňuje databáze. Platí databáze.
- Čas razítka: čas transakce / čas řádku. Platí čas řádku — `Created` rozhoduje pořadí a seed
  píše celý strom v jedné transakci.
- `Updated`: jen při skutečné změně / při každém UPDATE. Platí každý UPDATE.
- Historii zapisuje: aplikace / databáze. Platí databáze.
- Rozsah historizace: každá tabulka / vyjmenované tabulky. Platí vyjmenované tabulky.

## Dopady

—
