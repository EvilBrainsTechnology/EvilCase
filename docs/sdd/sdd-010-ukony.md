# SDD-010 — Úkony

- **Stav:** platí
- **Milníky:** M4
- **Související SDD:** [008](sdd-008-cislovani.md), [009](sdd-009-spisy.md),
  [011](sdd-011-kontakty.md), [013](sdd-013-komentare.md), [019](sdd-019-stitky.md)

## Rozsah

Entita úkonu, směr a kontakt, stav, externí číslo jednací, stránky, řazení a mazání. Soubory
patří SDD-012, komentáře SDD-013.

## Popis

### Entita

Act: `CaseId`, `ActNumber` (SDD-008), `ExternalActNumber?`, název, explicitní datum (`DateOnly`),
popis, štítky (SDD-019), nepovinný kontakt protistrany a nepovinný směr `Incoming` / `Outgoing`.
Směr a kontakt platí jen spolu: buď je vyplněné obojí, nebo nic; jinak 400 s chybami u polí
(SDD-004). Nepovinný stav `InProgress` (Rozpracováno) / `Sent` (Odesláno) na směru ani kontaktu
nezávisí; bez stavu ukazuje aplikace „—“. Nový úkon vzniká bez stavu, pokud ho založení nenese.

Délky: název nejvýše 256 znaků, popis 4000, externí číslo 128, číslo jednací 128. Povinné jsou
název a datum, v editaci i číslo jednací; ostatní pole jsou nepovinná. Nový úkon vzniká s dnešním
datem.

### Předvyplnění a upozornění

Výběr směru předvyplní kontakt z nadřízeného spisu, pokud je kontakt úkonu ještě prázdný; dál jde
volně změnit. Výběr odchozího směru předvyplní stav Rozpracováno, pokud je stav ještě prázdný;
API stav nikdy neodvozuje. Nese-li kontakt úkon i jeho spis a liší se, formulář i detail úkonu to hlásí
upozorněním, které jmenuje kontakt spisu; uložení to nebrání. Úkon bez kontaktu pod spisem
s kontaktem upozornění nevyvolá.

### Externí číslo jednací

Úkon nese nejvýše jedno číslo, které mu dal jiný úřad: nepovinný volný text bez vazby na
kontakt. Zadává se na editaci úkonu.

### Stránky a řazení

- `/acts` — seznam úkonů napříč spisy: datum úkonu, název se spisem, hlavní soubor, směr, stav,
  kontakt a okamžik poslední změny. Hledací pole hledá v názvu, popisu, čísle úkonu
  a externím čísle úkonu. Zužuje se hledáním, směrem, stavem (Vše, Bez stavu, Rozpracováno,
  Odesláno), rozsahem dat a štítkem; zúžení stavem nese jen tento seznam. Volba „Jen z otevřených spisů“ je
  ve výchozím stavu zapnutá a vynechá úkony uzavřených spisů. Výchozí řazení je podle data úkonu
  sestupně, 20 úkonů na stránku. Příchod na adresu seznamu nastaví všechny filtry podle adresy;
  co adresa nezmiňuje, má výchozí hodnotu.
- `/acts/new?case={id}` — založení, včetně štítků. Bez spisu v adrese formulář vyžaduje výběr spisu.
- `/acts/{actId}` — detail: štítky, údaje, komentáře (SDD-013), soubory (SDD-012). Záhlaví
  nese vedle směru stav; úkon bez stavu ho nenese. Nabídka vedle Upravit úkon nese u úkonu, který
  není odeslaný, Označit jako odesláno: nastaví stav Odesláno bez potvrzení a ostatní údaje
  nechá být.
  Nese pořadí úkonu ve spisu s přechodem na předchozí a další úkon a průběh spisu: nejvýše dva
  úkony před ním a dva po něm v pořadí výchozího řazení. Upozornění na odlišný kontakt spisu
  stojí v záhlaví kontaktu úkonu jako „Jiný než ve spisu“; kontakt spisu jmenuje po najetí myší.
- `/acts/{actId}/edit` — editace, včetně štítků.
- Založení i editace nesou stav jako přepínač Rozpracováno / Odesláno; klik na vybraný stav ho
  zruší.
- Seznam úkonů jde napříč spisy a zužuje se spisem, stavem spisu, kontaktem, štítkem, směrem,
  stavem a rozsahem dat; v detailu spisu je zúžený tímto spisem. Ukazuje datum, název, štítky (SDD-019), hlavní
  soubor, směr, stav, kontakt a okamžik poslední změny; podobu v detailu
  spisu řídí SDD-009.
- Každý seznam úkonů nese hned za názvem úzký sloupec Soubor s hlavním souborem úkonu (SDD-012):
  tlačítko v podobě barevné dlaždice typu souboru jako v kartě souborů, orámované barvou typu,
  které najetí myší ztmaví; jeho popisek a tooltip jmenují soubor; tooltip je bublina jako u porovnání verzí. Klik otevře náhled
  hlavního souboru se všemi jeho akcemi, bez listování ostatními soubory úkonu, a do detailu
  úkonu nevede. Změna v náhledu (přejmenování, nová verze, zrušení hlavního souboru, smazání) se
  v seznamu hned projeví. Úkon bez hlavního souboru má buňku prázdnou. Na mobilu stojí ikona
  vpravo vedle názvu. Výchozí řazení je
  podle data úkonu vzestupně; záhlaví sloupce řadí podle sebe a druhý klik otočí směr. Seznam je
  stránkovaný podle SDD-005 a nese celkový počet.

### Mazání

Mazání řídí matice v SDD-007; potvrzení jmenuje, co kaskáda bere.

## Rozhodnutí

- Kontakty úkonu: odesílatel a příjemce / jeden kontakt protistrany. Platí jeden.
- Směr a kontakt: nezávislé / jen spolu. Platí jen spolu.
- Stav a směr: jen u odchozích / nezávislé. Platí nezávislé; odchozí směr stav jen předvyplní.
- Kontakt odlišný od spisu: zákaz / upozornění. Platí upozornění.
- Řazení: datum + pořadové číslo / jen datum. Výchozí viditelné řazení je datum úkonu; shodná
  data řadí deterministicky `Created`.
- Externí číslo: jeden sloupec / N řádků s kontaktem. Platí jeden sloupec bez kontaktu.

## Dopady

—
