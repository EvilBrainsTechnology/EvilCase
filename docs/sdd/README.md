# SDD — návrhové dokumenty

SDD (software design document) závazně popisuje jednu oblast návrhu EvilCase.
`../product/vision.md` říká, co se staví; SDD říká jak. Změny SDD řídí
`.claude/rules/instructions.md`.

## Konvence

- Česky, styl podle `.claude/rules/writing.md`; deklarativně, v přítomném čase.
- Struktura podle [sdd-000-template.md](sdd-000-template.md).
- Soubory `sdd-NNN-<slug>.md`, slug bez diakritiky.
- Příklady jen syntetické — repozitář je veřejný.

## Pořadí čtení

Architektura 001 jako první. Průřezová 002–005 a 018 platí pro každou změnu ve své oblasti;
základ 006–008 (tenance, model, číslování), agendy 009–013, aplikace 014–017.

## Mapa SDD ↔ milník

| SDD | Téma | Milníky |
| --- | --- | --- |
| [001](sdd-001-architektura.md) | Architektura | průřez |
| [002](sdd-002-logovani-a-observabilita.md) | Logování a observabilita | průřez |
| [003](sdd-003-testovani.md) | Testování | průřez |
| [004](sdd-004-validace-a-chyby.md) | Validace a chyby | průřez |
| [005](sdd-005-api-konvence.md) | API konvence | průřez |
| [006](sdd-006-tenance-a-ucty.md) | Tenance a účty | M2 |
| [007](sdd-007-domenovy-model.md) | Doménový model | M2 |
| [008](sdd-008-cislovani.md) | Číslování | M2 |
| [009](sdd-009-spisy.md) | Spisy | M3 |
| [010](sdd-010-ukony.md) | Úkony | M4 |
| [011](sdd-011-kontakty.md) | Kontakty | M2, M3, M4, M6 |
| [012](sdd-012-soubory.md) | Soubory | M2, M5 |
| [013](sdd-013-komentare.md) | Komentáře | M3, M4 |
| [014](sdd-014-vyhledavani.md) | Vyhledávání | M7 |
| [015](sdd-015-dashboard.md) | Dashboard | M7 |
| [016](sdd-016-navigace-a-vzhled.md) | Navigace a vzhled | M1 |
| [017](sdd-017-seed-vzorovych-dat.md) | Seed vzorových dat | M2 |
| [018](sdd-018-prace-s-databazi.md) | Práce s databází | průřez |
