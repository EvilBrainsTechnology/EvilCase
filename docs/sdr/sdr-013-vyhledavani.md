# SDR-013 — Vyhledávání

- **Stav:** platí
- **Milníky:** M7
- **Související SDR:** [002](sdr-002-testovani.md), [006](sdr-006-domenovy-model.md),
  [007](sdr-007-cislovani.md), [008](sdr-008-spisy.md), [009](sdr-009-ukony.md)

## Rozsah

Fulltextové hledání nad spisy a úkony a navigace přesnou shodou. Fulltext nad obsahem
souborů je non-goal.

## Popis

### Rozsah hledání

Spisy a úkony: název, popis, `CaseNumber` / `ActNumber`, externí čísla. Bez ohledu na
diakritiku a velikost písmen.

### Technika

- Název a popis spisu i úkonu hledá PostgreSQL fulltext bez ohledu na diakritiku: `tsvector`
  s konfigurací `simple` a `unaccent`, přes GIN expression indexy nad stávajícími sloupci.
  Žádný uložený ani generovaný sloupec.
- `unaccent` není IMMUTABLE; indexy volají IMMUTABLE obálku. Rozšíření, obálku i indexy
  zakládá migrace `Init` (SDR-006) — M7 migraci nepotřebuje.
- Dotaz je prefixová `tsquery`.
- `CaseNumber`, `ActNumber` a hodnoty externích čísel hledá zvlášť ILIKE contains nad
  vlastními sloupci — tabulky jsou malé, bez indexu.
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

- Technika: normalizovaný uložený sloupec + LIKE / fulltext nad stávajícími sloupci. Uložený
  sloupec owner zamítl; platí fulltext.
- Fold diakritiky: .NET / `unaccent` v databázi. Platí `unaccent`.
- Výsledky: oddělené endpointy per entita / jeden endpoint. Platí jeden endpoint.

## Dopady

Dnešní ILIKE hledání v `CaseListQuery` zaniká; nahrazuje ho vyhledávací endpoint. Fold
diakritiky má test (SDR-002).
