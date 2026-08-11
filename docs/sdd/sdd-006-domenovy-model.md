# SDD-006 — Doménový model

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [005](sdd-005-tenance-a-ucty.md), [008](sdd-008-spisy.md) až
  [012](sdd-012-komentare.md), [013](sdd-013-vyhledavani.md), [016](sdd-016-seed-vzorovych-dat.md)

## Rozsah

Mapa entit, společné vlastnosti, co zaniká, matice mazání a reset migrací. Detaily
jednotlivých entit drží SDD-008 až 012.

## Popis

### Mapa entit

Account → Tenant → User (SDD-005). Tenantová data:

- **Case** — spis, volitelný rodič `ParentCaseId` (SDD-008).
- **ExternalCaseNumber** — externí značka spisu, vázaná na Contact (SDD-008).
- **Act** — úkon spisu (SDD-009).
- **ExternalActNumber** — externí číslo jednací úkonu, vázané na Contact (SDD-009).
- **Contact** — kontakt, dřívější Party (SDD-010).
- **FileAsset** — soubor spisu XOR úkonu (SDD-011).
- **Comment** — komentář spisu XOR úkonu (SDD-012).

### Zaniká

- `CaseRelation` — nahrazuje rodičovská vazba na spisu.
- `CaseTag` — tagy bez náhrady.
- `ActFileReference` — soubor patří právě jednomu vlastníku.
- Sloupec `Act.ExternalActNumber` — nahrazuje tabulka `ExternalActNumber`.

### Společné vlastnosti

- Id je UUIDv7, generované v aplikaci (`Guid.CreateVersion7()`).
- Každá entita nese `Created` a `Updated`; plní je jeden `SaveChangesInterceptor` nad
  `TimeProvider`.
- Tenantové entity nesou `TenantId` a `UserId` (SDD-005).
- Datum spisu a úkonu je `DateOnly` (`.claude/rules/data.md`).
- Délky řetězců: název 256, popis 4000, název kontaktu 256, adresa 1024, id datové
  schránky 16, hodnota externího čísla 128.

### Matice mazání

| Entita | Smazání |
| --- | --- |
| Case | kaskáda: úkony, komentáře, značky, soubory; podřízené spisy přežijí bez rodiče |
| Act | kaskáda: komentáře, externí čísla jednací, soubory |
| Contact | jen neodkazovaný; defaultní kontakt nikdy (SDD-010) |
| FileAsset | prosté; blob zaniká se záznamem (SDD-011) |
| Comment | prosté; jen autor (SDD-012) |

Každé smazání se v UI potvrzuje (SDD-003).

### Reset schématu

Dnešních 12 migrací se maže i se snapshotem; nové schéma zakládá jedna migrace `Init`. Init
zakládá i rozšíření `unaccent` a `pg_trgm`, IMMUTABLE obálku `unaccent`, GIN fulltextové
indexy a GIN trigram indexy vyhledávání (SDD-013) — M7 migraci nepotřebuje. Nasazená data se
zahodí — databázi dropne owner ručně.

## Rozhodnutí

- Id: `long` sekvence / UUIDv7. Platí UUIDv7 generované v aplikaci.
- Vazby spisů: symetrická relace / rodičovská hierarchie. Platí rodič.
- Tagy: zůstávají / zanikají. Zanikají.
- Migrace: řetěz na stávajících 12 / reset. Platí reset jednou `Init` migrací.
- Optimistická konkurence: token / bez tokenu. Platí zatím bez tokenu — poslední zápis
  vyhrává.

## Dopady

Modelové testy v `Tests/Data/Model/` se přepisují na nový model (SDD-002). Seed vzorových
dat sleduje model (SDD-016). Jeden nedělitelný slice je jen samotný reset schématu — starý
model s migracemi ven, nové entity s `Init` dovnitř, spolu s kódem a testy, které to rozbije;
číslování, jádro souborového úložiště a vzorový seed jsou samostatné slices M2.
