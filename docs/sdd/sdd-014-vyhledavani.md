# SDD-014 — Vyhledávání

- **Stav:** platí
- **Milníky:** M7
- **Související SDD:** [003](sdd-003-testovani.md), [007](sdd-007-domenovy-model.md),
  [008](sdd-008-cislovani.md), [009](sdd-009-spisy.md), [010](sdd-010-ukony.md)

## Rozsah

Fulltextové hledání nad spisy a úkony a navigace přesnou shodou. Fulltext nad obsahem
souborů je non-goal.

## Popis

### Rozsah hledání

Spisy a úkony: název, popis, `CaseNumber` / `ActNumber`, externí čísla. Bez ohledu na
diakritiku a velikost písmen.

### Technika

- Název a popis hledá databázový fulltext, čísla a externí čísla ILIKE contains; obě větve
  bez ohledu na diakritiku a velikost písmen, po indexech, které schéma nese od `Init`
  (SDD-007).
- Dotaz je prefixový.
- Jeden endpoint kombinuje obě větve; vrací spisy i úkony dohromady.

### Navigace přesnou shodou

- Přesná shoda `CaseNumber` nebo `ActNumber` naviguje rovnou na entitu.
- Přesná shoda externího čísla naviguje jen, když odpovídá právě jedné entitě; jinak se
  ukáže seznam výsledků.

### UI

Hledací pole na dashboardu a v seznamu spisů; debounce, hledá se od 2 znaků. Kombinované
výsledky — spisy i úkony — se ukazují v rozbalovacím seznamu pod polem, nejvýše 10 položek.
Navigace přesnou shodou se spouští jen Enterem nebo výběrem položky, nikdy během psaní.

## Rozhodnutí

- Technika: normalizovaný uložený sloupec + LIKE / fulltext nad stávajícími sloupci. Platí
  fulltext nad stávajícími sloupci.
- Fold diakritiky: .NET / `unaccent` v databázi. Platí `unaccent`.
- Výsledky: oddělené endpointy per entita / jeden endpoint. Platí jeden endpoint.

## Dopady

Dnešní ILIKE hledání v `CaseListQuery` zaniká; nahrazuje ho vyhledávací endpoint. Fold
diakritiky má test (SDD-003).
