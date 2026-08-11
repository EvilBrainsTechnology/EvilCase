# SDD-002 — Testování

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [005](sdd-005-tenance-a-ucty.md), [007](sdd-007-cislovani.md),
  [011](sdd-011-soubory.md), [013](sdd-013-vyhledavani.md), [016](sdd-016-seed-vzorovych-dat.md)

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
  v unikátních indexech (SDD-005).
- Testy číslování: formát, pořadí per den, přetečení, zpětné datování, ruční přepis
  (SDD-007).
- Souborové úložiště na temp adresáři: zápis, atomicita, smazání blobu (SDD-011).
- Fold diakritiky ve vyhledávání (SDD-013).
- Smoke test seedu: seed proběhne a založí spis se stromem (SDD-016).

## Rozhodnutí

- Testy úložiště: mock souborového systému / temp adresář. Platí temp adresář.

## Dopady

Testy `CaseRelation`, `CaseTag` a `PrincipalOwnerContext` zanikají se svými typy v M2
(SDD-005, SDD-006).
