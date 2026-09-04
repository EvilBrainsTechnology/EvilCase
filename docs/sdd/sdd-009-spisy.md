# SDD-009 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDD:** [007](sdd-007-domenovy-model.md), [008](sdd-008-cislovani.md),
  [011](sdd-011-kontakty.md), [013](sdd-013-komentare.md)

## Rozsah

Entita spisu, hierarchie, externí značka, stránky spisů a mazání. Číslování patří SDD-008,
úkony SDD-010, soubory SDD-012, komentáře SDD-013.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, `ExternalCaseNumber?`, explicitní datum (`DateOnly`), název,
popis, stav `Active` / `WaitingOnAuthority` / `Closed`, nepovinný kontakt protistrany (SDD-011).

Stav je jen štítek: na nic se neváže, spis ve stavu `Closed` jde editovat a přijímá úkony,
soubory i komentáře jako každý jiný. Nový spis vzniká jako `Active` a s dnešním datem.

Délky: název nejvýše 256 znaků, popis 4000, externí značka 128, spisová značka 64. Povinné jsou
název, datum a stav, v editaci i spisová značka; ostatní pole jsou nepovinná.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný: uložení, které by ho uzavřelo,
  vrací 409 (SDD-004).
- Podřízený spis se zakládá z detailu rodiče a rodiče na formuláři jen ukazuje; v editaci spisu
  jde rodič vybrat z kterýchkoli spisů tenantu kromě samotného spisu. Cyklus se pozná až při
  uložení.
- UI zobrazuje jen ploché seznamy: detail spisu ukazuje odkaz na rodiče a seznam přímých
  podřízených spisů. Žádný strom.

### Externí spisová značka

Spis nese nejvýše jednu značku, kterou mu dal jiný úřad: nepovinný volný text bez vazby na
kontakt. Zadává se na editaci spisu.

### Stránky

- `/cases` — seznam spisů: číslo, název, stav, datum. Řadí se podle data spisu sestupně,
  shodná data řadí `Created`; bez stránkování. Hledací pole hledá v názvu a popisu bez ohledu na
  diakritiku. Filtr stavu nabízí Otevřené (vše kromě uzavřených), Všechny stavy a každý stav
  zvlášť; výchozí je Otevřené.
- `/cases/new` — založení, včetně kontaktu; `?parent={id}` zakládá podřízený spis.
- `/cases/{id}` — detail: údaje, podřízené spisy, úkony (SDD-010), komentáře (SDD-013),
  soubory (SDD-012).
- `/cases/{id}/edit` — editace, včetně kontaktu a rodiče.

### Mazání

Mazání řídí matice v SDD-007. Potvrzení jmenuje, co kaskáda bere, a u spisu s podřízenými
spisy jejich počet a to, že zůstanou bez rodiče.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí osiření — rodič se vynuluje.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.
- Stav spisu: řídí chování / jen štítek. Platí jen štítek.
- Externí značka: N řádků s kontaktem / jeden sloupec. Platí jeden sloupec bez kontaktu.
- Kontakt spisu: povinný / nepovinný. Platí nepovinný.
- Zadání kontaktu: jen editace / založení i editace. Platí obojí.

## Dopady

—
