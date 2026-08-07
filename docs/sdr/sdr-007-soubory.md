# SDR-007 — Soubory

- **Stav:** platí
- **Milníky:** M5
- **Související SDR:** [002](sdr-002-domenovy-model.md), [015](sdr-015-testovani.md),
  [016](sdr-016-logovani-a-observabilita.md)

## Rozsah

Ukládání souborů, vlastnictví, upload, download a mazání.

## Popis

### Vlastnictví

FileAsset patří právě jednomu spisu XOR úkonu (check constraint). Žádné vazby ani odkazy
mezi soubory a jinými entitami — vědomé zjednodušení první iterace; tentýž dokument ve dvou
spisech jsou dva soubory.

### Úložiště

- Bajty leží na souborovém systému pod `{root}/{tenantId}/{fileAssetId}`; root dává
  `EvilBrains__EvilCase__Files__RootPath`.
- Databáze nese jen metadata: název, velikost, `MediaType`, SHA-256 hash.
- Zápis je atomický: dočasný soubor, pak rename.
- Blob zaniká se záznamem.

### Pravidla

- SHA-256 je kontrolní součet, ne deduplikace.
- Limit velikosti souboru je 100 MB.
- `MediaType` se bere z uploadu; příponě se nevěří.

### UI

Drag-and-drop multi-upload na detailu spisu i úkonu; download v prohlížeči. Smazání je
prosté, s potvrzením.

## Rozhodnutí

- Odkazy souboru do více úkonů: zůstávají / zanikají. Zanikají i s `ActFileReference`.
- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.

## Dopady

`ActFileReference` zaniká (SDR-002). Úložiště se testuje na temp adresáři (SDR-015). Zápis
a smazání blobu se loguje, obsah nikdy (SDR-016).
