# SDD-010 — Úkony

- **Stav:** platí
- **Milníky:** M4
- **Související SDD:** [008](sdd-008-cislovani.md), [009](sdd-009-spisy.md),
  [011](sdd-011-kontakty.md), [013](sdd-013-komentare.md)

## Rozsah

Entita úkonu, směr a kontakty, externí čísla jednací, stránky, řazení a mazání. Soubory
patří SDD-012, komentáře SDD-013.

## Popis

### Entita

Act: `CaseId`, `ActNumber` (SDD-008), název, explicitní datum (`DateOnly`), popis, směr
`Incoming` / `Outgoing`. Odesílatel je povinný kontakt, příjemce nepovinný kontakt.

### Předvyplnění

Odchozí úkon předvyplní odesílatele defaultním kontaktem uživatele, příchozí příjemce
(SDD-011). Obojí jde před uložením volně změnit.

### Externí čísla jednací

Úkon nese N externích čísel jednacích (`ExternalActNumber`): hodnota volným textem a povinný
kontakt, který číslo přidělil. Tabulka, ne sloupec; hodnota unikátní per úkon.

### Stránky a řazení

- `/cases/{id}/act/new` — založení.
- `/cases/{id}/act/{actId}` — detail: údaje, externí čísla, komentáře; sekce souborů přibývá
  v M5 (SDD-012).
- `/cases/{id}/act/{actId}/edit` — editace.
- Seznam úkonů žije v detailu spisu a řadí se podle data úkonu vzestupně; shodná data řadí
  `Created`.

### Mazání

Mazání řídí matice v SDD-007; potvrzení jmenuje, co kaskáda bere.

## Rozhodnutí

- Odesílatel: zamčený na defaultní kontakt / volně změnitelný. Platí volně změnitelný.
- Řazení: datum + pořadové číslo / jen datum. Viditelné řazení je datum úkonu; shodná data
  řadí deterministicky `Created`.
- Externí číslo: jeden sloupec / N řádků s kontaktem. Platí N řádků s kontaktem.

## Dopady

Sloupec `Act.ExternalActNumber` zaniká (SDD-007).
