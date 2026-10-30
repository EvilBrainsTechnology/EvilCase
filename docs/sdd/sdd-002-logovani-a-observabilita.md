# SDD-002 — Logování a observabilita

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [008](sdd-008-cislovani.md), [012](sdd-012-soubory.md),
  [017](sdd-017-seed-vzorovych-dat.md)

## Rozsah

Co se loguje a co nikdy.

## Popis

### Pipeline

Logování jde přes `EvilBrains.Logging.*` na serveru i ve WebAssembly. Seq zapíná URL
z prostředí; bez ní se loguje jen do konzole. Logují se požadavky na `/api/**` a `/mcp` kromě
úspěšného uploadu klientských logů — ten by se sám sebou zalogoval do dalšího uploadu. Mimo ně se
nikde neloguje nic, health checks včetně; výjimka je výjimka nebo odpověď 5xx, ta se loguje vždy.

### Co loguje každá agenda

Každý zápis business vrstvy — založení, změna, smazání — loguje identifikátory dotčených
entit, v každé agendě stejně. Výjimkou je seed vzorových dat, který loguje jen počty.

### Zvlášť loguje

- Založení spisu a úkonu: přidělené `CaseNumber` / `ActNumber` a id entity (SDD-008).
- Souborové úložiště: zápis blobu s cestou a velikostí, smazání s cestou (SDD-012).
- Verze souboru: uložení s číslem verze, id souboru a velikostí, smazání s číslem verze a id
  souboru (SDD-012).
- Archiv souborů: odeslání s id spisu nebo úkonu a počtem položek; přerušené odeslání jako chyba
  (SDD-012).
- Ověření podpisu datové zprávy: id souboru a výsledek; neznámý stav zneplatnění jako varování
  s otiskem certifikátu (SDD-012).
- Ověření podpisů podepsaného souboru: id souboru, id podepsaného souboru a výsledek každého
  podpisu; neznámý stav zneplatnění jako varování s otiskem certifikátu (SDD-012).
- Seed vzorových dat: začátek, výsledek a počty založených entit (SDD-017).

### Nikdy

Obsah souborů, těla komentářů, obsah reálných spisů, adresa jednorázového odkazu na soubor
(SDD-023). Log nese identifikátory, ne obsah.

## Rozhodnutí

—

## Dopady

—
