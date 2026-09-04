# SDD-010 — Úkony

- **Stav:** platí
- **Milníky:** M4
- **Související SDD:** [008](sdd-008-cislovani.md), [009](sdd-009-spisy.md),
  [011](sdd-011-kontakty.md), [013](sdd-013-komentare.md)

## Rozsah

Entita úkonu, směr a kontakt, externí číslo jednací, stránky, řazení a mazání. Soubory
patří SDD-012, komentáře SDD-013.

## Popis

### Entita

Act: `CaseId`, `ActNumber` (SDD-008), `ExternalActNumber?`, název, explicitní datum (`DateOnly`),
popis, nepovinný kontakt protistrany a nepovinný směr `Incoming` / `Outgoing`. Směr a kontakt
platí jen spolu: buď je vyplněné obojí, nebo nic; jinak 400 s chybami u polí (SDD-004).

Délky: název nejvýše 256 znaků, popis 4000, externí číslo 128, číslo jednací 128. Název a datum
jsou povinné, ostatní pole nepovinná. Nový úkon vzniká s dnešním datem.

### Předvyplnění a upozornění

Výběr směru předvyplní kontakt z nadřízeného spisu, pokud je kontakt úkonu ještě prázdný; dál jde
volně změnit. Nese-li kontakt úkon i jeho spis a liší se, formulář i detail úkonu to hlásí
upozorněním, které jmenuje kontakt spisu; uložení to nebrání. Úkon bez kontaktu pod spisem
s kontaktem upozornění nevyvolá.

### Externí číslo jednací

Úkon nese nejvýše jedno číslo, které mu dal jiný úřad: nepovinný volný text bez vazby na
kontakt. Zadává se na editaci úkonu.

### Stránky a řazení

- `/cases/{id}/act/new` — založení.
- `/cases/{id}/act/{actId}` — detail: údaje, komentáře (SDD-013), soubory (SDD-012).
- `/cases/{id}/act/{actId}/edit` — editace.
- Seznam úkonů žije v detailu spisu, ukazuje datum, číslo jednací, směr, název a kontakt
  a řadí se podle data úkonu vzestupně; shodná data řadí `Created`.

### Mazání

Mazání řídí matice v SDD-007; potvrzení jmenuje, co kaskáda bere.

## Rozhodnutí

- Kontakty úkonu: odesílatel a příjemce / jeden kontakt protistrany. Platí jeden.
- Směr a kontakt: nezávislé / jen spolu. Platí jen spolu.
- Kontakt odlišný od spisu: zákaz / upozornění. Platí upozornění.
- Řazení: datum + pořadové číslo / jen datum. Viditelné řazení je datum úkonu; shodná data
  řadí deterministicky `Created`.
- Externí číslo: jeden sloupec / N řádků s kontaktem. Platí jeden sloupec bez kontaktu.

## Dopady

—
