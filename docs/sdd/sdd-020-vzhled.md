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
akcent `--ec-accent` #1F3FAE. Významové dvojice pozadí a textu nese stav (zelená, žlutá, šedá),
směr úkonu (modrá příchozí, zelená odchozí) a chyba (červená). Každá dvojice drží kontrast
alespoň 4,5:1.

Písmo: `Instrument Sans` na text, `JetBrains Mono` na čísla, značky a přípony souborů. Obojí
se dodává s aplikací, ne z cizí sítě. Velikosti 11, 12, 13, 14, 16, 18, 22, 28 a 32 px;
nadpis detailu 32 px, seznamu a formuláře 28 px, na mobilu 22 px, nadpis karty 18 px,
v bočním sloupci 16 px, tělo 14 px, popisek 12 px, drobná značka 11 px.

Texty rozhraní i zapsané názvy a popisy drží českou typografii: nezlomitelná mezera po
jednopísmenné předložce a spojce, mezi číslem a jednotkou, mezi skupinami číslic, uvnitř data
a před pomlčkou.

Rozměry: rozestupy 4, 6, 8, 12, 16, 20, 24 a 32 px. Rádius karty 16 px, pole a tlačítka 10 px,
drobné prvky 6 až 9 px, pilulka 999 px. Výška tlačítka a pole 40 px, na formuláři 44 px,
drobné tlačítko 32 až 36 px. Cíl kliknutí nikdy pod 44 px na dotykovém zařízení.

### Primitiva

Aplikace má vlastní sadu komponent s prefixem `Ec`. Obrazovka skládá jen je, nikdy holé
`div` s vlastním stylem a nikdy třídy cizí knihovny:

- `EcPageHeader` — drobečky, název, stav, akce vpravo.
- `EcCard` — bílá plocha s linkou a rádiusem 16 px, volitelnou hlavičkou a patičkou.
- `EcButton` — varianty `primary`, `secondary`, `ghost`, `danger`; každé tlačítko nese ikonu
  vlevo od textu. `danger` je plné tmavě červené tlačítko s bílým textem a potvrzuje smazání. Tlačítko jen s ikonou má `aria-label`. Tichá textová akce u položky nebo
  v liště (Upravit a Smazat u komentáře, Zrušit filtry, Zobrazit i uzavřené) je výjimka: text
  bez ikony a bez plochy; mazání nese barvu chyby.
- `EcBadge` — pilulka pro stav spisu, směr úkonu a typ kontaktu; stav nese barevnou tečku, směr
  šipku.
- `EcTable` — seznam podle SDD-016: řádek je odkaz po celé šířce, řaditelné záhlaví je
  tlačítko, patička nese rozsah, počet a stránkování; tlačítka stránkování jen tam, kde je víc
  než jedna stránka. Chybějící hodnota v buňce je světlá
  pomlčka.
- `EcField` — popisek, ovládací prvek a pod ním nápověda, kterou při neplatné hodnotě nahradí
  chyba; povinné pole značí hvězdička. Nápověda a chyba z jedné fráze končí bez tečky, celá věta
  s tečkou.
- `EcModal` — nad nativním `dialog`, se správou fokusu; zavře ho Esc i klik mimo okno.
- `EcMenu` — tlačítko s nabídkou akcí; Esc i klik mimo ji zavřou, šipky chodí mezi položkami
  a fokus se vrací na tlačítko.
- `EcCombobox` — pole s hledáním bez ohledu na diakritiku a se založením záznamu přímo
  z nabídky; stojí na něm výběr kontaktu i štítku.
- `EcEmptyState` — ikona, věta, volitelná výzva k založení.
- `EcFileIcon` — dlaždice s ikonou typu souboru, bez textu přípony; ikonu i barvu určuje
  přípona (SDD-012).

### Ikony

Jediným zdrojem je `AppIcons`, kresba jednotná: tah 1,8 px, zakulacené konce, velikost 16 px
v tlačítku, 18 px v menu. Ikona vždy doprovází text; samostatná stojí jen tam, kde ji nahrazuje
`aria-label`. Emoji se nepoužívají.

### Skladba obrazovky

Nad obsahem je tmavá vodorovná lišta s logem, menu, hledáním napříč entitami ([SDD-021](sdd-021-vyhledavani.md), předloha
[`hledani.html`](../design/hledani.html)) a uživatelským menu; aktivní
položka menu je zvýrazněná po celé výšce lišty. Obsah leží na
šedém podkladu v bílých kartách; karta nikdy neleží v jiné kartě. Obsah stránky je nejvýše
1680 px široký a zbytek okna zůstane prázdný; roste přitom hlavní sloupec, boční zůstává 380 px.

Řádek seznamu má na desktopu ve všech seznamech stejnou výšku 48 px, ať nese jeden, nebo dva
řádky textu. Výjimkou jsou soubory, jejichž řádek nese dlaždici typu, a výsledky hledání.

Detail spisu i úkonu začíná kartou se jménem, stavem, popisem a řádkem údajů. Popis je hned
pod názvem, ne v postranním sloupci; bez popisu jde pod název rovnou řádek údajů. Obsah, který patří spisu, je v pravém sloupci; obsah,
který patří úkonu, je uvnitř úkonu. Soubory a komentáře proto existují dvakrát a název to
vždycky říká: „Soubory spisu" a „Soubory úkonu".

Další akce detailu jsou v nabídce `EcMenu` vedle hlavní akce; smazání je v ní poslední
a oddělené. Spisovou značku, číslo jednací i externí značky v řádku údajů jde zkopírovat
tlačítkem vedle hodnoty. Soubory a komentáře v bočním sloupci detailu spisu jsou kompaktní:
soubor drží název na jednom řádku a velikost s okamžikem nahrání na druhém a pole pro nový
komentář má dva řádky.

Karta souborů nese v záhlaví výzvu „Přetáhněte soubory sem nebo" a tlačítko Nahrát soubory,
v bočním sloupci „Přetáhněte sem nebo" a Nahrát; záhlaví drží jeden řádek a výzva se bez místa
zkrátí. Na mobilu záhlaví výzvu nenese a tlačítko je vždy Nahrát. Velikost a okamžik nahrání
se zalomí jen mezi sebou. Pod seznamem žádný pruh není. Řádek souboru je jedno ovládací místo po celé šířce a otevírá náhled; stažení a nabídka ⋯ jsou ohraničená
tlačítka jen s ikonou, v nabídce je Přejmenovat a oddělené, červené Smazat…. Při tažení
souborů přes kartu ji přikryje přerušovaný akcentový rámeček s textem „Pusťte soubory
a nahrají se k úkonu" (ve spisu „ke spisu") a limity. Náhled souboru je velký modal; hlavička
nese ikonu, název, velikost a okamžik nahrání, přechod mezi soubory karty s pořadím „2 z 5",
Přejmenovat, Stáhnout a zavření.

Načítání aplikace a přihlášení sdílejí jeden rám: tmavá stránka, uprostřed nahoře značka
a pod ní jedna bílá karta s postupem načítání, nebo s přihlašovacím formulářem. Značka zůstává
při přechodu na stejném místě, na desktopu i na mobilu.

### Stavy

Každý seznam a každá karta má stav načítání, chyby, prázdna a plného obsahu. Prázdný stav nese
ikonu, jednu větu a tam, kde jde záznam založit, i výzvu. Chyba nese větu, co se nepovedlo,
a nabídku opakování. Nenalezený záznam nebo stránka nese ikonu, větu a odkaz zpět na seznam.
Načítání drží výšku, aby obsah neposkakoval.

Ovládací prvek má viditelný stav nad myší, při fokusu a zakázaný; fokus je vidět vždy
a klávesnicí se dá projít celá obrazovka. Pole, které neprošlo kontrolou, má červený rámeček
a chybu pod sebou; po neúspěšném odeslání přejde fokus na první takové pole. Destruktivní akce
se potvrzuje a věta potvrzení jmenuje, co kaskáda bere.

### Mobil

Desktop je primární, každá denně používaná obrazovka je plnohodnotná i na mobilu. Menu se
schová do zásuvky, boční sloupec se zařadí pod hlavní, formulář má jeden sloupec a jeho
tlačítka zůstávají u spodního okraje okna. Pole má na mobilu písmo 16 px. Seznam se nikdy neposouvá vodorovně: na úzké šířce
se řádek přeskládá na dva až tři řádky a řazení přebírá select v liště.

### Bez cizí knihovny

Vzhled se nestaví přebarvením cizího CSS. Aplikace nenese knihovnu komponent ani cizí stylopis
a obrazovka nepoužije cizí třídu. Cizí kód přichází v úvahu jen na pozicování plovoucích prvků;
komponenta ho schová za vlastní rozhraní, aby ho obrazovky neviděly.

## Rozhodnutí

- Vzhled: přebarvit Tabler / vlastní tokeny a primitiva. Platí vlastní tokeny a primitiva.
- Postup: přepsat celou aplikaci najednou / migrovat po obrazovkách. Platí migrace po
  obrazovkách.
- Knihovna komponent: nahradit TabBlazor jinou knihovnou / napsat vlastní primitiva. Platí
  vlastní primitiva.
- Datum a modál: knihovna / nativní prvky prohlížeče. Platí nativní `input type="date"`
  a `dialog`.
- Šířka obsahu: bez omezení / strop. Platí strop 1680 px.
- Popis spisu: postranní sloupec / pod názvem. Platí pod názvem.
- Soubory a komentáře úkonu: samostatný panel / uvnitř úkonu. Platí uvnitř úkonu.
- Smazání na detailu: tlačítko vedle hlavní akce / poslední položka nabídky. Platí položka
  nabídky.

## Dopady

- SDD-016 nadále drží routy, menu a pravidla seznamů; jejich vzhled se řídí tímhle SDD.
- SDD-009, 010, 015 a 019 popisují obsah obrazovek, jejich vzhled se řídí tímto SDD.
