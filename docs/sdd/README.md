# SDD — návrhové dokumenty

SDD (software design document) závazně popisuje jednu oblast návrhu EvilCase.
`../product/vision.md` říká, co se staví; SDD říká jak. Kód, který SDD falzifikuje, ho mění
ve stejném pull requestu.

## Konvence

- Česky, styl podle `.claude/rules/writing.md`; deklarativně, v přítomném čase.
- Struktura podle [sdd-000-template.md](sdd-000-template.md).
- Soubory `sdd-NNN-<slug>.md`, slug bez diakritiky.
- Příklady jen syntetické — repozitář je veřejný.

## Pořadí čtení

Základ 005–007 (tenance, model, číslování), agendy 008–012, aplikace 013–016; průřezová
001–004 platí pro každou změnu ve své oblasti.

## Mapa SDD ↔ milník

| SDD | Téma | Milníky |
| --- | --- | --- |
| [001](sdd-001-logovani-a-observabilita.md) | Logování a observabilita | průřez |
| [002](sdd-002-testovani.md) | Testování | průřez |
| [003](sdd-003-validace-a-chyby.md) | Validace a chyby | průřez |
| [004](sdd-004-api-konvence.md) | API konvence | průřez |
| [005](sdd-005-tenance-a-ucty.md) | Tenance a účty | M2 |
| [006](sdd-006-domenovy-model.md) | Doménový model | M2 |
| [007](sdd-007-cislovani.md) | Číslování | M2 |
| [008](sdd-008-spisy.md) | Spisy | M3 |
| [009](sdd-009-ukony.md) | Úkony | M4 |
| [010](sdd-010-kontakty.md) | Kontakty | M2, M3, M4, M6 |
| [011](sdd-011-soubory.md) | Soubory | M2, M5 |
| [012](sdd-012-komentare.md) | Komentáře | M3, M4 |
| [013](sdd-013-vyhledavani.md) | Vyhledávání | M7 |
| [014](sdd-014-dashboard.md) | Dashboard | M7 |
| [015](sdd-015-navigace-a-vzhled.md) | Navigace a vzhled | M1 |
| [016](sdd-016-seed-vzorovych-dat.md) | Seed vzorových dat | M2 |
