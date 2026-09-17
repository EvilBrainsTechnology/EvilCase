# SDD-020 — Vzhled

- **Stav:** platí
- **Milníky:** M9
- **Související SDD:** [009](sdd-009-spisy.md), [010](sdd-010-ukony.md),
  [015](sdd-015-dashboard.md), [016](sdd-016-navigace-a-vzhled.md), [019](sdd-019-stitky.md)

## Rozsah

Vizuální jazyk aplikace a jeho stavební kameny: tokeny, primitiva, stavy a pravidla, kterými
se řídí každá obrazovka. Routy, menu a obsah stránek patří SDD-016 a SDD jednotlivých agend;
tohle SDD říká, jak ty stránky vypadají a z čeho se skládají.

Vizuální předloha každé obrazovky žije v [`docs/design/`](../design/README.md) a je závazná na
hodnoty. Co předloha neukazuje, řídí tenhle dokument.

## Popis

### Tokeny

Jediný zdroj barev, písem a rozměrů je `src/App/EvilCase.App/wwwroot/css/ec-tokens.css`.
Komponenta nikdy nepíše hodnotu přímo; sahá na proměnnou.

Barvy: podklad stránky `--ec-bg` #F2F3F6, plocha karty `--ec-surface` #FFFFFF, text
`--ec-text` #14161B, sekundární text `--ec-text-muted` #5B6270, linka `--ec-border` #E1E4EA,
akcent `--ec-accent` #3B5BDB. Významové dvojice pozadí a textu nese stav (zelená, žlutá, šedá),
směr úkonu (modrá příchozí, zelená odchozí) a chyba (červená). Každá dvojice drží kontrast
alespoň 4,5:1.

Písmo: `Instrument Sans` na text, `JetBrains Mono` na čísla, značky a přípony souborů. Obojí
se dodává s aplikací, ne z cizí sítě. Velikosti 12, 13, 14, 16, 18, 28 a 32 px; nadpis stránky
32 px, nadpis karty 18 px, tělo 14 px, popisek 12 px.

Rozměry: rozestupy 4, 6, 8, 12, 16, 20, 24 a 32 px. Rádius karty 16 px, pole a tlačítka 10 px,
drobné prvky 6 až 9 px, pilulka 999 px. Výška tlačítka a pole 40 px, na formuláři 44 px,
drobné tlačítko 32 až 36 px. Cíl kliknutí nikdy pod 44 px na dotykovém zařízení.

### Primitiva

Aplikace má vlastní sadu komponent s prefixem `Ec`. Obrazovka skládá jen je, nikdy holé
`div` s vlastním stylem a nikdy třídy cizí knihovny:

- `EcPageHeader` — drobečky, název, stav, akce vpravo.
- `EcCard` — bílá plocha s linkou a rádiusem 16 px, volitelnou hlavičkou a patičkou.
- `EcButton` — varianty `primary`, `secondary`, `ghost`, `danger`; každé tlačítko nese ikonu
  vlevo od textu. Tlačítko jen s ikonou má `aria-label`.
- `EcBadge` — pilulka pro stav spisu, směr úkonu a štítek.
- `EcTable` — seznam podle SDD-016: řádek je odkaz po celé šířce, řaditelné záhlaví je
  tlačítko, patička nese rozsah, počet a stránkování.
- `EcField` — popisek, ovládací prvek, nápověda a chyba pod sebou; povinné pole značí hvězdička.
- `EcModal` — nad nativním `dialog`, se správou fokusu a zavíráním klávesou Esc.
- `EcCombobox` — pole s hledáním bez ohledu na diakritiku a se založením záznamu přímo
  z nabídky; stojí na něm výběr kontaktu i štítku.
- `EcEmptyState` — ikona, věta, volitelná výzva k založení.
- `EcFileChip` — přípona souboru jako ikona dokumentu se zkratkou; barvu určuje typ.

### Ikony

Jediným zdrojem je `AppIcons`, kresba jednotná: tah 1,8 px, zakulacené konce, velikost 16 px
v tlačítku, 18 px v menu. Ikona vždy doprovází text; samostatná stojí jen tam, kde ji nahrazuje
`aria-label`. Emoji se nepoužívají.

### Skladba obrazovky

Nad obsahem je vodorovná lišta s logem, menu, hledáním a uživatelským menu. Obsah leží na
šedém podkladu v bílých kartách. Obsah stránky je nejvýše 1680 px široký a zbytek okna zůstane
prázdný; roste přitom hlavní sloupec, boční zůstává 380 px.

Detail spisu i úkonu začíná kartou se jménem, stavem, popisem a řádkem údajů. Popis je hned
pod názvem, ne v postranním sloupci. Obsah, který patří spisu, je v pravém sloupci; obsah,
který patří úkonu, je uvnitř úkonu. Soubory a komentáře proto existují dvakrát a název to
vždycky říká: „Soubory spisu" a „Soubory úkonu".

### Stavy

Každý seznam a každá karta má stav načítání, chyby, prázdna a plného obsahu. Prázdný stav nese
ikonu, jednu větu a tam, kde jde záznam založit, i výzvu. Chyba nese větu, co se nepovedlo,
a nabídku opakování. Načítání drží výšku, aby obsah neposkakoval.

Ovládací prvek má viditelný stav nad myší, při fokusu a zakázaný; fokus je vidět vždy
a klávesnicí se dá projít celá obrazovka. Destruktivní akce se potvrzuje a věta potvrzení
jmenuje, co kaskáda bere.

### Mobil

Desktop je primární, každá denně používaná obrazovka je plnohodnotná i na mobilu. Menu se
schová do zásuvky, boční sloupec se zařadí pod hlavní, formulář má jeden sloupec. Seznam se
nikdy neposouvá vodorovně: na úzké šířce se řádek přeskládá na dva až tři řádky a řazení
přebírá select v liště.

### Odchod od TabBlazoru

Nový vzhled se nestaví přebarvením Tableru. Tabler i TabBlazor v aplikaci zůstávají, dokud
drží nemigrované obrazovky, a mizí posledním krokem milníku: balíček z `csproj`, oba
stylopisy z `index.html`, `TablerColor` z modelů v `App/Models`.

Během přechodu žijí dva systémy vedle sebe. Vlastní styly jsou zapouzdřené pod třídou `ec` na
kořeni, aby se resety nepraly, a nová komponenta nepoužije ani jednu bootstrapovou třídu.
Cizí kód přichází v úvahu jen na pozicování plovoucích prvků; komponenta ho schová za vlastní
rozhraní, aby ho obrazovky neviděly.

## Rozhodnutí

- Vzhled: přebarvit Tabler / vlastní tokeny a primitiva. Platí vlastní tokeny a primitiva.
- Postup: přepsat celou aplikaci najednou / migrovat po obrazovkách. Platí migrace po
  obrazovkách; Tabler mizí až s poslední z nich.
- Knihovna komponent: nahradit TabBlazor jinou knihovnou / napsat vlastní primitiva. Platí
  vlastní primitiva.
- Datum a modál: knihovna / nativní prvky prohlížeče. Platí nativní `input type="date"`
  a `dialog`.
- Šířka obsahu: bez omezení / strop. Platí strop 1680 px.
- Popis spisu: postranní sloupec / pod názvem. Platí pod názvem.
- Soubory a komentáře úkonu: samostatný panel / uvnitř úkonu. Platí uvnitř úkonu.

## Dopady

- SDD-016 nadále drží routy, menu a pravidla seznamů; větu o Tableru a TabBlazoru nahrazuje
  odkaz sem.
- SDD-009, 010, 015 a 019 popisují obsah obrazovek, jejich vzhled se řídí tímto SDD.
