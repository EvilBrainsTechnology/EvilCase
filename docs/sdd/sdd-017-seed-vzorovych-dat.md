# SDD-017 — Seed vzorových dat

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md), [003](sdd-003-testovani.md),
  [007](sdd-007-domenovy-model.md), [012](sdd-012-soubory.md)

## Rozsah

Seed vzorových dat pro vývoj a ruční ověření aplikace. Seed účtů a administrátora patří
SDD-006.

## Popis

- Flag `EvilBrains__EvilCase__Database__SeedSampleData`, default `false`; Development ho má
  zapnutý. Seed běží při startu, v jakémkoli prostředí, po seedu administrátora a jen když
  tenant nemá žádný spis. Bez jediného uživatele se přeskočí; jinak plní tenant nejstaršího
  uživatele. Celý seed je jedna transakce.
- Data jsou pseudonymizovaný případ překročení rychlosti z `test-data/case-01-speeding.md`,
  přepsaný do modelu SDD-007: pod-spisy jako podřízené spisy, z nichž jeden visí o úroveň
  hlouběji pod jiným pod-spisem, strany jako kontakty, kontakt spisu i úkonu, externí značka
  spisu a externí čísla jednací úkonů, úkony se směrem, komentáře.
- Každý pod-spis nese dva syntetické úkony; počty úkonů, které drží zdroj, seed nepřebírá.
- Soubory jsou jednoduché syntetické TXT generované při seedu a zapsané úložištěm SDD-012.
  Hlavní spis i každý úkon dostane jeden, jmenovaný podle svého čísla s lomítky nahrazenými
  pomlčkami; poslední úkon hlavního spisu dostane druhý, s přílohami. Žádná PDF, žádné binárky
  v repozitáři.
- Zdroj pravdy seedu je kód; markdown je předloha a neparsuje se.

## Rozhodnutí

- Zdroj dat: parsování `test-data/*.md` / data přepsaná do C#. Platí C#.
- Soubory: generovaná PDF / prosté TXT. Platí TXT.
- Rozsah: celý případ včetně pod-spisů / jen hlavní spis. Platí celý případ — obrazovky se
  ověřují nad rozsahem reálného spisu.

## Dopady

Smoke test seedu (SDD-003). Seed loguje začátek, výsledek a počty (SDD-002).
