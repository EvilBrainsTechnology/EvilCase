# SDD-013 — Komentáře

- **Stav:** platí
- **Milníky:** M3, M4
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [007](sdd-007-domenovy-model.md),
  [009](sdd-009-spisy.md), [010](sdd-010-ukony.md)

## Rozsah

Komentáře spisů a úkonů.

## Popis

- Comment patří právě jednomu spisu XOR úkonu. Tělo je neomezený text; ukládá se oříznuté
  a prázdné tělo je 400 (SDD-004).
- Autor je `UserId`. Editovat a smazat komentář smí jen jeho autor; komukoli jinému je to 403
  (SDD-004) a UI mu ovládací prvky nenabízí.
- UI: chronologický seznam na detailu spisu a úkonu, přidání a editace inline, mazání
  s potvrzením. Každý komentář nese autora, okamžik vzniku a u upraveného i okamžik úpravy.
- Autor se zobrazuje jen u komentářů, e-mailem uživatele — User jiné jméno nemá; jinde v UI
  `UserId` nefiguruje.

## Rozhodnutí

- Editace cizího komentáře: kdokoli v tenantu / jen autor. Platí jen autor.
- Zobrazení autora: všude / jen u komentářů. Platí jen u komentářů.

## Dopady

Mazání spisu a úkonu drží matice v SDD-007.
