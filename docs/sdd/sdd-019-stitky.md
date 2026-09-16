# SDD-019 — Štítky a nastavení

- **Stav:** platí
- **Milníky:** M8
- **Související SDD:** [007](sdd-007-domenovy-model.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md), [016](sdd-016-navigace-a-vzhled.md)

## Rozsah

Entita štítku, jeho přidělení spisu a úkonu, sekce nastavení a zobrazení štítků. Stránky spisu
drží SDD-009, stránky úkonu SDD-010, routy a menu SDD-016.

## Popis

### Entita

Label: název a barva. Název má nejvýše 64 znaků a v tenantu je jedinečný; uloží se bez okolních
mezer. Barva je hodnota pevné palety, nikdy volný kód: `Blue`, `Azure`, `Indigo`, `Purple`,
`Pink`, `Red`, `Orange`, `Yellow`, `Lime`, `Green`, `Teal`, `Cyan`, `Gray`.

LabelAssignment: jeden štítek na jednom spisu, nebo na jednom úkonu, nikdy na obojím a nikdy na
ničem. Tutéž dvojici nese nejvýše jednou. Spis ani úkon nemá počet štítků omezený.

Číselník štítků je jeden pro spisy i úkony.

### Nastavení

`/settings` je agenda nastavení tenantu; zatím nese jedinou sekci, Štítky. Sekce ukazuje štítky
seřazené podle názvu a nechá štítek založit, přejmenovat, přebarvit a smazat. Název, který už
nese jiný štítek, se odmítne 409 (SDD-004). Smazání štítku ho sejme ze všech spisů a úkonů, které
ho nesly; spis ani úkon tím nezaniká. Potvrzuje se jako každé mazání a potvrzení jmenuje, že
štítek zmizí i ze spisů a úkonů.

### Přidělení

Štítky se přidělují na dvou místech:

- Formulář založení i editace spisu a úkonu nese výběr štítků; odešlou se spolu se zbytkem
  formuláře.
- Detail spisu a úkonu nese našeptávač, který štítek přidá nebo sejme rovnou, bez editace.

Obojí posílá celou množinu, kterou entita nese po zápisu: štítek, který v ní není, se sejme.
Štítek, který tenant nemá, odmítne zápis 409 a nezapíše nic.

### Zobrazení

Detail spisu i úkonu nese štítky s názvem a barvou.

Seznam nese jen barevné tečky v barvách štítků, bez názvu; název drží tooltip. Karta seznamu na
mobilu, kde tooltip není, nese štítky s názvem.

## Rozhodnutí

- Barva: volný kód / pevná paleta. Platí pevná paleta — drží kontrast ve světlém i tmavém motivu.
- Číselník: společný pro spisy i úkony / dva oddělené. Platí společný.
- Smazání štítku, který něco nese: zákaz / smazání i s jeho přiděleními. Platí smazání
  i s přiděleními; spis ani úkon, který štítek nesl, tím nezaniká.
- Zápis přidělení: přidání a odebrání po jednom / celá množina. Platí celá množina.
- Nastavení: vlastní stránka / sekce v profilu uživatele. Platí vlastní stránka `/settings`.

## Dopady

Mapa entit a matice mazání patří SDD-007, zdroje API SDD-005, routy a menu SDD-016, seed
SDD-017.
