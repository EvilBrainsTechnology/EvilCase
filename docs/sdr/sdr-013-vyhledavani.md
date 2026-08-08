# SDR-013 — Vyhledávání

- **Stav:** platí
- **Milníky:** M7
- **Související SDR:** [002](sdr-002-testovani.md), [007](sdr-007-cislovani.md),
  [008](sdr-008-spisy.md), [009](sdr-009-ukony.md)

## Rozsah

Fulltextové hledání nad spisy a úkony a navigace přesnou shodou. Fulltext nad obsahem
souborů je non-goal.

## Popis

### Rozsah hledání

Spisy a úkony: název, popis, `CaseNumber` / `ActNumber`, externí čísla. Bez ohledu na
diakritiku a velikost písmen.

### Technika

- Spis i úkon nesou normalizovaný sloupec `SearchText`: hledaná pole složená dohromady,
  lowercase, diakritika odstraněná v .NET. Přepočítává ho každý zápis, který ho ovlivňuje —
  i přidání, editace nebo smazání externího čísla a ruční přepis čísla, ne jen editace
  vlastního řádku entity.
- Dotaz se normalizuje stejně a hledá se `LIKE '%…%'`.
- Jeden endpoint vrací spisy i úkony dohromady.

### Navigace přesnou shodou

- Přesná shoda `CaseNumber` nebo `ActNumber` naviguje rovnou na entitu.
- Přesná shoda externího čísla naviguje jen, když odpovídá právě jedné entitě; jinak se
  ukáže seznam výsledků.

### UI

Hledací pole na dashboardu a v seznamu spisů; debounce, hledá se od 2 znaků. Kombinované
výsledky — spisy i úkony — se ukazují v rozbalovacím seznamu pod polem, nejvýše 10 položek.
Navigace přesnou shodou se spouští jen Enterem nebo výběrem položky, nikdy během psaní.

## Rozhodnutí

- Technika: PostgreSQL tsvector / normalizovaný sloupec + LIKE. Platí sloupec + LIKE.
- Fold diakritiky: `unaccent` v databázi / .NET. Platí .NET — jedna implementace pro zápis
  i dotaz.
- Výsledky: oddělené endpointy per entita / jeden endpoint. Platí jeden endpoint.

## Dopady

Dnešní ILIKE hledání v `CaseListQuery` zaniká; nahrazuje ho `SearchText`. Fold diakritiky má
test (SDR-002).
