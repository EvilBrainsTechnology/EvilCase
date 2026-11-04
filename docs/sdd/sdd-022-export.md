# SDD-022 — Export

- **Stav:** platí
- **Milníky:** —
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [007](sdd-007-domenovy-model.md),
  [012](sdd-012-soubory.md), [019](sdd-019-stitky.md)

## Rozsah

Ruční export všech dat tenanta do jednoho archivu zip: spouštění, struktura archivu a formát
dokumentů JSON. Sekci nastavení, ze které se export spouští, drží SDD-019. Plánovaný export,
rotace, ukládání exportů, archiv s heslem a import do rozsahu nepatří.

## Popis

### Spuštění

Tlačítko Exportovat vše v sekci Export nastavení stáhne `GET /api/export` obyčejným stažením
prohlížečem, stejně jako Stáhnout vše (SDD-012). Exportovat smí každý přihlášený uživatel tenanta;
nepřihlášený dostane 401. Archiv nese jen data tenanta volajícího (SDD-006).

Archiv se jmenuje `EvilCase export 2026-10-30.zip` podle pražského data exportu. Posílá se
průběžně, jak se píše, bez přípravy předem a bez omezení velikosti, i nad 4 GB. Všechna data se
čtou z jednoho snímku databáze pořízeného na začátku exportu; změna během exportu v archivu není.
Přerušené stažení export zastaví. Chyba po prvním bajtu stažení přeruší; prohlížeč nikdy neuloží
useknutý archiv jako celý.

### Struktura

```
export.json
cases/
  2026-08-21 Přestupek/
    case.json
    files/
      Smlouva (version 1).pdf
      Smlouva.pdf
    cases/
      2026-09-01 Odvolání/
        case.json
    acts/
      2026-09-02 Podání/
        act.json
        files/
          Podání.pdf
contacts/
  Krajský úřad.json
labels/
  labels.json
```

- Jména složek a dokumentů jsou anglicky.
- `cases/` nese kořenové spisy. Složka spisu nese `case.json`, ve složce `files/` soubory spisu
  s verzemi, ve složce `cases/` jednu složku za každý podspis a ve složce `acts/` jednu složku za
  každý úkon. Podspisy se zanořují do libovolné hloubky. Složka úkonu nese `act.json` a ve složce
  `files/` soubory úkonu s verzemi. Složka `files/`, `cases/` nebo `acts/` je v archivu, jen když
  něco nese.
- Složka spisu i úkonu se jmenuje `<datum> <název>`, datum `yyyy-MM-dd`. Název ztratí znaky,
  které jméno souboru nesmí obsahovat, jako ve Stáhnout vše, a zkrátí se na 80 znaků. Název, ze
  kterého nic nezbude, nahradí `case` nebo `act`.
- Soubory leží jako ve Stáhnout vše včetně verzí: aktuální verze pod jménem souboru, starší vedle
  ní jako „Smlouva (version 2).pdf“.
- `contacts/<jméno>.json` je jeden kontakt; jméno se upraví jako název spisu, prázdné nahradí
  `contact`.
- `labels/labels.json` nese všechny štítky tenanta; tenant bez štítků ho nemá.
- Jméno, které už ve složce je (bez ohledu na velikost písmen), dostane „ (2)“, „ (3)“ a další
  volné číslo, u souboru před příponou. Dokument JSON nese skutečný název i cestu v archivu.
- Položky archivu nesou pražský čas, soubor okamžik nahrání své verze, dokument spisu, úkonu
  a kontaktu okamžik poslední změny záznamu, ostatní začátek exportu. Jména položek jsou v UTF-8.
- Verze, jejíž obsah na disku chybí, export nezastaví: v archivu není, její dokument ji označí
  `missing: true` a zaloguje se varování.
- Tenant bez dat vydá `export.json` a prázdné složky `cases`, `contacts` a `labels`.

### Dokumenty JSON

Formát je verzovaný; přidané pole verzi nezvýší, každá jiná změna struktury zvýší verzi
formátu i jejích schémat. Schéma JSON
každého typu dokumentu leží v [`docs/export/v1/`](../export/v1/): `export.schema.json`,
`case.schema.json`, `act.schema.json`, `contact.schema.json`, `labels.schema.json`
a společné definice `common.schema.json`.

- Klíče jsou anglicky a jmenují se jako v kontraktu API; datum je `yyyy-MM-dd`, okamžik ISO 8601
  v UTC; dokumenty nesou id.
- Pole bez hodnoty je přítomné jako `null`.
- Každý dokument nese `type`: `export`, `case`, `act`, `contact` nebo `labels`.
- `export.json`: verze formátu, okamžik exportu, verze aplikace, id a název tenanta.
- `case.json`: všechna pole spisu, id nadřízeného spisu, protistrana jako id a jméno, štítky
  jako id a název, komentáře, soubory, okamžik založení a poslední změny, cesta složky.
- `act.json`: všechna pole úkonu včetně stavu, id spisu, kontakt jako id a jméno, štítky jako id a název,
  komentáře, soubory, okamžik založení a poslední změny, cesta složky.
- Komentář: id, text, e-mail autora, okamžik založení a poslední změny.
- Soubor: id, jméno a u každé verze číslo, velikost, typ obsahu, SHA-256, okamžik nahrání, e-mail
  toho, kdo ji nahrál, cesta v archivu a příznak `missing`.
- Kontakt: všechna pole, okamžik založení a poslední změny, cesta v archivu.
- Štítky: u každého id, název a barva.

Export nenese historii změn, smazané záznamy, uživatele, refresh tokeny ani data jiných tenantů.

### Logování

Začátek a konec exportu se logují s id tenanta, počty spisů, úkonů, kontaktů, štítků a verzí
souborů, počtem chybějících obsahů a velikostí archivu; obsah nikdy.

## Rozhodnutí

- Spuštění: příprava na pozadí a odkaz ke stažení / průběžné stažení. Platí průběžné stažení.
- Oprávnění: jen správce / každý přihlášený uživatel tenanta. Platí každý přihlášený uživatel.
- Struktura: plochý seznam dokumentů / složky podle stromu spisů. Platí složky podle stromu.
- Formát: kontrakt API / vlastní dokumenty exportu. Platí vlastní dokumenty, aby se kontrakt API
  mohl měnit bez změny formátu.
- Chybějící obsah verze: přerušit export / označit a pokračovat. Platí označit a pokračovat.

## Dopady

Sekci Export v nastavení drží SDD-019, routu SDD-016. Jména souborů a verzí sdílí export se
Stáhnout vše (SDD-012).
