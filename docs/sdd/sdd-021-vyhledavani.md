# SDD-021 — Vyhledávání

- **Stav:** platí
- **Milníky:** M9
- **Související SDD:** [005](sdd-005-api-konvence.md), [006](sdd-006-tenance-a-ucty.md),
  [016](sdd-016-navigace-a-vzhled.md), [019](sdd-019-stitky.md), [020](sdd-020-vzhled.md)

## Rozsah

Hledání napříč entitami z horní lišty: endpoint, pravidla shody a chování nabídky výsledků.
Vzhled nabídky drží [SDD-020](sdd-020-vzhled.md), předloha [`hledani.html`](../design/hledani.html).
Hledání uvnitř jednoho seznamu zůstává u SDD dané agendy.

## Popis

### Endpoint

`GET /api/search?q=` vrací šest skupin: spisy, úkony, kontakty, soubory, komentáře a štítky.
Skupina nese nejvýše 3 položky a celkový počet shod. Dotaz má 2 až 256 znaků a nezačíná ani
nekončí mezerou, jinak je odpověď 400. Hledá se jen v datech tenanta volajícího
([SDD-006](sdd-006-tenance-a-ucty.md)).

### Shoda

Shoda ignoruje velikost písmen i diakritiku a hledá podřetězec. Prohledává se:

- spis: název, číslo, číslo u úřadu;
- úkon: název, číslo, číslo u úřadu;
- kontakt: jméno, adresa, ID datové schránky;
- soubor: název souboru;
- komentář: text;
- štítek: název.

Spisy a úkony jdou od nejnovějšího data, ostatní skupiny od nejnovějšího založení.

### Nabídka v liště

- Pole je v horní liště na každé obrazovce; Ctrl K a Cmd K do něj přesunou kurzor.
- Nabídka se otevře od 2 znaků, psaní se zpožďuje; kratší text ukáže nápovědu, dotaz bez
  shody prázdný stav.
- Skupiny jdou v pořadí spisy, úkony, kontakty, soubory, komentáře, štítky; skupina nese počet.
- Řádek ukazuje druh entity ikonou a základní údaje; hledaný text je v názvu i v údajích
  zvýrazněný bez ohledu na diakritiku a velikost písmen.
- Šipky procházejí řádky přes skupiny dokola, Enter řádek otevře, Esc nabídku zavře; zavře ji
  i klik mimo ni.
- Spis, úkon a kontakt otevřou svůj detail; soubor a komentář detail vlastníka; štítek seznam
  spisů s tímto štítkem (`/cases?label={id}`).
- Skupina spisů, úkonů a kontaktů nabízí odkaz na seznam s předvyplněným hledáním (`?q=`).
- Na mobilu zabere nabídka obrazovku pod lištou a řádky mají dotykovou velikost.

## Rozhodnutí

- Endpoint: vlastní `/api/search` / skládání z výpisů entit. Platí vlastní endpoint; jeden
  požadavek, jedna odpověď.
- Stránkování: `Skip` a `Take` / pevná velikost skupiny. Platí pevná skupina o 3 položkách a
  celkový počet; úplný výpis je v seznamu dané entity.
- Hledání v popisu spisu: ano / ne. Platí ne; popis je dlouhý text mimo identifikaci spisu.

## Dopady

- [SDD-005](sdd-005-api-konvence.md): výpis hledání nestránkuje.
- [SDD-016](sdd-016-navigace-a-vzhled.md): seznamy berou `?q=` a spisy `?label=`.
- [SDD-020](sdd-020-vzhled.md): vzhled nabídky.
