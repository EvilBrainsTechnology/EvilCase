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
- Neúspěšný upload může zanechat osiřelý blob; tolerují se, úklid není. Mazání drží matice
  v SDD-007.

### Pravidla

- SHA-256 je uložený kontrolní součet, ne deduplikace; nic ho neověřuje.
- Limit velikosti souboru je 100 MB; větší upload vrací 413 a prohlížeč ho odmítne dřív, než
  ho odešle.
- Název souboru je povinný, nejvýše 256 znaků; ukládá se jen jeho poslední část, cesta se
  zahazuje. `MediaType` se bere z uploadu, nejvýše 128 znaků; příponě se nevěří. Delší hodnota
  nebo chybějící název je 400.
- Chybějící `MediaType` se stahuje jako `application/octet-stream`. Soubor bez blobu je 404.

### UI

Drag-and-drop multi-upload na detailu spisu i úkonu; hromadné přetažení běží po souborech
a odmítne jen soubor, který selže. Dávka nad 100 souborů se odmítá celá. Seznam souborů ukazuje
název, velikost a okamžik nahrání, od nejstaršího. Download je autentizovaný požadavek, ne
prostý odkaz; odpověď nese `Content-Disposition: attachment` a `X-Content-Type-Options: nosniff`.
Smazání je prosté, s potvrzením.

## Rozhodnutí

- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.

## Dopady

Zápis a smazání blobu se loguje, obsah nikdy (SDD-002).
