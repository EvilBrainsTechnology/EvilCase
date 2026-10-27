# SDD-012 — Soubory

- **Stav:** platí
- **Milníky:** M2, M5
- **Související SDD:** [001](sdd-001-architektura.md),
  [002](sdd-002-logovani-a-observabilita.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Ukládání souborů, vlastnictví, upload, download a mazání.

## Popis

### Vlastnictví

FileAsset patří právě jednomu spisu XOR úkonu. Žádné vazby mezi soubory a jinými entitami;
tentýž dokument ve dvou spisech jsou dva soubory.

### Úložiště

- Bajty leží na souborovém systému pod kořenem z konfigurace
  (`EvilBrains__EvilCase__Files__RootPath`, povinný); databáze nese jen metadata: název,
  velikost, `MediaType`, SHA-256 hash, cestu k blobu. Blob se jmenuje podle id souboru,
  bez přípony.
- Blob se nikdy fyzicky nemaže. Neúspěšný upload i smazaný záznam (SDD-007) zanechají osiřelý
  blob; osiřelé blob nejde ani vypsat, ani uklidit — úložiště jen roste.

### Pravidla

- SHA-256 je uložený kontrolní součet, ne deduplikace; nic ho neověřuje.
- Limit velikosti souboru je 100 MB; větší upload vrací 413 a prohlížeč ho odmítne dřív, než
  ho odešle.
- Název souboru je povinný, nejvýše 256 znaků; ukládá se jen jeho poslední část, cesta se
  zahazuje. `MediaType` se bere z uploadu, nejvýše 128 znaků; příponě se nevěří. Delší hodnota
  nebo chybějící název je 400.
- Soubor jde přejmenovat. Platí pro nové jméno stejná pravidla jako při uploadu; bajty, hash
  a `MediaType` zůstávají. Soubor cizího vlastníka je 404.
- Soubor bez blobu je 404.

### UI

Drag-and-drop multi-upload na detailu spisu i úkonu; soubory lze pustit kamkoli na kartu
souborů a karta při tažení ukáže, kam se nahrají, a limity. Hromadné přetažení běží po
souborech a odmítne jen soubor, který selže. Dávka nad 100 souborů se odmítá celá. Soubory
přetažené nebo vybrané během nahrávání se odmítnou s hláškou a nenahrají se; rozběhlá dávka
doběhne. Seznam souborů ukazuje ikonu typu, název, velikost a okamžik nahrání, od
nejstaršího. Obsah souboru
se vydává z jedné adresy a odpověď nese `X-Content-Type-Options: nosniff`. Rastrové
obrázky, zvuk a video nesou `Content-Disposition: inline` a typ podle přípony; každý jiný typ,
PDF, SVG a HTML také, nese `attachment` a typ `application/octet-stream`; uložený `MediaType` se
v odpovědi nikdy nevrací. Obsah souboru nikdy nespustí skript a vložit do stránky ho smí jen
aplikace sama. Smazání je prosté, s potvrzením.

Klik na řádek souboru otevře náhled; u typu bez náhledu ten řekne, že není k dispozici, a
nabídne stažení. Stažení je obyčejné stažení prohlížečem a uloží soubor vždy. Stažení,
přejmenování a smazání jsou akce řádku vedle toho.

Přejmenování mění jen jméno bez přípony, přípona zůstává. Potvrzuje se Enterem nebo Uložit,
Esc a Zrušit ho zahodí.

### Náhled

- Rastrové obrázky (jpg, jpeg, png, gif, webp, avif, bmp), zvuk a video zobrazí
  prohlížeč sám z adresy obsahu; ostatní typy zobrazí aplikace. Typ se pozná podle přípony,
  uložený `MediaType` se nepoužije. Příloha datové zprávy se ukáže ze zprávy samé.
- Zobrazí se PDF, obrázky (jpg, jpeg, png, gif, webp, avif, bmp, svg, heic, heif),
  text (txt, csv, tsv, log), kód (json, xml, isdoc, yaml, yml, html), Markdown (md, markdown), dokument Word
  (docx), sešit (xlsx, xls, ods), e-mail (eml, msg), datovou zprávu a doručenku ISDS (zfo), zvuk (mp3, m4a, wav, ogg, oga) a video
  (mp4, webm, m4v, mov). Jiný typ nabídne jen stažení. Soubor, který se nepodaří přečíst, nabídne
  také jen stažení.
- PDF, text, kód, Markdown, e-mail, datová zpráva a dokument Word mají nad obsahem stejnou lištu se zvětšením
  a zmenšením (50 až 400 %, také Ctrl nebo Cmd s kolečkem myši), na počítači i se skutečnou
  velikostí 100 %, a s hledáním. Hledání nerozlišuje velikost písmen ani diakritiku, zvýrazní
  všechny výskyty, aktuální odliší a posune do zobrazení a ukáže pořadí z počtu (3 z 12), nebo
  Nenalezeno; Enter přejde na další výskyt, Shift+Enter na předchozí, Esc hledání vymaže a náhled
  nezavře.
- PDF se ukáže jako stránky pod sebou, zpočátku na šířku náhledu, nejvýš však ve 100 %, i
  v mobilním prohlížeči. Lišta navíc ukáže stránku z počtu stran s polem pro skok na stránku
  a přizpůsobení na šířku a na celou stránku. Text jde vybrat a zkopírovat.
- U textu, kódu, Markdownu, e-mailu a datové zprávy zvětšení mění velikost písma a text se zalomí do šířky náhledu;
  zpočátku je 100 %.
- Odkaz v PDF na místo v dokumentu přejde na jeho stránku, odkaz na webovou nebo e-mailovou adresu
  se otevře v nové záložce, jiný odkaz nefunguje. Formuláře, skripty a vložená média nefungují.
  PDF chráněné heslem nebo nečitelné nabídne jen stažení.
- Text je vždy prostý text s čísly řádků, nikdy vykreslená stránka; ukáže se nejvýše první
  1 MB a nejvýše 5 000 řádků a náhled řekne, že je soubor delší. SVG je jen obrázek.
- Kód se ukáže jako text s čísly řádků a barevně zvýrazněnou syntaxí, nikdy jako vykreslená
  stránka; HTML se nevykoná. Ukáže se nejvýše 5 000 řádků a náhled řekne, že je soubor delší.
  Zpočátku je kód naformátovaný; tlačítko Formátovat na liště přepíná mezi naformátovaným
  a původním textem a každý nově zobrazený soubor začne naformátovaný. Soubor delší než 1 MB
  ukáže jen svůj začátek v původní podobě, soubor neplatný ve svém jazyce (ISDOC jako XML) celý
  v původní podobě; tlačítko je pak vypnuté a náhled řekne proč.
- Markdown se ukáže formátovaně, nejvýše první 1 MB. Syrové HTML v něm se nevykoná, ukáže se
  jako text; odkaz zůstane jen na webovou nebo e-mailovou adresu a otevře se v nové záložce;
  obrázky se nenačítají.
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
  přílohy s textem „příloha zprávy" a jménem zprávy, u víc příloh přechod mezi nimi s pořadím
  „příloha 2 z 5", Stáhnout přílohu a zavření; příloha se nepřejmenovává. Příloha bez náhledu
  nabídne jen své stažení.
- Příloha se stáhne pod svým jménem bez
  cesty a bez znaků, které jméno souboru nesmí obsahovat; prázdné jméno nahradí „příloha".
- HEIC a HEIF se ukážou jako obrázek.
- Náhled listuje mezi soubory karty, ze které se otevřel.

### Podpis datové zprávy

- Server ověří podpis uložené datové zprávy i doručenky. Podpis je platný, když obsah odpovídá
  podpisu, zapečetil ho Informační systém datových schránek (Digitální a informační agentura)
  kvalifikovaným certifikátem PostSignum pro elektronickou pečeť platným v okamžiku
  kvalifikovaného časového razítka nad podpisem a certifikát není zneplatněný. Certifikát smí
  vypršet později. Zpráva bez certifikátu pečeti není podepsanou datovou zprávou.
- Certifikáty autorit PostSignum jsou součástí aplikace; certifikáty, které soubor nese, se
  k ověření autority nepoužijí.
- Zneplatnění se ověřuje online u PostSignum. Když PostSignum neodpoví, podpis nelze ověřit.
- Vedle štítku Datová zpráva nebo Doručenka stojí štítek podpisu: „Ověřuji podpis…“, „Podpis
  ISDS platný“, „Podpis neplatný“ nebo „Podpis nelze ověřit“. Klik na něj ukáže podrobnosti:
  u platného podpisu podepsaného, vydavatele a platnost certifikátu, časové razítko a okamžik
  ověření, že certifikát není odvolán; u neplatného důvod; u neověřitelného vysvětlení a Zkusit
  znovu. Zpráva a přílohy jsou k dispozici, i než ověření doběhne. Chyba
  načtení výsledku se ukáže jako Podpis nelze ověřit.
- Vnořená datová zpráva z přílohy štítek podpisu nemá.
- Výsledek ověření vydává samostatná adresa, jen výsledek bez obsahu souboru. Soubor nad 30 MB
  je neplatný bez čtení.

## Rozhodnutí

- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.
- Smazání blobu: fyzické / žádné. Platí žádné — bajty přežívají smazaný záznam.
- Limit náhledu datové zprávy: bez limitu / 30 MB. Platí 30 MB.
- Zneplatnění certifikátu podpisu: neověřovat / ověřit online. Platí online; bez odpovědi
  PostSignum podpis nelze ověřit.

## Dopady

Zápis blobu se loguje, obsah nikdy (SDD-002).
