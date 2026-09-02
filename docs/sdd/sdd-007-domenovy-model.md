# SDD-007 — Doménový model

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [009](sdd-009-spisy.md) až
  [013](sdd-013-komentare.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Mapa entit, společné vlastnosti, co zaniká, matice mazání a migrace. Detaily
jednotlivých entit drží SDD-009 až 012.

## Popis

### Mapa entit

Account → Tenant → User (SDD-006). Tenantová data:

- **Case** — spis, volitelný rodič `ParentCaseId` (SDD-009).
- **ExternalCaseNumber** — externí značka spisu, vázaná na Contact (SDD-009).
- **Act** — úkon spisu (SDD-010).
- **ExternalActNumber** — externí číslo jednací úkonu, vázané na Contact (SDD-010).
- **Contact** — kontakt, dřívější Party (SDD-011).
- **FileAsset** — soubor spisu XOR úkonu (SDD-012).
- **Comment** — komentář spisu XOR úkonu (SDD-013).

### Zaniká

- `CaseRelation` — nahrazuje rodičovská vazba na spisu.
- `CaseTag` — tagy bez náhrady.
- `ActFileReference` — soubor patří právě jednomu vlastníku.
- Sloupec `Act.ExternalActNumber` — nahrazuje tabulka `ExternalActNumber`.

### Společné vlastnosti

- Id je UUIDv7, generované v aplikaci (`Guid.CreateVersion7()`).
- Každá entita nese `Created` a `Updated`; plní je trigger v databázi z jejích hodin (SDD-018).
- Tenantové entity nesou `TenantId` a vlastníka `UserId`; kontakt vlastníka nemá (SDD-006).
  Obě hodnoty na nový řádek doplní zápis z `IUserContext`.
- Datum spisu a úkonu je `DateOnly` (`.claude/rules/data.md`).
- Každá entita kromě `RefreshToken` nese `Deleted`: prázdné, dokud řádek žije, jinak okamžik
  jeho smazání.

### Matice mazání

Smazání je razítko: řádek dostane `Deleted`, zmizí z každého čtení a zůstane v databázi. Spisová
značka, číslo jednací i hodnota externí značky proto zůstávají obsazené a smazaný soubor si své
bajty drží.

| Entita | Smazání |
| --- | --- |
| Case | kaskáda: úkony, komentáře, značky, soubory i podřízené spisy |
| Act | kaskáda: komentáře, externí čísla jednací, soubory |
| Contact | jen kontakt, na který neodkazuje nic, ani smazaný řádek; defaultní kontakt nikdy (SDD-011) |
| FileAsset | prosté; blob na disku zůstává (SDD-012) |
| Comment | prosté; jen autor (SDD-013) |

Celá kaskáda nese jeden okamžik. Řádek, který vzalo dřívější smazání, si svůj okamžik nechá,
takže se s pozdější kaskádou nevrací.

Každé smazání se v UI potvrzuje (SDD-004). Obnovu ani trvalé smazání UI nenabízí.

### Migrace

Schéma začíná migrací `Init`; řetěz migrací od ní jen roste a nikdy se nepřepisuje.

## Rozhodnutí

- Id: `long` sekvence / UUIDv7. Platí UUIDv7 generované v aplikaci.
- Vazby spisů: symetrická relace / rodičovská hierarchie. Platí rodič.
- Tagy: zůstávají / zanikají. Zanikají.
- Migrace: řetěz na starých 12 / reset. Platil reset; schéma začíná migrací `Init`.
- Optimistická konkurence: token / bez tokenu. Platí zatím bez tokenu — poslední zápis
  vyhrává.
- Smazání: řádek zaniká / razítko. Platí razítko na každé entitě kromě `RefreshToken`.
- Obsazenost čísel po smazání: uvolní se / zůstane. Platí zůstane — obnova nikdy nekoliduje.
- Podřízené spisy při smazání rodiče: osiření / kaskáda. Platí kaskáda (SDD-009).

## Dopady

Modelové testy v `Tests/Data/Model/` sledují model (SDD-003). Seed vzorových dat sleduje
model (SDD-017).
