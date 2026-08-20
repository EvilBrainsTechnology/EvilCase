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

- Název a popis spisu i úkonu hledá PostgreSQL fulltext bez ohledu na diakritiku a velikost
  písmen: `tsvector` s konfigurací `simple` (tokeny převádí na malá písmena) a `unaccent`,
  přes GIN expression indexy nad stávajícími sloupci. Žádný uložený ani generovaný sloupec.
- `unaccent` není IMMUTABLE; indexy volají IMMUTABLE obálku. Rozšíření, obálku i indexy
  zakládá migrace `Init` (SDD-007) — M7 migraci nepotřebuje.
- Dotaz je prefixová `tsquery`.
- `CaseNumber`, `ActNumber` a hodnoty externích čísel hledá zvlášť ILIKE contains nad
  vlastními sloupci; ILIKE nerozlišuje velikost písmen. Dotazy jdou po indexu: rozšíření
  `pg_trgm` a GIN trigram indexy nad těmito sloupci, obojí zakládá `Init` (SDD-007).
- Jeden endpoint kombinuje obě větve; vrací spisy i úkony dohromady.
- Dotazy jdou přes fulltextové funkce Npgsql / EF Core, bez raw SQL.

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
