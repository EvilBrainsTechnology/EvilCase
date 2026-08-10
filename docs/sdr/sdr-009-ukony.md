# SDR-009 — Úkony

- **Stav:** platí
- **Milníky:** M4
- **Související SDR:** [007](sdr-007-cislovani.md), [008](sdr-008-spisy.md),
  [010](sdr-010-kontakty.md), [012](sdr-012-komentare.md)

## Rozsah

Entita úkonu, směr a kontakty, externí čísla jednací, stránky, řazení a mazání. Soubory
patří SDR-011, komentáře SDR-012.

## Popis

### Entita

Act: `CaseId`, `ActNumber` (SDR-007), název, explicitní datum (`DateOnly`), popis, směr
`Incoming` / `Outgoing`. Odesílatel je povinný kontakt, příjemce nepovinný kontakt.

### Předvyplnění

Odchozí úkon předvyplní odesílatele defaultním kontaktem uživatele, příchozí příjemce
(SDR-010). Obojí jde před uložením volně změnit.

### Externí čísla jednací

Úkon nese N externích čísel jednacích (`ExternalActNumber`): hodnota volným textem a povinný
kontakt, který číslo přidělil. Tabulka, ne sloupec; hodnota unikátní per úkon.

### Stránky a řazení

- `/cases/{id}/act/new` — založení.
- `/cases/{id}/act/{actId}` — detail: údaje, externí čísla, komentáře; sekce souborů přibývá
  v M5 (SDR-011).
- `/cases/{id}/act/{actId}/edit` — editace.
- Seznam úkonů žije v detailu spisu a řadí se podle data úkonu vzestupně; shodná data řadí
  `Created`.

### Mazání

Smazání úkonu bere kaskádou komentáře, externí čísla jednací a soubory (SDR-006); potvrzení
jmenuje, co kaskáda bere.

## Rozhodnutí

- Odesílatel: zamčený na defaultní kontakt / volně změnitelný. Platí volně změnitelný.
- Řazení: datum + pořadové číslo / jen datum. Viditelné řazení je datum úkonu; shodná data
  řadí deterministicky `Created`.
- Externí číslo: jeden sloupec / N řádků s kontaktem. Platí N řádků s kontaktem.

## Dopady

Sloupec `Act.ExternalActNumber` zaniká (SDR-006). Přesná shoda externího čísla naviguje jen
při jednoznačnosti (SDR-013).
