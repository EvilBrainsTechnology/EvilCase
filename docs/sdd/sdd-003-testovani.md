# SDD-003 — Testování

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [008](sdd-008-cislovani.md),
  [012](sdd-012-soubory.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Vrstvy testů a povinné testy nových oblastí.

## Popis

### Platí dál

- NUnit v `Tests/EvilCase.Tests`; změna chování nese test (`.claude/rules/github.md`).
- `Architecture/LayerTests` pinují vrstvení.
- Modelové testy nad `IDesignTimeModel`: fixture per entita a konvenční testy.
- Hosting testy nad `EvilCaseHost`: autentizace, hlavičky, rate limiting, routing.
- Čistá doménová logika se testuje bez databáze.

### Nové povinnosti

- Konvenční test izolace tenantů: každá tenantová entita má query filter a `TenantId`
  v unikátních indexech (SDD-006).
- Testy číslování: formát, pořadí per den, přetečení, zpětné datování, ruční přepis
  (SDD-008).
- Souborové úložiště na temp adresáři: zápis, atomicita, smazání blobu (SDD-012).
- Smoke test seedu: seed proběhne a založí spis se stromem (SDD-017).
- Testy razítek nad reálnou PostgreSQL: hodnoty, které si zápis čte zpět, změna, která `Created`
  nechá být, zápis mimo EF a pokrytí každé mapované tabulky triggerem; bez serveru selžou s tím,
  co chybí, nepřeskakují se (SDD-018).
- Konvenční test smazání: každá entita kromě `RefreshToken` má filtr smazání a žádný filtr
  v modelu není bezejmenný (SDD-007).
- Testy smazání nad reálnou PostgreSQL: kaskáda pod jedním okamžikem, dřívější razítko, které
  pozdější kaskáda nepřepíše, obsazené číslo po smazání a kontakt, který drží i smazaný řádek
  (SDD-007).

## Rozhodnutí

- Testy úložiště: mock souborového systému / temp adresář. Platí temp adresář.
- Testy razítek: fake kontext / reálná databáze. Platí reálná databáze — razítko píší hodiny
  databáze, ne aplikace.
- Test doubles: ruční třídy / mockovací knihovna. Platí NSubstitute pro stub, který jen zaznamená
  argumenty a vrátí připravenou hodnotu; stavový fake si píše sám.

## Dopady

Testy `CaseRelation`, `CaseTag` a `PrincipalOwnerContext` zanikají se svými typy v M2
(SDD-006, SDD-007).
