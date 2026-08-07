# SDR-016 — Logování a observabilita

- **Stav:** platí
- **Milníky:** průřez
- **Související SDR:** [003](sdr-003-cislovani.md), [007](sdr-007-soubory.md),
  [012](sdr-012-seed-vzorovych-dat.md)

## Rozsah

Co nové featury logují a co nikdy. Pipeline se nemění.

## Popis

### Platí dál

Logging přes `EvilBrains.Logging.*` (server i WebAssembly), Seq z prostředí, request
logging s allow-listem, health checks — vše podle READMEs pod `src/Utils/` a
`.claude/rules/api.md`.

### Nové featury logují

- Přidělení čísla: přidělené `CaseNumber` / `ActNumber` a id entity (SDR-003).
- Souborové úložiště: zápis a smazání blobu s id, velikostí a výsledkem (SDR-007).
- Seed vzorových dat: začátek, výsledek a počty založených entit (SDR-012).

### Nikdy

Obsah souborů, těla komentářů, obsah reálných spisů. Log nese identifikátory, ne obsah.

## Rozhodnutí

—

## Dopady

Beze změny v `src/Utils/`.
