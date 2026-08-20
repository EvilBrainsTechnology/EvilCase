# SDD-017 — Seed vzorových dat

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md), [003](sdd-003-testovani.md),
  [007](sdd-007-domenovy-model.md), [012](sdd-012-soubory.md)

## Rozsah

Seed vzorových dat pro vývoj a ruční ověření aplikace. Seed účtů a administrátora patří
SDD-006.

## Popis

- Flag `EvilBrains__EvilCase__Database__SeedSampleData`, default `false`. Seed běží při
  startu, v jakémkoli prostředí, jen když tenant nemá žádný spis.
- Data jsou pseudonymizovaný případ překročení rychlosti z `test-data/case-01-speeding.md`,
  přemapovaný na nový model: pod-spisy jako podřízené spisy hlavního spisu, strany jako
  kontakty, externí značky s vazbou na kontakt, který je přidělil, úkony se směrem,
  odesílatelem a příjemcem, komentáře.
- Soubory jsou jednoduché syntetické TXT generované při seedu a zapsané úložištěm SDD-012,
  jehož jádro od M2 existuje. Žádná PDF, žádné binárky v repozitáři.
- Seeder je C# kód s daty zapsanými v kódu; markdown se neparsuje.
- Pull request, který mění model, mění seeder ve stejném pull requestu.

## Rozhodnutí

- Zdroj dat: parsování `test-data/*.md` / data přepsaná do C#. Platí C#.
- Soubory: generovaná PDF / prosté TXT. Platí TXT.
- Rozsah: celý případ včetně pod-spisů / jen hlavní spis. Platí celý případ — obrazovky se
  ověřují nad rozsahem reálného spisu.

## Dopady

Smoke test seedu (SDD-003). Seed loguje začátek, výsledek a počty (SDD-002). Dnešní záměr
generovat SQL z `test-data/` zaniká; `test-data/README.md` se opraví, až se seeder napíše.
