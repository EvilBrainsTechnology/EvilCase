# SDD-012 — Soubory

- **Stav:** platí
- **Milníky:** M2, M5
- **Související SDD:** [001](sdd-001-architektura.md),
  [002](sdd-002-logovani-a-observabilita.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Ukládání souborů, vlastnictví, upload, download a mazání.

## Popis

### Vlastnictví

FileAsset patří právě jednomu spisu XOR úkonu (check constraint). Žádné vazby mezi soubory a
jinými entitami; tentýž dokument ve dvou spisech jsou dva soubory.

### Úložiště

- Bajty leží na souborovém systému pod kořenem z konfigurace; databáze nese jen metadata:
  název, velikost, `MediaType`, SHA-256 hash, cestu k blobu.
- Blob se zapisuje před commitem databázové transakce; osiřelý blob po neúspěšné transakci se
  toleruje, bez automatického úklidu. Mazání drží matice v SDD-007.
- Kód úložiště žije v `Common/EvilCase.Files` (SDD-001).

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
- Přípona v názvu blobu: zůstává / nezůstává. Nezůstává.

## Dopady

Zápis a smazání blobu se loguje, obsah nikdy (SDD-002).
