# SDR-002 — Doménový model

- **Stav:** platí
- **Milníky:** M2
- **Související SDR:** [001](sdr-001-tenance-a-ucty.md), [004](sdr-004-spisy.md) až
  [008](sdr-008-komentare.md), [012](sdr-012-seed-vzorovych-dat.md)

## Rozsah

Mapa entit, společné vlastnosti, co zaniká, matice mazání a reset migrací. Detaily
jednotlivých entit drží SDR-004 až 008.

## Popis

### Mapa entit

Account → Tenant → User (SDR-001). Tenantová data:

- **Case** — spis, volitelný rodič `ParentCaseId` (SDR-004).
- **ExternalCaseNumber** — externí značka spisu, vázaná na Contact (SDR-004).
- **Act** — úkon spisu (SDR-005).
- **ExternalActNumber** — externí číslo jednací úkonu, vázané na Contact (SDR-005).
- **Contact** — kontakt, dřívější Party (SDR-006).
- **FileAsset** — soubor spisu XOR úkonu (SDR-007).
- **Comment** — komentář spisu XOR úkonu (SDR-008).

### Zaniká

- `CaseRelation` — nahrazuje rodičovská vazba na spisu.
- `CaseTag` — tagy bez náhrady.
- `ActFileReference` — soubor patří právě jednomu vlastníku.
- Sloupec `Act.ExternalActNumber` — nahrazuje tabulka `ExternalActNumber`.

### Společné vlastnosti

- Id je UUIDv7, generované v aplikaci (`Guid.CreateVersion7()`).
- Každá entita nese `Created` a `Updated`; plní je jeden `SaveChangesInterceptor` nad
  `TimeProvider`.
- Tenantové entity nesou `TenantId` a `CreatedBy` (SDR-001).
- Datum spisu a úkonu je `DateOnly` (`.claude/rules/data.md`).

### Matice mazání

| Entita | Smazání |
| --- | --- |
| Case | kaskáda: úkony, komentáře, značky, soubory; podřízené spisy přežijí bez rodiče |
| Act | kaskáda: komentáře, externí čísla jednací, soubory |
| Contact | jen neodkazovaný; defaultní kontakt nikdy (SDR-006) |
| FileAsset | prosté; blob zaniká se záznamem (SDR-007) |
| Comment | prosté; jen autor (SDR-008) |

Každé smazání se v UI potvrzuje (SDR-014).

### Reset schématu

Dnešních 12 migrací se maže i se snapshotem; nové schéma zakládá jedna migrace `Init`.
Nasazená data se zahodí — databázi dropne owner ručně.

## Rozhodnutí

- Id: `long` sekvence / UUIDv7. Platí UUIDv7 generované v aplikaci.
- Vazby spisů: symetrická relace / rodičovská hierarchie. Platí rodič.
- Tagy: zůstávají / zanikají. Zanikají.
- Migrace: řetěz na stávajících 12 / reset. Platí reset jednou `Init` migrací.

## Dopady

Modelové testy v `Tests/Data/Model/` se přepisují na nový model (SDR-015). Seed vzorových
dat sleduje model (SDR-012). Schéma M2 je jeden nedělitelný slice — migrace kolidují.
