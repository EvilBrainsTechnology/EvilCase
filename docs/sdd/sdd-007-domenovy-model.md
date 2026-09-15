# SDD-007 — Doménový model

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [009](sdd-009-spisy.md) až
  [013](sdd-013-komentare.md), [017](sdd-017-seed-vzorovych-dat.md), [019](sdd-019-stitky.md)

## Rozsah

Mapa entit, společné vlastnosti, matice mazání a migrace. Detaily jednotlivých entit drží
SDD-009 až 013 a SDD-019.

## Popis

### Mapa entit

Account → Tenant → User (SDD-006). Tenantová data:

- **Case** — spis, volitelný rodič `ParentCaseId`, nepovinný kontakt (SDD-009).
- **Act** — úkon spisu, nepovinný kontakt se směrem (SDD-010).
- **Contact** — kontakt (SDD-011).
- **FileAsset** — soubor spisu XOR úkonu (SDD-012).
- **Comment** — komentář spisu XOR úkonu (SDD-013).
- **Label** — štítek tenantu (SDD-019).
- **LabelAssignment** — štítek na spisu XOR úkonu (SDD-019).

### Společné vlastnosti

- Id je UUIDv7 generované aplikací, takže pořadí vzniku je i pořadím identifikátorů.
- Každá entita nese `Created` a `Updated`; plní je databáze ze svých hodin (SDD-018).
- Tenantové entity nesou `TenantId` a vlastníka `UserId`; kontakt vlastníka nemá (SDD-006).
- Datum spisu a úkonu je kalendářní datum, ne okamžik.
- Výčet je v databázi uložený jménem svého členu, takže stav i směr se v databázi čtou stejně
  jako na drátě.

### Matice mazání

| Entita | Smazání |
| --- | --- |
| Case | kaskáda: úkony, komentáře, soubory, přidělené štítky; podřízené spisy do libovolné hloubky se vším, co k nim patří |
| Act | kaskáda: komentáře, soubory, přidělené štítky |
| Contact | jen ten, na který neodkazuje žádný spis ani úkon (SDD-011) |
| FileAsset | prosté; blob přežívá záznam (SDD-012) |
| Comment | prosté; jen autor (SDD-013) |
| Label | kaskáda: přidělení štítku spisům a úkonům (SDD-019) |

Kaskáda maže záznamy souborů; jejich bajty na disku zůstávají, blob se nikdy nemaže.
Každé smazání se v UI potvrzuje (SDD-004).

### Migrace

Řetěz migrací začíná u `Init` a jen roste; žádná už vydaná migrace se nepřepisuje.

## Rozhodnutí

- Id: `long` sekvence / UUIDv7. Platí UUIDv7 generované aplikací.
- Vazby spisů: symetrická relace / rodičovská hierarchie. Platí rodič.
- Optimistická konkurence: token / bez tokenu. Platí bez tokenu — poslední zápis vyhrává.

## Dopady

—
