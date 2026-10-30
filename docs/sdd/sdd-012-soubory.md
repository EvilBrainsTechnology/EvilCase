# SDD-012 — Soubory

- **Stav:** platí
- **Milníky:** M2, M5
- **Související SDD:** [001](sdd-001-architektura.md),
  [002](sdd-002-logovani-a-observabilita.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Ukládání souborů a jejich verzí, vlastnictví, upload, download a mazání.

## Popis

### Vlastnictví

FileAsset patří právě jednomu spisu XOR úkonu. Žádné vazby mezi soubory a jinými entitami;
tentýž dokument ve dvou spisech jsou dva soubory. Jméno souboru je u jeho spisu nebo úkonu
jedinečné bez ohledu na velikost písmen.

### Úložiště

- Bajty leží na souborovém systému pod kořenem z konfigurace
  (`EvilBrains__EvilCase__Files__RootPath`, povinný); databáze nese jen metadata: název
  souboru a u každé verze velikost, `MediaType`, SHA-256 hash a cestu k blobu. Blob se jmenuje
  podle id verze, bez přípony.
- Blob se nikdy fyzicky nemaže. Neúspěšný upload, upload beze změny i smazaný soubor nebo verze
  (SDD-007) zanechají osiřelý blob; osiřelé blob nejde ani vypsat, ani uklidit — úložiště jen
  roste.

### Pravidla

- SHA-256 je uložený kontrolní součet, ne deduplikace; porovnává se jen s aktuální verzí téhož
  souboru.
- Limit velikosti souboru je 100 MB; větší upload vrací 413 a prohlížeč ho odmítne dřív, než
  ho odešle.
- Název souboru je povinný, nejvýše 256 znaků; ukládá se jen jeho poslední část, cesta se
  zahazuje. `MediaType` se bere z uploadu, nejvýše 128 znaků; příponě se nevěří. Delší hodnota
  nebo chybějící název je 400.
- Soubor jde přejmenovat. Platí pro nové jméno stejná pravidla jako při uploadu; jméno, které
  bez ohledu na velikost písmen nese jiný soubor téhož spisu nebo úkonu, je 409, změna jen
  velikosti písmen vlastního jména projde. Verze, jejich bajty, hash a `MediaType` zůstávají.
  Soubor cizího vlastníka je 404.
- Verze bez blobu je 404.

### Verze

- Soubor má verze od 1, aktuální je verze s nejvyšším číslem. Obsah, velikost a `MediaType` nese
  verze, jméno soubor.
- Upload jména, které spis nebo úkon už má (bez ohledu na velikost písmen), přidá souboru verzi
  a soubor si nechá své jméno. Shoda platí jen u téhož spisu nebo úkonu, ne u podřízených spisů
  ani u úkonů spisu; v dávce se druhý soubor téhož jména stane další verzí prvního.
- Upload nové verze konkrétního souboru jeho jméno nepoužije. Jiná přípona (bez ohledu na
  velikost písmen) je 400, limity platí jako u uploadu.
- Obsah shodný s aktuální verzí (stejný SHA-256) verzi nevytvoří a upload odpoví 200 s aktuální
  verzí; nový soubor nebo nová verze je 201.
- Nová verze má číslo nejvyšší existující verze + 1, čísla se nepřečíslovávají.
- Verzi jde smazat; po smazání aktuální verze je aktuální nejvyšší zbývající. Jediná zbývající
  verze se samostatně smazat nedá (409). Smazání souboru smaže všechny jeho verze.
- Obsah, podpis datové zprávy a podepisující se čtou z aktuální verze, nebo z verze, kterou
  požadavek jmenuje číslem; verze, která neexistuje nebo patří cizímu souboru, je 404.
- Výpis verzí jde od nejnovější: číslo, velikost, okamžik nahrání a e-mail toho, kdo verzi
  nahrál.
- Novou verzi nahrát, verzi smazat i soubor přejmenovat a smazat smí kdokoli z tenantu.

### UI

Karta souborů leží na detailu spisu i úkonu podle toho, zda nese soubor. Bez souborů je
kompaktní v pravém sloupci: ve spisu pod Podřízenými spisy nad komentáři, v úkonu pod kartou
Spis nad Průběhem spisu. S aspoň jedním souborem je široká v hlavním sloupci: ve spisu nad
úkony, v úkonu nad komentáři. Karta se přesune hned po prvním nahraném souboru a po smazání
posledního, bez obnovení stránky; během nahrávání zůstane na místě a přesune se, až nahrávání
skončí. Přesunutá karta ukazuje dál zvýraznění, hlášky a chyby. Dokud se seznam nenačte, karta
není v žádném sloupci; seznam, který se načíst nepodaří, ukáže karta v pravém sloupci
s nabídkou opakování. Na mobilu jde hlavní sloupec před bočním.

Drag-and-drop multi-upload na detailu spisu i úkonu; soubory lze pustit kamkoli na kartu
souborů a karta při tažení ukáže, kam se nahrají, a limity. Hromadné přetažení běží po
souborech a odmítne jen soubor, který selže. Dávka nad 100 souborů se odmítá celá. Soubory
přetažené nebo vybrané během nahrávání se odmítnou s hláškou a nenahrají se; rozběhlá dávka
doběhne. Seznam souborů ukazuje každý soubor jednou: ikonu typu, název a velikost
a okamžik nahrání aktuální verze, v pořadí prvního nahrání od nejstaršího. Obsah souboru
se vydává z jedné adresy a odpověď nese `X-Content-Type-Options: nosniff`. Rastrové
obrázky, zvuk a video nesou `Content-Disposition: inline` a typ podle přípony; každý jiný typ,
PDF, SVG a HTML také, nese `attachment` a typ `application/octet-stream`; uložený `MediaType` se
v odpovědi nikdy nevrací. Obsah souboru nikdy nespustí skript a vložit do stránky ho smí jen
aplikace sama. Smazání je prosté, s potvrzením.

Široká karta nese v záhlaví vedle Nahrát soubory dělené tlačítko: Stáhnout vše stáhne aktuální
verze všech souborů karty jako jeden archiv zip, šipka vedle něj otevře nabídku se Stáhnout vše
včetně verzí. Kompaktní karta ho nenese. Na mobilu nesou Nahrát soubory i Stáhnout vše jen
ikonu. Stažení je obyčejné stažení prohlížečem.

- Archiv nese jen soubory spisu nebo úkonu samého, ne podřízených spisů ani úkonů spisu. Spis
  nebo úkon bez souborů vydá prázdný archiv; neznámý nebo cizí je 404.
- Archiv se jmenuje podle názvu spisu nebo úkonu, „Přestupek.zip“, s verzemi „Přestupek (včetně
  verzí).zip“. Název ztratí znaky, které jméno souboru nesmí obsahovat, jako příloha datové
  zprávy; prázdný nahradí „Soubory spisu“ nebo „Soubory úkonu“.
- Archiv je plochý: aktuální verze souboru leží pod jeho jménem, starší verze vedle něj jako
  „Smlouva (verze 2).pdf“, jméno bez přípony dostane číslo verze na konec. Jméno položky nenese
  cestu ani dvě tečky za sebou. Položky jdou v pořadí seznamu, verze souboru podle čísla; každá
  nese okamžik nahrání své verze v pražském čase platném v tom okamžiku.
- Jméno, které už v archivu je (bez ohledu na velikost písmen), dostane před příponou „ (2)“,
  „ (3)“ a další volné číslo. Jména aktuálních verzí mají přednost před jmény starších verzí.
- Archiv se posílá průběžně, bez sestavení předem, i nad 4 GB. Soubor, který se nepodaří
  přečíst, stažení přeruší; prohlížeč nikdy neuloží useknutý archiv jako celý.

Klik na řádek souboru otevře náhled aktuální verze; u typu bez náhledu ten řekne, že není
k dispozici, a nabídne stažení. Stažení je obyčejné stažení prohlížečem a uloží soubor vždy,
i starší verzi pod jménem souboru. Vedle toho nese řádek stažení aktuální verze a nabídku
Upravit, Nahrát novou verzi, Verze (3), Přejmenovat a Smazat. Upravit nese jen soubor, který
náhled podle přípony upravuje (Úpravy textu) a který má nejvýše 1 MB; otevře náhled rovnou
v úpravách, Markdown formátovaně. Soubor, který po načtení upravit nejde, zůstane v náhledu ke
čtení s jeho poznámkou. Potvrzení smazání souboru s více verzemi řekne, kolik verzí se smaže.

U souboru s více verzemi nese řádek štítek „verze 3“ s číslem aktuální verze, kompaktní karta
ho uvádí textem před velikostí; soubor s jedinou verzí štítek nemá. Soubory a verze nahrané od
načtení stránky jsou zvýrazněné se štítkem „nový“ nebo „nová verze“ do obnovení stránky. Po
nahrání karta vypíše soubory, které se nahrály jako nová verze („Smlouva.pdf: nahráno jako
verze 3.“), a soubory, jejichž obsah je stejný jako poslední verze („Smlouva.pdf: obsah je
stejný jako poslední verze, nová verze nevznikla.“).

Nahrát novou verzi otevře výběr jednoho souboru, jehož jméno se nepoužije. Soubor s jinou
příponou karta odmítne hláškou „Návrh.docx: nová verze souboru Smlouva.pdf musí mít příponu
.pdf.“ a nic neodešle.

Verze (3) otevře okno Verze souboru se jménem souboru, počtem verzí a Nahrát novou verzi.
Verze jdou od nejnovější; každá ukáže číslo, u aktuální štítek „aktuální“, okamžik nahrání,
e-mail toho, kdo ji nahrál, a velikost a nabídne Stáhnout a Smazat, všechny kromě nejstarší
i Porovnat s předchozí (Porovnání verzí). Klik na verzi otevře její
náhled. Smazání verze potvrzuje okno, které jmenuje číslo verze a soubor. Jediná verze Smazat
nenabízí; okno nese Smazat soubor, které otevře potvrzení smazání souboru. Na mobilu je okno
vysunuté od spodního okraje.

Přejmenování mění jen jméno bez přípony, přípona zůstává. Potvrzuje se Enterem nebo Uložit,
Esc a Zrušit ho zahodí. Jméno jiného souboru odmítne hláškou „Soubor tohoto jména už existuje“.

### Náhled

- Rastrové obrázky (jpg, jpeg, png, gif, webp, avif, bmp), zvuk a video zobrazí
  prohlížeč sám z adresy obsahu; ostatní typy zobrazí aplikace. Typ se pozná podle přípony,
  uložený `MediaType` se nepoužije. Příloha datové zprávy se ukáže ze zprávy samé.
- Zobrazí se PDF, obrázky (jpg, jpeg, png, gif, webp, avif, bmp, svg, heic, heif),
  text (txt, csv, tsv, log), kód (json, xml, isdoc, yaml, yml, html), Markdown (md, markdown), dokument Word
  (docx), sešit (xlsx, xls, ods), e-mail (eml, msg), datovou zprávu a doručenku ISDS (zfo), podepsaný soubor (p7m, p7s), zvuk (mp3, m4a, wav, ogg, oga) a video
  (mp4, webm, m4v, mov). Jiný typ nabídne jen stažení. Soubor, který se nepodaří přečíst, nabídne
  také jen stažení.
- PDF, text, kód, Markdown, e-mail, datová zpráva a dokument Word mají nad obsahem stejnou lištu se zvětšením
  a zmenšením (50 až 400 %, také Ctrl nebo Cmd s kolečkem myši), na počítači i se skutečnou
  velikostí 100 %, a s hledáním. Hledání stojí na pravém konci lišty a zleva nese pořadí výskytu
  z počtu (3 z 12), Hledám… nebo Nenalezeno, pole hledání a předchozí a další výskyt; na mobilu
  ho otevře ikona na vlastním řádku. Hledání nerozlišuje velikost písmen ani diakritiku, zvýrazní
  všechny výskyty, aktuální odliší a posune do zobrazení; Enter přejde na další výskyt,
  Shift+Enter na předchozí, Esc hledání vymaže a náhled nezavře.
- Poznámky náhledu i editoru textu, kódu a Markdownu (soubor je delší, není platný ve svém jazyce,
  nejde naformátovat, není v kódování UTF-8, upravuje se originál, samostatný podpis) stojí na
  liště za přepínačem, tlačítkem Formátovat nebo tlačítky formátu, před hledáním. Text, který se
  nevejde, končí výpustkou a celé znění ukáže tooltip.
- Text, kód a Markdown, formátovaně i ve zdrojovém kódu, v náhledu i v úpravách, se pod lištou
  posouvají celé a posuvník stojí u pravého okraje náhledu.
- PDF se ukáže jako stránky pod sebou, zpočátku na šířku náhledu, nejvýš však ve 100 %, i
  v mobilním prohlížeči. Lišta navíc ukáže stránku z počtu stran s polem pro skok na stránku
  a přizpůsobení na šířku a na celou stránku. Text jde vybrat a zkopírovat.
- U textu, kódu, Markdownu, e-mailu a datové zprávy zvětšení mění velikost písma a text se zalomí do šířky náhledu;
  zpočátku je 100 %.
- Odkaz v PDF na místo v dokumentu přejde na jeho stránku, odkaz na webovou nebo e-mailovou adresu
  se otevře v nové záložce, jiný odkaz nefunguje. Formuláře, skripty a vložená média nefungují.
  PDF chráněné heslem nebo nečitelné nabídne jen stažení.
- Text je vždy prostý text s čísly řádků, nikdy vykreslená stránka; ukáže se nejvýše první
  1 MB a náhled řekne, že je soubor delší. SVG je jen obrázek.
- Kód se ukáže jako text s čísly řádků a barevně zvýrazněnou syntaxí, nikdy jako vykreslená
  stránka; HTML se nevykoná. Zpočátku je kód naformátovaný; tlačítko Formátovat na liště přepíná
  mezi naformátovaným a původním textem a každý nově zobrazený soubor začne naformátovaný. Soubor
  delší než 1 MB ukáže jen svůj začátek v původní podobě, soubor neplatný ve svém jazyce (ISDOC
  jako XML) celý v původní podobě; tlačítko je pak vypnuté a náhled řekne proč. Totéž platí, když
  se soubor nepodaří naformátovat z jiného důvodu. Text s řádkem delším než 10 000 znaků se ukáže
  celý bez zvýraznění, v náhledu i v editoru.
- Text, kód a zdrojový kód Markdownu ukáže náhled stejně jako editor, jen ho nejde měnit a nemá
  kurzor: s čísly řádků a se zalomenými dlouhými řádky. Text jde vybrat a zkopírovat, Ctrl+A
  nebo Cmd+A vybere celý text. Ctrl+F nebo Cmd+F v textu přejde do hledání na liště. Hledání
  počítá výskyty v celém textu.
- Markdown se ukáže formátovaně, nejvýše první 1 MB, včetně tabulek, přeškrtnutí a seznamů
  úkolů; v náhledu nemá kurzor a zaškrtávací políčka úkolů nejdou měnit. Syrové HTML v něm se
  nevykoná, ukáže se jako text. Odkaz zůstane jen na webovou (http, https) nebo e-mailovou
  adresu a otevře se v nové záložce; jiný odkaz se ukáže jako prostý text. Obrázky se
  nenačítají: místo obrázku se ukáže jeho popis, bez popisu slovo „obrázek“.
- Lišta náhledu Markdownu nese přepínač Formátovaně / Zdrojový kód, na mobilu Formát / Zdroj.
  Zdrojový kód ukáže text s čísly řádků a barevně zvýrazněnými značkami Markdownu, se stejnou
  lištou jako formátované zobrazení. Zvolené zobrazení platí pro každý další soubor Markdown,
  dokud se náhled nezavře; nově otevřený náhled začne formátovaně.
- Dokument Word se ukáže jako stránky pod sebou, zpočátku na šířku náhledu, nejvýš však ve 100 %.
  Stránka končí tam, kde ji Word ukončil při posledním uložení, nebo na ručním zlomu stránky či
  oddílu. Dokument bez údajů od Wordu a stránka, která je i tak o víc než desetinu vyšší než
  papír, se rozdělí podle výšky papíru mezi odstavci a řádky tabulek; takové stránky se mohou od
  Wordu o několik řádků lišit. Prázdná stránka se neukáže. Lišta navíc ukáže
  stránku z počtu stran s polem pro skok na stránku a přizpůsobení na šířku a na celou stránku
  podle velikosti papíru.
- Sešit se ukáže jako tabulka s kartami listů; z každého listu se ukáže nejvýše prvních
  1 000 řádků a náhled řekne, že je list delší.
- E-mail ukáže odesílatele, příjemce, předmět, datum, text zprávy a seznam příloh s velikostí;
  zpráva nenačítá obrázky a nespouští skripty, HTML tělo se ukáže jako text.
- Datová zpráva ukáže předmět se štítkem Datová zpráva, odesílatele a adresáta se jménem, ID
  schránky a adresou, vyplněná čísla jednací a spisové značky odesílatele i adresáta, ID zprávy,
  okamžik dodání a doručení a seznam příloh s ikonou, jménem, velikostí a stažením. Doručenka
  ukáže štítek Doručenka, tutéž obálku a místo příloh události s okamžikem a popisem bez kódu
  události. Prázdný údaj obálky ukáže pomlčku, prázdné číslo jednací a spisová značka se
  vynechají.
- Soubor zfo, který není datovou zprávou ani doručenkou ISDS (třeba formulář Software602), nebo
  je větší než 30 MB, nabídne jen stažení.
- Klik na přílohu ukáže v tomtéž náhledu její náhled podle přípony, stejně jako u souboru karty,
  vnořenou datovou zprávu nevyjímaje. Hlavička pak nese návrat na zprávu, ikonu, jméno a velikost
  přílohy s textem „příloha zprávy" a jménem zprávy, Stáhnout přílohu, u víc příloh přechod mezi
  nimi s pořadím „příloha 2 z 5" a zavření; příloha se nepřejmenovává. Příloha bez náhledu
  nabídne jen své stažení.
- Příloha se stáhne pod svým jménem bez
  cesty a bez znaků, které jméno souboru nesmí obsahovat; prázdné jméno nahradí „příloha".
- HEIC a HEIF se ukážou jako obrázek.
- Náhled listuje mezi soubory karty, ze které se otevřel, vždy v jejich aktuální verzi; hlavička
  ukáže pořadí „Soubor 2 z 5“. Listování souborů i příloh stojí v hlavičce vpravo za Stáhnout,
  hned před zavřením; na mobilu na vlastním řádku pod tlačítky, u pravého okraje.
- Hlavička souboru karty nese Nová verze, která nahraje novou verzi jako akce řádku a pak ukáže
  aktuální verzi, a u souboru s více verzemi tlačítko Verze s počtem verzí, které ukáže a schová
  pruh verzí pod hlavičkou. Náhled hlášku o nahrané verzi neukazuje, vypíše ji karta.
- Pruh verzí nese výběr verze („Verze 2 z 3 · okamžik · e-mail“, za „z“ stojí číslo aktuální
  verze) se seznamem verzí od nejnovější a se Spravovat verze, které otevře okno Verze souboru.
- Starší verze se ukáže vždy s pruhem verzí v barvě upozornění, s textem „Starší verze. Stáhnout
  stáhne tuto verzi.“ a se Zobrazit aktuální verzi. Hlavička ukáže velikost a okamžik nahrání té
  verze; obsah, podpis datové zprávy, podepisující i stažení jsou té verze. Pruh samostatného
  podpisu u starší verze není. Hlavička nese vedle názvu štítek „verze 2“ a tlačítko Verze je
  neaktivní; k aktuální verzi vede pruh verzí.

### Úpravy textu

- Text (txt, csv, tsv, log), kód (json, xml, isdoc, yaml, yml, html) a Markdown (md, markdown) jde
  upravit přímo v náhledu. Hlavička náhledu nese Upravit před Verze jen u aktuální verze souboru
  karty, který se celý vejde do 1 MB a je platné UTF-8, s BOM i bez něj. Starší verze, obsah podepsaného dokumentu a příloha
  datové zprávy se neupravují. U souboru, který není platné UTF-8, náhled řekne „Soubor není
  v kódování UTF-8, proto ho nejde upravit.“
- Editor ukáže celý text s čísly řádků, dlouhé řádky zalomí a kód barevně zvýrazní stejně jako
  náhled. Text sám neformátuje ani nedoplňuje. Upravit vymění náhled za editor na témže místě,
  stejně velký a ve 100 %. Kód se upravuje vždy v původní podobě; když ho náhled ukazoval naformátovaný, editor
  řekne „Upravuje se originál souboru, ne jeho naformátovaná podoba.“ Hlavička nese jen ikonu
  a jméno souboru, „Upravujete verzi 3“, po změně „Neuložené změny · upravujete verzi 3“, Zrušit, Uložit
  a zavření; listování soubory, Verze, Nová verze, Přejmenovat a Stáhnout se během úprav skryjí.
- Lišta nad textem nese Zpět a Znovu, neaktivní, když není co vrátit, a hledání Hledat v textu
  uspořádané jako v náhledu: pořadí výskytu z počtu, pole a předchozí a další výskyt. Zpět
  a znovu jde i Ctrl+Z a Ctrl+Y, Cmd+Z a Cmd+Shift+Z. Hledání nerozlišuje velikost písmen,
  zvýrazní všechny výskyty a vybere další; pořadí je pořadí výskytu u kurzoru. Enter přejde na
  další výskyt, Shift+Enter na předchozí, Esc hledání vymaže, vrátí kurzor do textu a náhled
  nezavře. Ctrl+F nebo Cmd+F v textu přejde do hledání.
- Tab vloží do textu tabulátor, do kódu odsazení jeho prvního odsazeného řádku, nejvýše osm
  mezer; když žádný odsazený není, v YAML dvě mezery, jinak tabulátor. Výběr přes více řádků Tab
  odsadí a Shift+Tab odsazení ubere. První Esc v textu náhled nezavře a následující Tab přesune
  fokus z textu; další Esc se chová jako Esc kdekoli v náhledu.
- Markdown se upravuje v zobrazení, které náhled právě ukazuje: formátovaně přímo v náhledu,
  nebo ve zdrojovém kódu stejným editorem jako text, se zvýrazněnými značkami Markdownu.
  Přepínač Formátovaně / Zdrojový kód zůstává na liště i během úprav. Přepnutí přenese text
  i s neuloženými změnami do druhého zobrazení a historii Zpět a Znovu začne znovu.
- Lišta formátovaných úprav nese Zpět, Znovu, přepínač, Nadpis 1, 2 a 3, Tučně, Kurzíva,
  Přeškrtnout, Odrážky, Číslovaný seznam, Seznam úkolů, Citace, Kód, Odkaz, Tabulka a na pravém
  konci hledání Hledat v textu uspořádané jako v náhledu: pořadí výskytu z počtu, pole
  a předchozí a další výskyt. Tlačítko formátu, který platí v místě kurzoru, je zvýrazněné
  a druhý stisk formát zruší: nadpis vrátí na odstavec, seznam a citaci zruší. Jiný seznam
  převede seznam na stisknutý druh. Tlačítko, které v místě kurzoru nejde použít, je neaktivní.
  Kód udělá z označeného textu v jednom odstavci kód v řádku, jinak z odstavce blok kódu; v bloku
  kódu ho vrátí na odstavec. Tabulka vloží tabulku 3 × 3 se záhlavím; v tabulce a v bloku kódu je
  neaktivní. Zaškrtávací políčko úkolu jde během úprav přepnout.
- Odkaz otevře okno „Odkaz“ s polem Adresa a nápovědou „Začíná https://, http:// nebo
  mailto:“. Jinou adresu okno odmítne textem „Zadejte adresu, která začíná https://, http://
  nebo mailto:.“ Vložit odkaz propojí označený text, odkaz v místě kurzoru, nebo bez výběru vloží
  adresu samu jako odkaz; u existujícího odkazu okno nabídne i Odebrat odkaz. Enter odkaz vloží,
  Esc a Zrušit okno zavřou.
- Hledání ve formátovaných úpravách nerozlišuje velikost písmen ani diakritiku, zvýrazní všechny
  výskyty a vybere další; pořadí je pořadí výskytu u kurzoru a výskyty sledují úpravy textu.
  Enter přejde na další výskyt, Shift+Enter na předchozí, Esc hledání vymaže, vrátí kurzor do
  textu a náhled nezavře. Ctrl+F nebo Cmd+F v textu přejde do hledání. Formátované úpravy nemají
  zvětšení; zvětšení náhledu se při Upravit vrátí na 100 %. Ctrl+B a Cmd+B je tučně, Ctrl+I
  a Cmd+I kurzíva, zpět a znovu jako v editoru textu, Ctrl+S nebo Cmd+S uloží. Psaní značek
  Markdownu (# na začátku řádku, -, 1., >, ``` a dvojice ** nebo `) formát rovnou použije. Tab
  v seznamu odsadí položku a Shift+Tab ji vrátí, jinde Tab přesune fokus.
- Formátovaně uložený Markdown se zapíše jednotně: značky, odrážky a odsazení mohou v souboru
  vypadat jinak než předtím, i mimo změněné místo. Uložit je aktivní, jen když uživatel obsah
  změnil; vrácení všech změn přes Zpět ho znovu vypne a úpravy otevřené a zavřené beze změny
  soubor nezmění. Syrové HTML a obrázky v souboru zůstanou. Vložený obsah z jiné aplikace si
  ponechá nadpisy, odstavce, seznamy, citace, tabulky, kód, formát písma a odkazy na webové
  a e-mailové adresy; obrázky a ostatní HTML vynechá.
- Uložit, i Ctrl+S nebo Cmd+S, nahraje text jako novou verzi souboru pod jeho jménem stejně jako
  Nová verze a náhled se vrátí ke čtení aktuální verze, bez hlášky o nahrané verzi. Dokud se text neliší od načteného, je
  Uložit neaktivní. Uložení zachová UTF-8, BOM, pokud ho soubor měl, a konce řádků souboru; konce
  řádků určí první konec řádku v souboru (CRLF, LF nebo CR), soubor bez konce řádku dostane LF.
- Uložení se nekontroluje proti souběžné změně: nová verze vznikne nad tou, která je v tu chvíli
  aktuální. Text shodný s aktuální verzí verzi nevytvoří a náhled se vrátí ke čtení. Selhané
  uložení nechá text v editoru a ukáže důvod se Zkusit znovu.
- Soubor se samostatným podpisem ukáže na liště v barvě upozornění „Soubor má samostatný podpis
  Smlouva.xml.p7s. Nová verze ho zneplatní.“
- Zrušit bez změn vrátí náhled ke čtení hned. Neuložené změny hlídá okno „Zahodit neuložené
  změny?“ s textem „Úpravy souboru Smlouva.xml nejsou uložené. Po zahození se nedají vrátit.“,
  Pokračovat v úpravách a Zahodit změny. Okno se ukáže při Zrušit, při zavření náhledu křížkem,
  Esc i klikem mimo a při přechodu v aplikaci, i tlačítkem Zpět prohlížeče. Po zahození změn
  udělá Zpět nebo Vpřed prohlížeče krok v historii a nový záznam do ní nepřidá. Zavření nebo
  obnovení záložky a přechod mimo aplikaci ohlásí vlastní dotaz prohlížeče.
- Na mobilu nese hlavička pod jménem Zrušit a Uložit vedle sebe přes celou šířku a po změně
  ukáže jen „Neuložené změny“; lišta se při nedostatku místa posouvá do strany. Lišta
  formátovaných úprav je i na mobilu v jednom řádku a posouvá se do strany; přepínač nese
  Formát / Zdroj.

### Porovnání verzí

- Verze textu, kódu a Markdownu jde porovnat v náhledu. Hlavička náhledu souboru karty s více
  verzemi nese za Verze tlačítko Porovnat, které porovná zobrazenou verzi s předchozí, nejstarší
  verzi s následující. Porovnat s předchozí v okně Verze souboru otevře náhled s porovnáním té
  verze s předchozí; na mobilu nese jen ikonu.
- Během porovnání nese hlavička ikonu a jméno souboru, „Porovnání verze 2 s verzí 3“, Zavřít
  porovnání, Stáhnout a listování soubory; Upravit, Verze, Nová verze a Přejmenovat se skryjí.
  Stáhnout stáhne novější z porovnávaných verzí. Zavřít porovnání vrátí náhled k verzi, kterou
  ukazoval předtím; přechod na jiný soubor porovnání ukončí.
- Nad texty stojí výběr Starší a Novější verze s číslem, dnem nahrání, štítkem „aktuální“,
  velikostí a e-mailem toho, kdo verzi nahrál. Starší nabízí jen verze před novější, Novější jen
  verze po starší; ostatní jsou v seznamu neaktivní.
- Vedle sebe stojí starší verze vlevo a novější vpravo a oba texty se posouvají spolu. V jednom
  sloupci stojí odebrané řádky nad přidanými a čísla řádků jsou čísla novější verze. Na mobilu je
  porovnání vždy v jednom sloupci a výběr verzí ukáže jen číslo verze. Texty mají čísla řádků,
  zalomené dlouhé řádky a zvýraznění syntaxe jako náhled a nejdou měnit.
- Odebraný řádek je červený se znaménkem −, přidaný zelený se znaménkem +; v řádku, který se
  změnil, jsou změněná slova zvýrazněná. Úsek aspoň pěti nezměněných řádků dál než tři řádky od
  změny se sbalí do „… 40 nezměněných řádků“, které ho klikem rozbalí.
- Lišta nese přepínač Vedle sebe / V jednom sloupci (na mobilu ne), u kódu Původní / Formátovaně
  (na mobilu Formát), Předchozí změna a Další změna, které přecházejí dokola, souhrn „+12 −5
  řádků“ a hledání. Kód se porovnává naformátovaný; když některou z verzí naformátovat nejde,
  porovná se původní text, přepínač je neaktivní a lišta to řekne. Verze, které se naformátované
  neliší, se porovnají v původním textu.
- Markdown se porovnává formátovaně. Přepínač Formátovaně / Zdrojový kód (na mobilu Formát /
  Zdroj) za přepínačem sloupců přejde na porovnání zdrojového kódu, které je stejné jako u textu;
  volba platí, dokud se porovnání nezavře. Verze, které se formátovaně neliší, se porovnají ve
  zdrojovém kódu.
- Formátované porovnání ukáže obě verze jako náhled Markdownu, které nejdou měnit. Vložený text je
  zelený a podtržený, smazaný červený a přeškrtnutý. Text, kterému se změnil jen formát, a blok
  jiného typu jsou fialové s čárkovaným rámečkem. Blok, kterému se změnil typ nebo formát, nese za
  textem štítek, který změny pojmenuje („nadpis 2 → odstavec; nově tučně“, „adresa odkazu A → B“);
  změnu formátu pojmenuje i tooltip nad textem. Přidaný blok je celý zelený, odebraný celý
  červený. Tabulka se porovná po buňkách; řádky se poznají podle první buňky, když
  se první buňky v každé z tabulek liší. Pod porovnáním stojí legenda barev.
- Vedle sebe stojí protější bloky na stejné výšce. V jednom sloupci stojí odebraný obsah tam, kde
  byl, i odebrané řádky tabulky a položky seznamu. Souhrn počítá bloky („+3 −2 bloky“): změněný
  blok na obou stranách, řádek tabulky jako blok. Nezměněné úseky se nesbalují. Obrázky se
  nenačítají a HTML se nevykoná.
- Hledání v porovnání nerozlišuje velikost písmen ani diakritiku a ukáže pořadí výskytu z počtu;
  výskyt ve sbaleném úseku úsek rozbalí. V jednom sloupci hledá jen v novější verzi. Porovnání
  nemá zvětšení.
- Rozdíl jen v koncích řádků (CRLF, LF nebo CR) se nepočítá; verze, které se jinak neliší, ukážou
  jen „Verze jsou shodné“. Verze větší než 1 MB nebo neplatná v UTF-8 se neporovná a porovnání
  řekne proč („Verzi 3 nelze porovnat: soubor je větší než 1 MB“); Porovnat zůstává aktivní.
  Verze, kterou se nepodaří načíst, ukáže chybu se Zkusit znovu.
- Lišta úprav textu, kódu a Markdownu nese přepínač Změny, na začátku úprav vypnutý. Změny
  porovnají upravený text s verzí, která se upravuje, vždy v původní podobě a v jednom sloupci jako
  porovnání verzí. Za Změny stojí „proti“ s výběrem kterékoli verze souboru, Předchozí změna
  a Další změna jen s ikonou a souhrn „+12 −5 řádků“, který sleduje úpravy. Zapnuté Změny a zvolená
  verze platí až do konce úprav, i přes přepnutí Formátovaně / Zdrojový kód.
- Text, kód a zdrojový kód Markdownu jde ve Změnách dál upravovat; odebrané řádky nejdou měnit.
  Zpět, Znovu, hledání v upraveném textu, Ctrl+S a hlídání neuložených změn platí jako bez Změn.
  Sbalený úsek se rozbalí, když do něj zasáhne úprava nebo výskyt hledání.
- Formátované úpravy Markdownu ukážou se Změnami místo upravovaného textu jeho formátované
  porovnání v jednom sloupci, které nejde měnit. Pod lištou stojí „Změny proti“ s výběrem verze,
  Předchozí změna, Další změna, souhrn v blocích a legenda barev. Tlačítka formátu jsou neaktivní;
  Zpět, Znovu, přepínač, Uložit a hledání, které hledá v porovnání, fungují.
- Verze větší než 1 MB nebo neplatná v UTF-8 se ve Změnách neporovná a řekne proč; verze, kterou
  se nepodaří načíst, ukáže chybu se Zkusit znovu. Upravovaný text zůstává vidět a jde měnit.

### Podpis datové zprávy

- Server ověří podpis uložené datové zprávy i doručenky. Podpis je platný, když obsah odpovídá
  podpisu, zapečetil ho Informační systém datových schránek v produkčním prostředí, který
  provozuje Digitální a informační agentura nebo dříve Ministerstvo vnitra, kvalifikovaným
  certifikátem PostSignum pro elektronickou pečeť platným v okamžiku kvalifikovaného časového
  razítka nad podpisem a certifikát není zneplatněný. Certifikát smí vypršet později. Zpráva bez
  certifikátu pečeti není podepsanou datovou zprávou.
- Certifikáty autorit PostSignum jsou součástí aplikace, i těch, které už PostSignum nepoužívá;
  certifikáty, které soubor nese, se k ověření autority nepoužijí.
- Zneplatnění se ověřuje online u PostSignum. Když PostSignum neodpoví nebo zneplatnění
  certifikátu už ověřit nejde, štítek ukáže „Podpis nelze ověřit“.
- Vedle štítku Datová zpráva nebo Doručenka stojí štítek podpisu: „Ověřuji podpis…“, „Podpis
  ISDS platný“, „Podpis neplatný“ nebo „Podpis nelze ověřit“. Klik na něj ukáže podrobnosti:
  u platného podpisu podepsaného, vydavatele a platnost certifikátu, časové razítko a okamžik
  ověření, že certifikát není odvolán; u neplatného důvod; u neověřitelného vysvětlení a Zkusit
  znovu. Zpráva a přílohy jsou k dispozici, i než ověření doběhne. Chyba
  načtení výsledku se ukáže jako Podpis nelze ověřit.
- Vnořená datová zpráva z přílohy štítek podpisu nemá.
- Výsledek ověření vydává samostatná adresa, jen výsledek bez obsahu souboru. Soubor nad 30 MB
  je neplatný bez čtení.

### Podepsaný soubor

- Soubor p7m nebo p7s je elektronický podpis ve formátu CMS. Podepsaný dokument v něm náhled
  ukáže podle přípony jména bez koncovky p7m nebo p7s (Smlouva.pdf.p7m jako PDF), stejně jako
  soubor karty. Jméno bez známé přípony dostane příponu typu, který ukáže obsah dokumentu
  (Potvrzení.p7s s XML jako Potvrzení.xml); s ní se dokument zobrazí i stáhne. Dokument bez
  náhledu, vnořený podepsaný soubor a vnořená datová zpráva nabídnou jen stažení dokumentu.
- Hlavička podepsaného dokumentu ukáže u velikosti „podepsaný dokument“ se jménem dokumentu
  a nese Přejmenovat, Stáhnout dokument a Stáhnout s příponou souboru (Stáhnout .p7m). Dokument
  se stáhne pod jménem bez koncovky podpisu, soubor celý pod svým jménem.
- Pod hlavičkou je pruh Podpisy: u každého podepisujícího štítek podpisu, jméno, druh podpisu,
  vydavatel certifikátu a čas. Čas je čas časového razítka nad podpisem („časové razítko 12. 10. 2026
  10:14“), bez razítka čas, který uvádí podepisující („bez časového razítka, uvedeno
  12. 10. 2026 15:40“).
- Samostatný podpis, soubor p7m nebo p7s bez dokumentu, ukáže kartu se štítkem Samostatný
  podpis, nadpisem „Podpis souboru“ se jménem bez koncovky podpisu, oddílem Podepsaný soubor
  a oddílem Podpisy s počtem. U každého podepisujícího ukáže štítek podpisu, jméno, druh,
  vydavatele a platnost certifikátu a časové razítko, bez něj čas podpisu s poznámkou, že ho
  uvádí podepisující, a u platného podpisu okamžik ověření, že certifikát není odvolán.
- Druh podpisu určí certifikát: kvalifikovaný elektronický podpis, zaručený elektronický podpis
  založený na kvalifikovaném certifikátu, kvalifikovaná elektronická pečeť, zaručená
  elektronická pečeť založená na kvalifikovaném certifikátu, jinak elektronický podpis.
  Vydavatel PostSignum, I.CA a eIdentity se ukáže zkratkou, jiný jménem z certifikátu.
  Podepisující, jehož certifikát soubor nenese, je Neznámý podepisující.
- Podepisující vydává samostatná adresa, bez obsahu souboru. Když se je nepodaří načíst, pruh
  i karta to řeknou a nabídnou Zkusit znovu; dokument je k dispozici i bez nich. Podpis, který
  nejde přečíst, a soubor s více než 50 podepisujícími pruh i karta ohlásí bez Zkusit znovu.
- Soubor p7m nebo p7s, který není podpisem CMS nebo je větší než 30 MB, nabídne jen stažení.
  Podepsaný soubor v příloze datové zprávy nabídne jen stažení.

### Samostatný podpis a jeho soubor

- Samostatný podpis patří k souboru téhož spisu nebo úkonu, jehož jméno je jméno podpisu bez
  koncovky p7s nebo p7m (Smlouva.pdf.p7s ke Smlouva.pdf). Koncovka smí mít velká písmena, zbytek
  jména se musí shodovat přesně. Když k témuž jménu patří dva soubory s koncovkou p7s či p7m,
  podpis nepatří k žádnému. Přejmenování, nahrání i smazání souboru párování změní. Soubor p7m
  s dokumentem uvnitř samostatným podpisem není. Podpis se ověřuje proti aktuální verzi souboru,
  i u starší verze podpisu; nová verze souboru bez nového podpisu tak ukáže Podpis neplatný.
- Oddíl Podepsaný soubor na kartě ukáže ikonu, jméno a velikost souboru a Otevřít, které soubor
  ukáže v témže náhledu. Bez souboru řekne, že soubor tohoto jména mezi soubory není, a vyzve
  k nahrání pod tímto jménem; nejednoznačný soubor ohlásí.
- Soubor, ke kterému patří samostatný podpis, ukáže pod hlavičkou pruh Podpisy s textem
  „Podepsáno samostatným podpisem“ a tlačítkem se jménem podpisu, které podpis ukáže v témže
  náhledu, a s podepisujícími jako u podepsaného dokumentu.

### Ověření podepsaného souboru

- Server ověří každý podpis podepsaného souboru zvlášť, nad dokumentem uvnitř, u samostatného
  podpisu nad jeho souborem. Podpis je platný, když obsah odpovídá podpisu, certifikát
  podepisujícího je kvalifikovaný certifikát pro podpis nebo pečeť od PostSignum, I.CA nebo
  eIdentity a není odvolán. S časovým razítkem nad podpisem musí certifikát platit v okamžiku
  razítka a smí vypršet později; razítko musí vydat kvalifikovaná služba týchž autorit. Bez
  razítka musí certifikát platit dnes.
- Podpis je neplatný, když obsah neodpovídá podpisu, podepsaný odkaz na certifikát
  podepisujícího ukazuje na jiný certifikát nebo je nečitelný, certifikát neplatil v okamžiku
  razítka, razítko není platné nebo je certifikát odvolán. Podpis bez odkazu na certifikát se
  posoudí bez něj.
- Podpis nelze ověřit, když certifikát nevydala česká kvalifikovaná autorita, soubor certifikát
  podepisujícího nenese, certifikát bez razítka dnes neplatí, autorita neodpoví na dotaz na
  odvolání, samostatný podpis nemá svůj soubor nebo je jeho soubor větší než 30 MB.
- Certifikáty autorit PostSignum, I.CA a eIdentity jsou součástí aplikace; certifikáty, které
  soubor nese, se k ověření autority nepoužijí. Druh podpisu se jen zobrazí, o platnosti
  nerozhoduje.
- Odvolání se ověřuje online u autority, která certifikát vydala.
- Štítek podpisu je „Ověřuji podpis…“, „Podpis platný“, „Podpis neplatný“ nebo „Podpis nelze
  ověřit“. Klik na štítek v pruhu ukáže podrobnosti: u platného podepsaného, druh, vydavatele
  s „platný k času razítka“, bez razítka „platný ke dni ověření“ s poznámkou, že čas uvádí
  podepisující a podpis je ověřen k dnešku, čas razítka nebo podpisu a okamžik ověření, že
  certifikát není odvolán. U neplatného ukáže důvod, u neověřitelného vysvětlení, u certifikátu
  jiné autority i podepsaného a vydavatele. Nedostupná kontrola odvolání a chyba načtení
  výsledku nabídnou Zkusit znovu. Karta samostatného podpisu ukáže důvod a Zkusit znovu přímo
  u podepisujícího.
- Podepisující a dokument jsou vidět, i než ověření doběhne. Chyba načtení výsledku se ukáže
  jako Podpis nelze ověřit.
- Výsledek ověření vydává samostatná adresa, bez obsahu souboru.

## Rozhodnutí

- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.
- Smazání blobu: fyzické / žádné. Platí žádné — bajty přežívají smazaný záznam.
- Limit náhledu datové zprávy: bez limitu / 30 MB. Platí 30 MB.
- Zneplatnění certifikátu podpisu: neověřovat / ověřit online. Platí online; bez odpovědi
  PostSignum podpis nelze ověřit.
- Limit náhledu podepsaného souboru: bez limitu / 30 MB. Platí 30 MB.
- Limit podepisujících podepsaného souboru: bez limitu / 50. Platí 50.
- Čtení podepisujících: v prohlížeči / na serveru. Platí na serveru.
- Ověření podpisu podepsaného souboru: hned / později. Platí hned, na serveru.
- Autority podepsaného souboru: jen PostSignum / české kvalifikované autority / evropský seznam
  důvěryhodných služeb. Platí PostSignum, I.CA a eIdentity.
- Podpis bez časového razítka: neověřovat / ověřit k dnešku. Platí k dnešku.
- Soubor samostatného podpisu: podle jména / ruční přiřazení. Platí podle jména.
- Limit souboru samostatného podpisu: 100 MB / 30 MB. Platí 30 MB.
- Shoda jména při uploadu: s velikostí písmen / bez ní. Platí bez ní.
- Nová verze s jinou příponou: přijmout / odmítnout. Platí odmítnout.
- Upload shodný s aktuální verzí: nová verze / beze změny. Platí beze změny.
- Zvýraznění nově nahraných souborů: trvale / do obnovení stránky. Platí do obnovení stránky.
- Potvrzení smazání verze: v řádku / samostatné okno. Platí samostatné okno.
- Číslo nové verze: počítadlo souboru / nejvyšší existující + 1. Platí nejvyšší existující + 1;
  čísla se nepřečíslovávají.
- Smazání jediné verze: smaže soubor / odmítne se. Platí odmítne se.
- Přejmenování na jméno jiného souboru: povolit / odmítnout. Platí odmítnout.
- Místo karty souborů: pevné / podle toho, zda nese soubor. Platí podle toho, zda nese soubor.
- Datum v seznamu: první nahrání / aktuální verze. Platí aktuální verze; pořadí podle prvního
  nahrání.
- Archiv vlastníka bez souborů: 404 / prázdný archiv. Platí prázdný archiv.
- Úprava starší verze: povolit / jen aktuální verze. Platí jen aktuální verze.
- Souběžná úprava: kontrolovat / nekontrolovat. Platí nekontrolovat; uložení vytvoří verzi nad
  aktuální.
- Kódování upravovaného textu: jakékoli / jen UTF-8. Platí jen UTF-8.
- Po uložení úprav: zůstat v editoru / náhled ke čtení. Platí náhled ke čtení.
- Archiv souborů: sestavit předem / posílat průběžně. Platí průběžně.
- Zápis Markdownu po formátované úpravě: zachovat původní zápis / jednotný. Platí jednotný.
- Režim úpravy Markdownu: vždy formátovaně / podle zobrazení náhledu. Platí podle zobrazení
  náhledu.
- Seznam úkolů v náhledu: políčka měnitelná / neměnná. Platí neměnná.
- Úprava naformátovaného kódu: naformátovaný text / originál. Platí originál.
- Řádky náhledu textu a kódu: nejvýše 5 000 / bez limitu. Platí bez limitu, jen první 1 MB.
- Limit porovnání verzí: bez limitu / 1 MB. Platí 1 MB na verzi.
- Porovnání kódu: původní / naformátovaný. Platí naformátovaný, když jdou naformátovat obě verze
  a naformátované se liší.
- Konce řádků v porovnání: rozdíl / žádný rozdíl. Platí žádný rozdíl.
- Změny formátovaných úprav Markdownu: upravitelné / jen ke čtení. Platí jen ke čtení.

## Dopady

Zápis blobu, uložení a smazání verze se logují, obsah nikdy (SDD-002).
