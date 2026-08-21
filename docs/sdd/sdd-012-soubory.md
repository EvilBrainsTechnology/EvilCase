# SDD-012 — Soubory

- **Stav:** platí
- **Milníky:** M2, M5
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md), [003](sdd-003-testovani.md),
  [007](sdd-007-domenovy-model.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Ukládání souborů, vlastnictví, upload, download a mazání.

## Popis

### Vlastnictví

FileAsset patří právě jednomu spisu XOR úkonu (check constraint). Žádné vazby ani odkazy
mezi soubory a jinými entitami — vědomé zjednodušení první iterace; tentýž dokument ve dvou
spisech jsou dva soubory.

### Úložiště

- Bajty leží pod `{root}/{tenantId}/{aa}/{bb}/{fileAssetId}`; `aa` jsou poslední dva hex znaky
  UUIDv7 souboru, `bb` dva před nimi — přední část v7 je timestamp a nerozprostřela by se.
- Blob se jmenuje jen id souboru, bez přípony; jméno se nikdy nebere z uploadu.
- Relativní cesta se vrací ze zápisu a ukládá do `FileAsset.StoragePath`; každé pozdější čtení
  i smazání jde přes ni, takže změna schématu rozložení nezneviditelní staré bloby.
- `EvilBrains__EvilCase__Files__RootPath` se váže přes options s validací datovými anotacemi
  při startu; nenastavený root shodí start hostitele, ne až první upload.
- Relativní root se vyhodnocuje proti adresáři aplikace, ne proti pracovnímu adresáři.
- Databáze nese jen metadata: název, velikost, `MediaType`, SHA-256 hash, cestu k blobu.
- Zápis je atomický: dočasný soubor, pak rename.
- Blob zaniká se záznamem.
- Blob se zapisuje před commitem databázové transakce; osiřelý blob po neúspěšné transakci
  se toleruje, bez automatického úklidu.
- Kód úložiště žije v `Common/EvilCase.Files` (SDD-001); `EvilCase.Data` nese jen metadata.
- Jádro úložiště — konfigurace rootu, zápis a smazání blobu — vzniká v M2, seed zapisuje TXT
  soubory (SDD-017); M5 dodává jen UI.

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
- Přípona v názvu blobu: zůstává / nezůstává. Nezůstává, blob nese jen id souboru.

## Dopady

`ActFileReference` zaniká (SDD-007). Úložiště se testuje na temp adresáři (SDD-003). Zápis
a smazání blobu se loguje, obsah nikdy (SDD-002). V nasazeném image je root pevně
`/var/lib/evilcase/files`; `deploy/docker-compose.yml` na něj jen připojí svazek.
