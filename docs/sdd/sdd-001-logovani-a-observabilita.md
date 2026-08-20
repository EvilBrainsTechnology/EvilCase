# SDD-001 — Logování a observabilita

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [007](sdd-007-cislovani.md), [011](sdd-011-soubory.md),
  [016](sdd-016-seed-vzorovych-dat.md)

## Rozsah

Co nové featury logují a co nikdy. Pipeline se nemění.

## Popis

### Platí dál

Logging přes `EvilBrains.Logging.*` (server i WebAssembly), Seq z prostředí, request
logging s allow-listem, health checks — vše podle READMEs pod `src/Utils/` a
`.claude/rules/api.md`.

### Nové featury logují

- Přidělení čísla: přidělené `CaseNumber` / `ActNumber` a id entity (SDD-007).
- Souborové úložiště: zápis a smazání blobu s id, velikostí a výsledkem (SDD-011).
- Seed vzorových dat: začátek, výsledek a počty založených entit (SDD-016).

### Nikdy

Obsah souborů, těla komentářů, obsah reálných spisů. Log nese identifikátory, ne obsah.

## Rozhodnutí

—

## Dopady

Beze změny v `src/Utils/`.
