# SDD-003 — Testování

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [006](sdd-006-tenance-a-ucty.md), [008](sdd-008-cislovani.md),
  [012](sdd-012-soubory.md), [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Vrstvy testů a povinné testy.

## Popis

### Vrstvy testů

- NUnit v `Tests/EvilCase.Tests`; změna chování nese test (`.claude/rules/github.md`).
- Testy vrstvení pinují povolené závislosti mezi projekty (SDD-001).
- Modelové testy nad modelem EF: fixture na každou doménovou entitu a konvenční testy nad
  celým modelem. Check constraints se čtou z návrhového modelu, běhový je zahazuje.
- Hosting testy nad reálnou pipeline: autentizace, hlavičky, rate limiting, routing.
- Čistá doménová logika se testuje bez databáze.

### Povinné testy

- Konvenční test izolace tenantů: každá tenantová entita má query filter a `TenantId`
  v unikátních indexech; uživatel je výjimka, jeho e-mail je unikátní přes celé nasazení
  (SDD-006).
- Testy číslování: formát, pořadí per den, přetečení, zpětné datování, ruční přepis, souběh
  (SDD-008).
- Souborové úložiště na temp adresáři: zápis, atomicita, smazání blobu (SDD-012).
- Smoke test seedu: seed proběhne a založí spis se stromem (SDD-017).
- Testy razítek nad reálnou PostgreSQL: hodnoty, které si zápis čte zpět, změna, která `Created`
  nechá být, zápis mimo EF a pokrytí každé mapované tabulky triggerem; bez serveru selžou s tím,
  co chybí, nepřeskakují se (SDD-018).

## Rozhodnutí

- Testy úložiště: mock souborového systému / temp adresář. Platí temp adresář.
- Testy razítek: fake kontext / reálná databáze. Platí reálná databáze — razítko píší hodiny
  databáze, ne aplikace.
- Test doubles: ruční třídy / mockovací knihovna. Platí NSubstitute pro stub, který jen zaznamená
  argumenty a vrátí připravenou hodnotu; stavový fake si píše sám.

## Dopady

—
