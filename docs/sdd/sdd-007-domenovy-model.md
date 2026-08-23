# SDD-007 — Doménový model

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [009](sdd-009-spisy.md) až
  [013](sdd-013-komentare.md), [014](sdd-014-vyhledavani.md), [017](sdd-017-seed-vzorovych-dat.md)

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

### Matice mazání

| Entita | Smazání |
| --- | --- |
| Case | kaskáda: úkony, komentáře, značky, soubory; podřízené spisy přežijí bez rodiče |
| Act | kaskáda: komentáře, externí čísla jednací, soubory |
| Contact | jen neodkazovaný; defaultní kontakt nikdy (SDD-011) |
| FileAsset | prosté; blob zaniká se záznamem (SDD-012) |
| Comment | prosté; jen autor (SDD-013) |

Každé smazání se v UI potvrzuje (SDD-004).

### Migrace

Schéma začíná migrací `Init`; řetěz migrací od ní jen roste a nikdy se nepřepisuje.
Vyhledávací rozšíření a indexy (SDD-014) nese schéma od `Init` — M7 migraci nepotřebuje.

## Rozhodnutí

- Id: `long` sekvence / UUIDv7. Platí UUIDv7 generované v aplikaci.
- Vazby spisů: symetrická relace / rodičovská hierarchie. Platí rodič.
- Tagy: zůstávají / zanikají. Zanikají.
- Migrace: řetěz na starých 12 / reset. Platil reset; schéma začíná migrací `Init`.
- Optimistická konkurence: token / bez tokenu. Platí zatím bez tokenu — poslední zápis
  vyhrává.

## Dopady

Modelové testy v `Tests/Data/Model/` sledují model (SDD-003). Seed vzorových dat sleduje
model (SDD-017).
