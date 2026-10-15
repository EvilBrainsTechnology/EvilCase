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
- Chybějící `MediaType` se stahuje jako `application/octet-stream`. Soubor bez blobu je 404.

### UI

Drag-and-drop multi-upload na detailu spisu i úkonu; soubory lze pustit kamkoli na kartu
souborů a karta při tažení ukáže, kam se nahrají, a limity. Hromadné přetažení běží po
souborech a odmítne jen soubor, který selže. Dávka nad 100 souborů se odmítá celá. Seznam
souborů ukazuje ikonu typu, název, velikost a okamžik nahrání, od nejstaršího. Download je
autentizovaný požadavek, ne prostý odkaz; odpověď nese `Content-Disposition: attachment`
a `X-Content-Type-Options: nosniff`. Smazání je prosté, s potvrzením.

Klik na řádek souboru otevře náhled; soubor typu bez náhledu se stáhne. Stažení, přejmenování
a smazání jsou akce řádku vedle toho.

Přejmenování mění jen jméno bez přípony, přípona zůstává. Potvrzuje se Enterem nebo Uložit,
Esc a Zrušit ho zahodí.

### Náhled

- Náhled běží v prohlížeči nad staženými bajty; typ se pozná podle přípony, uložený
  `MediaType` se nepoužije.
- Zobrazí se PDF, obrázky (jpg, jpeg, png, gif, webp, avif, bmp, svg), text (txt, md, csv, tsv,
  json, xml, log, isdoc, html), zvuk (mp3, m4a, wav, ogg, oga) a video (mp4, webm, m4v, mov).
  Jiný typ nabídne jen stažení.
- Text je vždy prostý text s čísly řádků, nikdy vykreslená stránka; ukáže se nejvýše první
  1 MB a nejvýše 5 000 řádků a náhled řekne, že je soubor delší. SVG je jen obrázek.
- Náhled listuje mezi soubory karty, ze které se otevřel.

## Rozhodnutí

- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.
- Smazání blobu: fyzické / žádné. Platí žádné — bajty přežívají smazaný záznam.

## Dopady

Zápis blobu se loguje, obsah nikdy (SDD-002).
