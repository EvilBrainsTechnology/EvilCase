# SDR-010 — Dashboard

- **Stav:** platí
- **Milníky:** M7
- **Související SDR:** [004](sdr-004-spisy.md), [005](sdr-005-ukony.md),
  [009](sdr-009-vyhledavani.md)

## Rozsah

Úvodní stránka `/`.

## Popis

Dashboard stojí nad reálnými daty tenantu:

- dlaždice počtů spisů podle stavu,
- poslední úkony podle data úkonu, s odkazem do detailu,
- naposledy změněné spisy,
- hledací pole (SDR-009).

Žádné lhůty. Prázdný tenant vede na založení prvního spisu.

## Rozhodnutí

- Dashboard: zaniká ve prospěch `/cases` / zůstává nad reálnými daty. Zůstává.
- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.

## Dopady

Hard-coded `SampleData` dnešní úvodní stránky zaniká v M7.
