# Produktová vize

Zdroj pravdy o tom, co se z EvilCase staví. Produktový loop čte tento soubor na začátku každého
kola a bootstrap z něj odvozuje labely, milníky a backlog. Roadmapa a otevřené otázky žijí
v GitHub Issues, ne tady.

## Co je EvilCase

Systém pro vedení spisů správních a soudních řízení. Spis v čase roste: přibývají úkony,
dokumenty přicházejí a odcházejí, vedle sebe běží související řízení. Aplikace to všechno drží
na jednom místě a v každém okamžiku odpoví, co se stalo a co ve spisu je.

Současný cíl je **první použitelný základ**: jeden člověk vede reálný spis rukou — každý spis,
úkon, dokument, strana i poznámka žije v aplikaci místo ve složce na disku. Záměrně malý; co
základ vynechává, je dole v non-goals a přijde později, po krocích.

## Horizont

- Teď: jeden uživatel, vlastní spisy, ruční zadávání. Optimalizuje se rychlost práce s reálným
  spisem.
- Později možná: multi-tenant SaaS pro advokátní kanceláře. Přihlášení, sessions a default-deny
  autorizace už stojí; každý agregát nese vlastníka od první migrace (`.claude/rules/data.md`);
  nic dalšího z tenancy se nestaví.

## Doménový model

Názvy v kódu jsou anglické: spis = `Case`, vazba mezi spisy = `CaseRelation`, úkon = `Act`,
soubor = `FileAsset`, odkaz na soubor z jiného úkonu = `ActFileReference`, strana = `Party`,
komentář = `Comment`; spisová značka = `CaseNumber`, číslo jednací = `ActNumber`, cizí,
přidělené někým jiným, jsou `ExternalCaseNumber` a `ExternalActNumber`.

**Spis** — jedno řízení. Nese vlastníka, status, tagy, strany, spisové značky a komentáře.
Spisy jsou si rovné: spis souvisí s N dalšími spisy, vazba je symetrická a holá — bez poznámky,
bez druhu, bez směru. Zobrazuje a nastavuje se vždy jen přímá vazba: detail spisu ukazuje spisy,
s nimiž souvisí, a nic za nimi. Smazání spisu bere jeho vazby s sebou; spisy, s nimiž souvisel,
zůstávají.

**Úkon** — jednotka práce ve spisu: jedno podání, rozhodnutí, vyrozumění nebo výzva. Má směr
(odchozí/příchozí), název, povinné datum, číslo jednací (`ActNumber`, viz Číslování),
u příchozího i číslo vydavatele (`ExternalActNumber`), shrnutí a soubory. Seznamy úkonů se
řadí výhradně podle data úkonu. Soubor převzatý z jiného úkonu se čte přes shrnutí jeho
primárního úkonu.

**Soubor** — příloha úkonu. Patří svému primárnímu úkonu a nese svůj původní název; odkazovat
na něj mohou i další úkony a každý takový odkaz má vlastní název, který původní přetěžuje.
Totéž PDF přiložené v pěti souvisejících spisech je jeden soubor a čtyři odkazy. Soubory se
nahrávají i stahují v prohlížeči, včetně hromadného uploadu přetažením.

**Strana** — úřad, úřední osoba nebo člověk; plochá, sdílená napříč spisy; nese id datové
schránky a adresu jako jeden volný text tištěný po blocích. Vybírá se nebo zakládá inline
všude, kde ji spis, úkon nebo značka jmenuje, a spravuje se v samostatné agendě, která ukazuje,
kde všude figuruje. Smazat jde jen strana, na kterou nic neodkazuje.

**Spisová značka** — spis nese svou interní značku (`CaseNumber`, viz Číslování) plus N cizích
značek (`ExternalCaseNumber`), každou svázanou se stranou, která ji přidělila — každý úřad
v řetězu přiděluje svou.

**Komentář** — volná poznámka ke spisu nebo úkonu. Průběžný deník spisu.

**Status a tagy** — status je malá uzavřená množina (`Active`, `WaitingOnAuthority`,
`Closed`); tagy jsou volný text.

Vše, co uživatel zadá, jde editovat i smazat; destruktivní operace se napřed potvrzuje.

## Číslování

Aplikace vydává vlastní spisové značky a čísla jednací; cokoli přidělené někým jiným zůstává
volný text. Oba vzory jsou celoaplikační konfigurace uložená v databázi a editovaná na
obrazovce Nastavení — její první kus.

- Spisová značka: každý spis bere další číslo z jedné řady, default
  `EC-{year}{month}{day}-{seq}` → `EC-20260804-001`.
- Číslo jednací: každý úkon při vytvoření, default
  `{case-number}-{year}{month}{day}-{seq}` → `EC-20260804-001-20260805-002`.
- `{seq}` počítá v rámci období, které vzor jmenuje — s `{day}` denně, jen s `{year}` ročně,
  u čísla jednacího navíc v rámci spisu. Šířku si vzor určuje sám: `{seq}` jsou tři cifry,
  `{seq:6}` šest. Je to minimum, ne strop — `{seq:6}` píše `000001` a milionté číslo `1000000`.
  Textové řazení řady tedy platí po šířku, kterou vzor zvolil, a dál ne; roční řada chce širší
  `{seq}` než denní.
- Další číslo je o jedna vyšší než nejvyšší, které v řadě už je uložené. Počítá se do něj
  všechno, co má tvar, jaký aktuální vzor dnes píše — i značka zadaná ručně. Smazaný spis tak
  své číslo uvolní a připadne dalšímu.
- Vzor, který by v nejširším případě nevešel do svého sloupce, se neuloží; ve vzoru čísla
  jednacího zabere nejvíc celá spisová značka, ne `{seq}`. Dvě `{seq}` v jednom vzoru se
  neuloží také — z výsledného čísla už nejde přečíst, kde jedna končí a druhá začíná.
- Datum ve značce je den vydání v časové zóně, ve které aplikace běží (v Dockeru `TZ`, default
  `Europe/Prague`); zpětně datovaný úkon se nepřečísluje.
- Vygenerované hodnoty jdou přepsat; databáze hlídá, že spisová značka je unikátní v rámci
  vlastníka a číslo jednací v rámci svého spisu — ručně zavedený starý spis si nechá svou
  historickou značku. Změna vzoru žádné uložené číslo nepřepisuje.
- Přepsaná spisová značka už vydaná čísla jednací nepřečísluje ani řadu nezačíná znovu: řada
  patří spisu, ne značce v ní. Další úkon pokračuje v počítání, jen je napsaný pod novou
  značkou — jeden spis tak může nést čísla jednací dvojí podoby.
- Hledání matchuje značky i čísla jednací včetně prefixu; přesná shoda skočí rovnou na spis
  nebo úkon.

## Vzorová data

`EvilBrains__EvilCase__Database__SeedSampleData` (default `false`) naplní databázi při startu,
v jakémkoli prostředí, jen dokud neobsahuje žádný spis. Data jsou pseudonymizovaný případ
o překročení rychlosti z `test-data/case-01-speeding.md`, celý: několik vzájemně souvisejících
spisů, strany, značky, úkony se syntetickými PDF, komentáře — každá obrazovka se tak staví
a ověřuje nad rozsahem reálného případu, včetně dokumentu sdíleného několika spisy.

## Priority

V pořadí podle toho, co při práci s reálným spisem bolí nejvíc:

1. Všechno k případu na jednom místě — spis, spisy s ním související, úkony a dokumenty — místo
   složky na disku.
2. Zavedení nového úkonu s jeho dokumenty pod minutu.
3. Rychlé nalezení spisu i úkonu, textem i značkou; hledání ignoruje diakritiku.

## Milníky

| # | Milník | Dodá |
| --- | --- | --- |
| M1 | Model a vzorová data | hierarchie spisů nahrazená vazbou mezi spisy; model úkonu ořezaný na jedno povinné datum bez pořadového čísla, seznamy úkonů řazené datem; zjednodušený model souboru (primární úkon, pojmenované odkazy, bez rolí); přejmenování identifikátorů v kódu na `CaseNumber`, `ActNumber`, `ExternalCaseNumber` a `ExternalActNumber`; číslování s výchozími vzory; seed přepínač nahrávající vzorová data |
| M2 | UI spisu | detail spisu se souvisejícími spisy a komentáři; založení, editace a smazání spisu, včetně přidání a odebrání vazby; značky, status a tagy; obrazovka Nastavení se vzory číslování; hledání bez ohledu na diakritiku |
| M3 | Úkony a soubory | seznam úkonů v detailu spisu; stránka úkonu se shrnutím, soubory a komentáři; přidání, editace a smazání úkonu; upload včetně hromadného přetažením a download |
| M4 | Strany | samostatná agenda; inline výběr nebo založení všude, kde se strana jmenuje |

Základ je hotový, když jde reálný spis vést rukou od začátku do konce.

## Non-goals pro teď

Lhůty, timeline, import složek (testovací data vstupují ad hoc nebo seedem), extrakce textu a
fulltext, .docx šablony a generovaná podání, datové schránky (ISDS), e-mail, AI shrnutí,
multi-tenancy, role, pozvánky, billing, správa uživatelů nad rámec seedovaného administrátora.
Model všem nechává místo; nic z toho se nestaví.

## Soukromí

Repozitář je veřejný. `.claude/rules/github.md` drží reálný obsah spisů mimo každý zápis;
testovací fixtures jsou syntetické nebo pseudonymizované (`test-data/README.md`). Reálné složky
spisů na disku jsou jen ke čtení.
