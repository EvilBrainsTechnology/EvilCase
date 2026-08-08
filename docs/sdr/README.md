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

Základ 005–007 (tenance, model, číslování), agendy 008–012, aplikace 013–016; průřezová
001–004 platí pro každou změnu ve své oblasti.

## Mapa SDR ↔ milník

| SDR | Téma | Milníky |
| --- | --- | --- |
| [001](sdr-001-logovani-a-observabilita.md) | Logování a observabilita | průřez |
| [002](sdr-002-testovani.md) | Testování | průřez |
| [003](sdr-003-validace-a-chyby.md) | Validace a chyby | průřez |
| [004](sdr-004-api-konvence.md) | API konvence | průřez |
| [005](sdr-005-tenance-a-ucty.md) | Tenance a účty | M2 |
| [006](sdr-006-domenovy-model.md) | Doménový model | M2 |
| [007](sdr-007-cislovani.md) | Číslování | M2 |
| [008](sdr-008-spisy.md) | Spisy | M3 |
| [009](sdr-009-ukony.md) | Úkony | M4 |
| [010](sdr-010-kontakty.md) | Kontakty | M2, M6 |
| [011](sdr-011-soubory.md) | Soubory | M5 |
| [012](sdr-012-komentare.md) | Komentáře | M3, M4 |
| [013](sdr-013-vyhledavani.md) | Vyhledávání | M7 |
| [014](sdr-014-dashboard.md) | Dashboard | M7 |
| [015](sdr-015-navigace-a-vzhled.md) | Navigace a vzhled | M1 |
| [016](sdr-016-seed-vzorovych-dat.md) | Seed vzorových dat | M2 |
