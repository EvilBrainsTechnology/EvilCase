# SDR-015 — Testování

- **Stav:** platí
- **Milníky:** průřez
- **Související SDR:** [001](sdr-001-tenance-a-ucty.md), [003](sdr-003-cislovani.md),
  [007](sdr-007-soubory.md), [009](sdr-009-vyhledavani.md), [012](sdr-012-seed-vzorovych-dat.md)

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
  v unikátních indexech (SDR-001).
- Testy číslování: formát, pořadí per den, přetečení, zpětné datování, ruční přepis
  (SDR-003).
- Souborové úložiště na temp adresáři: zápis, atomicita, smazání blobu (SDR-007).
- Fold diakritiky a plnění `SearchText` (SDR-009).
- Smoke test seedu: seed proběhne a založí spis se stromem (SDR-012).

## Rozhodnutí

- Testy úložiště: mock souborového systému / temp adresář. Platí temp adresář.

## Dopady

Testy `CaseRelation`, `CaseTag` a `PrincipalOwnerContext` zanikají se svými typy v M2
(SDR-002).
