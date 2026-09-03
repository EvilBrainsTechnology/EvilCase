# SDD-009 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDD:** [007](sdd-007-domenovy-model.md), [008](sdd-008-cislovani.md),
  [013](sdd-013-komentare.md)

## Rozsah

Entita spisu, hierarchie, externí značka, stránky spisů a mazání. Číslování patří SDD-008,
soubory SDD-012, komentáře SDD-013.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, `ExternalNumber?`, explicitní datum (`DateOnly`), název,
popis, stav `Active` / `WaitingOnAuthority` / `Closed`. Bez tagů.

Stav je jen štítek: na nic se neváže, spis ve stavu `Closed` jde editovat a přijímá úkony,
soubory i komentáře jako každý jiný. Nový spis vzniká jako `Active`.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný; hlídá ho zápis v business
  vrstvě.
- Podřízený spis se zakládá z detailu rodiče; rodič jde nastavit i v editaci spisu.
- UI zobrazuje jen ploché seznamy: detail spisu ukazuje odkaz na rodiče a seznam přímých
  podřízených spisů. Žádný strom.

### Externí spisová značka

Spis nese nejvýše jednu značku, kterou mu dal jiný úřad: nepovinný volný text bez vazby na
kontakt. Zadává se na editaci spisu.

### Stránky

- `/cases` — seznam spisů: číslo, název, stav, datum. Řadí se podle data spisu sestupně,
  shodná data řadí `Created`; bez stránkování. Hledací pole hledá v názvu a popisu bez ohledu na
  diakritiku a filtr stavu s výchozí hodnotou Otevřené.
- `/cases/new` — založení.
- `/cases/{id}` — detail: údaje, podřízené spisy, komentáře; sekce úkonů přibývá
  v M4 (SDD-010), sekce souborů v M5 (SDD-012).
- `/cases/{id}/edit` — editace.

### Mazání

Mazání řídí matice v SDD-007; potvrzení jmenuje, co kaskáda bere.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí osiření — rodič se vynuluje.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.
- Stav spisu: řídí chování / jen štítek. Platí jen štítek.
- Externí značka: N řádků s kontaktem / jeden sloupec. Platí jeden sloupec bez kontaktu.

## Dopady

`CaseRelation`, `CaseTag` a `ExternalCaseNumber` zanikají (SDD-007).
