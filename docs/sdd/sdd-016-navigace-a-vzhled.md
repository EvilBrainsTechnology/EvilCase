# SDD-016 — Navigace a vzhled

- **Stav:** platí
- **Milníky:** M1
- **Související SDD:** [008](sdd-008-cislovani.md), [015](sdd-015-dashboard.md),
  [019](sdd-019-stitky.md), [020](sdd-020-vzhled.md)

## Rozsah

Routy aplikace, menu a vzhled. Obsah stránek drží SDD jednotlivých agend.

## Popis

### Routy

URL nesou UUID entit, nikdy jejich čísla:

| Routa | Obsah |
| --- | --- |
| `/` | dashboard (SDD-015) |
| `/cases` | seznam spisů |
| `/cases/new` | založení spisu |
| `/cases/{id}` | detail spisu |
| `/cases/{id}/edit` | editace spisu |
| `/acts` | seznam úkonů napříč spisy |
| `/acts/new?case={id}` | založení úkonu |
| `/acts/{actId}` | detail úkonu |
| `/acts/{actId}/edit` | editace úkonu |
| `/contacts` | kontakty |
| `/contacts/{id}` | detail kontaktu |
| `/settings` | přesměruje na `/settings/labels` |
| `/settings/labels` | nastavení, štítky (SDD-019) |
| `/settings/api-tokens` | nastavení, API tokeny (SDD-019) |
| `/settings/export` | nastavení, export dat tenanta (SDD-019, SDD-022) |
| `/login` | přihlášení |

Neznámá routa vykreslí stav nenalezeno uvnitř aplikace, neznámé id v routě prázdný stav
s tím, který záznam chybí.

### Menu a vzhled

Menu nese Přehled, Spisy, Úkony, Kontakty a Nastavení a zvýrazňuje položku i na podřízených
routách; menu je ve vodorovné liště nad obsahem. Na mobilu je menu ve spodní liště: pět
položek s ikonou nad popiskem, aktivní v barvě akcentu, a horní lišta nese jen značku,
hledání a uživatelské menu. Vizuální jazyk drží [SDD-020](sdd-020-vzhled.md):
tokeny, primitiva `Ec*` a předlohy obrazovek v [`docs/design/`](../design/README.md).
Desktop je primární, každá denně používaná obrazovka je plnohodnotná
i na mobilu a seznam se na mobilu nikdy neposouvá vodorovně. Datum se všude píše `d. M. yyyy`,
okamžik `d. M. yyyy H:mm` v časovém pásmu prohlížeče. Přehled píše dnešní a včerejší okamžik
relativně (SDD-015). Řádek tabulky se seznamem záznamů je odkaz na detail záznamu po celé šířce
a otevírá se na nové kartě nebo v novém okně jako kterýkoli jiný odkaz. Záhlaví sloupce
pojmenovává entitu, které sloupec patří. Buňka nese odkaz jen tam, kde vede jinam než řádek.

Každý seznam vypadá stejně; stránka volí jen sloupce, jejich pořadí a nabízené ovládání.
Seznam spisů i úkonů má za sloupcem Změněno poslední úzký sloupec Soubor s hlavním souborem
(SDD-009, SDD-010); datum spisu a úkonu je v každém seznamu tučné. Žádný seznam nezobrazuje číslo jednací ani spisovou značku.
Drobečky nad stránkou vedou přes všechny nadřízené spisy od kořenového a pojmenují spis, úkon
i kontakt názvem, nikdy číslem jednacím ani spisovou značkou.
Řaditelné záhlaví sloupce je tlačítko, na mobilu řadí select v liště. Lišta seznamu na mobilu
nese pod hledáním selecty se jménem v rámečku (stav, směr nebo druh, řazení) a tlačítko filtrů
s počtem aktivních filtrů. Přepínač je na mobilu select; data, štítek a zaškrtávací volby jsou ve
spodním panelu Filtry, který zavírá tlačítko s počtem výsledků („Zobrazit 7 spisů"). Seznam na víc stránek nese
v patičce zobrazený rozsah, celkový počet a předchozí/další; seznam na jednu stránku patičku nemá.

Hledání v horní liště prochází všechny entity ([SDD-021](sdd-021-vyhledavani.md)). Seznam spisů,
úkonů a kontaktů bere hledaný text v `?q=`.

Každý seznam má prázdný stav; kde jde záznam založit, nese výzvu k založení.

### Přihlášení

Každá stránka kromě `/login` vyžaduje přihlášení; nepřihlášeného aplikace přesměruje na
`/login` a po přihlášení ho vrátí tam, odkud přišel. Cíl mimo aplikaci se ignoruje a vede na `/`.

## Rozhodnutí

- Identifikátor v URL: `CaseNumber` / UUID. Platí UUID.
- Umístění menu: levý sloupec / vodorovná lišta. Platí vodorovná lišta.
- Menu na mobilu: zásuvka / spodní lišta. Platí spodní lišta.

## Dopady

- Vzhled menu i stránek se řídí [SDD-020](sdd-020-vzhled.md).
