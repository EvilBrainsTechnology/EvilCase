# SDR-012 — Komentáře

- **Stav:** platí
- **Milníky:** M3, M4
- **Související SDR:** [006](sdr-006-domenovy-model.md), [008](sdr-008-spisy.md),
  [009](sdr-009-ukony.md)

## Rozsah

Komentáře spisů a úkonů.

## Popis

- Comment patří právě jednomu spisu XOR úkonu (check constraint). Tělo je neomezený text.
- Autor je `CreatedBy`. Editovat a smazat komentář smí jen autor; vynucuje to business
  vrstva.
- UI: chronologický seznam na detailu spisu a úkonu, přidání inline, mazání s potvrzením.
- Autor se zobrazuje jen u komentářů, e-mailem uživatele — User jiné jméno nemá; jinde v UI
  `CreatedBy` nefiguruje.

## Rozhodnutí

- Editace cizího komentáře: kdokoli v tenantu / jen autor. Platí jen autor.
- Zobrazení autora: všude / jen u komentářů. Platí jen u komentářů.

## Dopady

Komentáře spisů přicházejí s M3, komentáře úkonů s M4. Smazání spisu a úkonu bere komentáře
kaskádou (SDR-006).
