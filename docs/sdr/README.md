# SDR — návrhové dokumenty

SDR (software design record) závazně popisuje jednu oblast návrhu EvilCase.
`../product/vision.md` říká, co se staví; SDR říká jak. Kód, který SDR falzifikuje, ho mění
ve stejném pull requestu.

## Konvence

- Česky, styl podle `.claude/rules/writing.md`; deklarativně, v přítomném čase.
- Struktura podle [sdr-000-template.md](sdr-000-template.md).
- Soubory `sdr-NNN-<slug>.md`, slug bez diakritiky.
- Příklady jen syntetické — repozitář je veřejný.

## Pořadí čtení

Základ 001–003 (tenance, model, číslování), agendy 004–008, aplikace 009–012; průřezová
013–016 platí pro každou změnu ve své oblasti.

## Mapa SDR ↔ milník

| SDR | Téma | Milníky |
| --- | --- | --- |
| [001](sdr-001-tenance-a-ucty.md) | Tenance a účty | M2 |
| [002](sdr-002-domenovy-model.md) | Doménový model | M2 |
| [003](sdr-003-cislovani.md) | Číslování | M2 |
| [004](sdr-004-spisy.md) | Spisy | M3 |
| [005](sdr-005-ukony.md) | Úkony | M4 |
| [006](sdr-006-kontakty.md) | Kontakty | M2, M6 |
| [007](sdr-007-soubory.md) | Soubory | M5 |
| [008](sdr-008-komentare.md) | Komentáře | M3, M4 |
| [009](sdr-009-vyhledavani.md) | Vyhledávání | M7 |
| [010](sdr-010-dashboard.md) | Dashboard | M7 |
| [011](sdr-011-navigace-a-vzhled.md) | Navigace a vzhled | M1 |
| [012](sdr-012-seed-vzorovych-dat.md) | Seed vzorových dat | M2 |
| [013](sdr-013-api-konvence.md) | API konvence | průřez |
| [014](sdr-014-validace-a-chyby.md) | Validace a chyby | průřez |
| [015](sdr-015-testovani.md) | Testování | průřez |
| [016](sdr-016-logovani-a-observabilita.md) | Logování a observabilita | průřez |
