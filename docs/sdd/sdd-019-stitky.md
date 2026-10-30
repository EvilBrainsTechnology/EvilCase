# SDD-019 — Štítky a nastavení

- **Stav:** platí
- **Milníky:** M8
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [007](sdd-007-domenovy-model.md),
  [009](sdd-009-spisy.md), [010](sdd-010-ukony.md), [016](sdd-016-navigace-a-vzhled.md),
  [022](sdd-022-export.md)

## Rozsah

Entita štítku, jeho přidělení spisu a úkonu, nastavení a jeho sekce a zobrazení štítků. Stránky
spisu drží SDD-009, stránky úkonu SDD-010, routy a menu SDD-016.

## Popis

### Entita

Label: název a barva. Název má nejvýše 64 znaků a v tenantu je jedinečný; uloží se bez okolních
mezer. Barva je hodnota pevné palety, nikdy volný kód: `Blue`, `Azure`, `Indigo`, `Purple`,
`Pink`, `Red`, `Orange`, `Yellow`, `Lime`, `Green`, `Teal`, `Cyan`, `Gray`.

LabelAssignment: jeden štítek na jednom spisu, nebo na jednom úkonu, nikdy na obojím a nikdy na
ničem. Tutéž dvojici nese nejvýše jednou. Spis ani úkon nemá počet štítků omezený.

Číselník štítků je jeden pro spisy i úkony.

### Nastavení

Nastavení nese sekce Štítky (`/settings/labels`), API tokeny (`/settings/api-tokens`) a Export
(`/settings/export`), každou na vlastní stránce se společnou navigací sekcí. Na desktopu je
navigace svislým panelem, na mobilu řadou pilulek. `/settings` přesměruje na Štítky.

Sekce Štítky ukazuje štítky seřazené podle názvu, u každého počet spisů a počet úkonů, které ho
nesou, a nechá štítek založit, přejmenovat, přebarvit a smazat. Název, který už nese jiný štítek,
se odmítne 409 (SDD-004). Smazání štítku ho sejme ze všech spisů a úkonů, které ho nesly; spis ani
úkon tím nezaniká. Potvrzuje se jako každé mazání a potvrzení jmenuje, kolik spisů a úkonů štítek
nese.

Sekce API tokeny ukazuje tokeny přihlášeného uživatele (SDD-006), nejnovější nahoře: název,
oprávnění, kdy token vznikl, do kdy platí a kdy byl naposledy použit. Prošlý token nese
„Vypršel“, dosud nepoužitý „Nepoužit“. „Nový token“ bere název (povinný), oprávnění (výchozí
Jen čtení) a platnost 30, 90 nebo 365 dní (výchozí 90). Po založení se token ukáže jedinkrát,
s tlačítkem pro zkopírování a upozorněním, že ho uživatel znovu neuvidí. „Zrušit“ token zruší po
potvrzení jako každé mazání; zrušený token ze seznamu zmizí.

Sekce Export popíše, co archiv nese a co ne, a nese tlačítko Exportovat vše s názvem souboru,
pod kterým se archiv stáhne. Export řídí [SDD-022](sdd-022-export.md).

### Přidělení

Štítky se přidělují na dvou místech:

- Formulář založení i editace spisu a úkonu nese výběr štítků; odešlou se spolu se zbytkem
  formuláře.
- Detail spisu a úkonu nese našeptávač, který štítek přidá nebo sejme rovnou, bez editace.

Výběr štítků nabízí jen štítky, které entita ještě nenese. Text, který nesedí na název žádného
štítku, nabídne založení nového štítku s volbou barvy z palety; založený štítek se přidělí hned.

Obojí posílá celou množinu, kterou entita nese po zápisu: štítek, který v ní není, se sejme.
Štítek, který tenant nemá, odmítne zápis 409 a nezapíše nic.

### Zobrazení

Detail spisu i úkonu nese štítky s názvem a barvou.

Seznam spisů i úkonů nese štítky jako drobné značky s názvem a barevnou tečkou hned za názvem
spisu nebo úkonu, na každé šířce.

## Rozhodnutí

- Štítky v seznamu: barevné tečky s názvem v tooltipu / drobné značky s názvem. Platí drobné značky
  s názvem.
- Barva: volný kód / pevná paleta. Platí pevná paleta — drží čitelný kontrast.
- Číselník: společný pro spisy i úkony / dva oddělené. Platí společný.
- Smazání štítku, který něco nese: zákaz / smazání i s jeho přiděleními. Platí smazání
  i s přiděleními; spis ani úkon, který štítek nesl, tím nezaniká.
- Zápis přidělení: přidání a odebrání po jednom / celá množina. Platí celá množina.
- Nastavení: vlastní stránka / sekce v profilu uživatele. Platí vlastní stránky pod `/settings/`,
  jedna na sekci.

## Dopady

Mapa entit a matice mazání patří SDD-007, zdroje API SDD-005, routy a menu SDD-016, seed
SDD-017.
