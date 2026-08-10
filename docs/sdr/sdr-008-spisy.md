# SDR-008 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDR:** [006](sdr-006-domenovy-model.md), [007](sdr-007-cislovani.md),
  [010](sdr-010-kontakty.md), [012](sdr-012-komentare.md)

## Rozsah

Entita spisu, hierarchie, externí značky, stránky spisů a mazání. Číslování patří SDR-007,
soubory SDR-011, komentáře SDR-012.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, explicitní datum (`DateOnly`), název, popis, stav
`Active` / `WaitingOnAuthority` / `Closed`. Bez tagů.

Stav je jen štítek: na nic se neváže, spis ve stavu `Closed` jde editovat a přijímá úkony,
soubory i komentáře jako každý jiný. Nový spis vzniká jako `Active`.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný; hlídá ho zápis v business
  vrstvě.
- UI zobrazuje jen ploché seznamy: detail spisu ukazuje odkaz na rodiče a seznam přímých
  podřízených spisů. Žádný strom.

### Externí značky

Spis nese N externích značek (`ExternalCaseNumber`): hodnota volným textem a povinný
kontakt, který značku přidělil. Hodnota je unikátní per spis. Spravují se na editaci spisu.

### Stránky

- `/cases` — seznam spisů: číslo, název, stav, datum; hledání (SDR-013). Řadí se podle data
  spisu sestupně, shodná data řadí `Created`; bez stránkování.
- `/cases/new` — založení.
- `/cases/{id}` — detail: údaje, značky, podřízené spisy, úkony, soubory, komentáře.
- `/cases/{id}/edit` — editace.

### Mazání

Smazání spisu bere kaskádou úkony, komentáře, značky a soubory; potvrzení jmenuje, co
kaskáda bere. Podřízené spisy přežijí bez rodiče.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí osiření — rodič se vynuluje.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.
- Stav spisu: řídí chování / jen štítek. Platí jen štítek.

## Dopady

`CaseRelation` a `CaseTag` zanikají (SDR-006). Značky vyžadují kontakt (SDR-010). Přesná
shoda značky naviguje (SDR-013).
