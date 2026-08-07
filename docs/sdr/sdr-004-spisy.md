# SDR-004 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDR:** [002](sdr-002-domenovy-model.md), [003](sdr-003-cislovani.md),
  [006](sdr-006-kontakty.md), [008](sdr-008-komentare.md)

## Rozsah

Entita spisu, hierarchie, externí značky, stránky spisů a mazání. Číslování patří SDR-003,
soubory SDR-007, komentáře SDR-008.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, explicitní datum (`DateOnly`), název, popis, stav
`Active` / `WaitingOnAuthority` / `Closed`. Bez tagů.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný; hlídá ho zápis v business
  vrstvě.
- UI zobrazuje jen ploché seznamy: detail spisu ukazuje odkaz na rodiče a seznam přímých
  podřízených spisů. Žádný strom.

### Externí značky

Spis nese N externích značek (`ExternalCaseNumber`): hodnota volným textem a povinný
kontakt, který značku přidělil. Hodnota je unikátní per spis. Spravují se na editaci spisu.

### Stránky

- `/cases` — seznam spisů: číslo, název, stav, datum; hledání (SDR-009).
- `/cases/{id}` — detail: údaje, značky, podřízené spisy, úkony, soubory, komentáře.
- `/cases/{id}/edit` — založení a editace.

### Mazání

Smazání spisu bere kaskádou úkony, komentáře, značky a soubory; potvrzení jmenuje, co
kaskáda bere. Podřízené spisy přežijí bez rodiče.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí osiření — rodič se vynuluje.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.

## Dopady

`CaseRelation` a `CaseTag` zanikají (SDR-002). Značky vyžadují kontakt (SDR-006). Přesná
shoda značky naviguje (SDR-009).
