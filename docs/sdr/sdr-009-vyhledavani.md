# SDR-009 — Vyhledávání

- **Stav:** platí
- **Milníky:** M7
- **Související SDR:** [003](sdr-003-cislovani.md), [004](sdr-004-spisy.md),
  [005](sdr-005-ukony.md), [015](sdr-015-testovani.md)

## Rozsah

Fulltextové hledání nad spisy a úkony a navigace přesnou shodou. Fulltext nad obsahem
souborů je non-goal.

## Popis

### Rozsah hledání

Spisy a úkony: název, popis, `CaseNumber` / `ActNumber`, externí čísla. Bez ohledu na
diakritiku a velikost písmen.

### Technika

- Spis i úkon nesou normalizovaný sloupec `SearchText`: hledaná pole složená dohromady,
  lowercase, diakritika odstraněná v .NET. Plní ho každý zápis entity.
- Dotaz se normalizuje stejně a hledá se `LIKE '%…%'`.
- Jeden endpoint vrací spisy i úkony dohromady.

### Navigace přesnou shodou

- Přesná shoda `CaseNumber` nebo `ActNumber` naviguje rovnou na entitu.
- Přesná shoda externího čísla naviguje jen, když odpovídá právě jedné entitě; jinak se
  ukáže seznam výsledků.

### UI

Hledací pole na dashboardu a v seznamu spisů; debounce, hledá se od 2 znaků.

## Rozhodnutí

- Technika: PostgreSQL tsvector / normalizovaný sloupec + LIKE. Platí sloupec + LIKE.
- Fold diakritiky: `unaccent` v databázi / .NET. Platí .NET — jedna implementace pro zápis
  i dotaz.
- Výsledky: oddělené endpointy per entita / jeden endpoint. Platí jeden endpoint.

## Dopady

Dnešní ILIKE hledání v `CaseListQuery` nahrazuje `SearchText`. Fold diakritiky má test
(SDR-015).
