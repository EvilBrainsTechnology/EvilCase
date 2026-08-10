# SDR-011 — Soubory

- **Stav:** platí
- **Milníky:** M2, M5
- **Související SDR:** [001](sdr-001-logovani-a-observabilita.md), [002](sdr-002-testovani.md),
  [006](sdr-006-domenovy-model.md), [016](sdr-016-seed-vzorovych-dat.md)

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
- Blob se zapisuje před commitem databázové transakce; osiřelý blob po neúspěšné transakci
  se toleruje, bez automatického úklidu.
- Jádro úložiště — konfigurace rootu, zápis a smazání blobu — vzniká v M2, seed zapisuje TXT
  soubory (SDR-016); M5 dodává jen UI.

### Pravidla

- SHA-256 je uložený kontrolní součet, ne deduplikace; zatím ho nic neověřuje.
- Limit velikosti souboru je 100 MB; větší upload vrací 413.
- `MediaType` se bere z uploadu; příponě se nevěří.

### UI

Drag-and-drop multi-upload na detailu spisu i úkonu; hromadné přetažení běží po souborech
a odmítne jen soubor, který selže. Download jde přes fetch a Blob URL — nese autorizační
hlavičku; endpoint posílá `Content-Disposition: attachment`
a `X-Content-Type-Options: nosniff`. Smazání je prosté, s potvrzením.

## Rozhodnutí

- Odkazy souboru do více úkonů: zůstávají / zanikají. Zanikají i s `ActFileReference`.
- Deduplikace hashem: ano / ne. Ne.
- Limit velikosti: bez limitu / 100 MB. Platí 100 MB.
- Úložiště: databáze / souborový systém. Platí souborový systém, metadata v databázi.

## Dopady

`ActFileReference` zaniká (SDR-006). Úložiště se testuje na temp adresáři (SDR-002). Zápis
a smazání blobu se loguje, obsah nikdy (SDR-001). Nasazený kontejner nese `RootPath` na
trvalém svazku; `deploy/docker-compose.yml` a `deploy/README.md` se mění s jádrem úložiště
v M2.
