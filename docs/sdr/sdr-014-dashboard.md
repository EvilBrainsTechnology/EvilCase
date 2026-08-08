# SDR-014 — Dashboard

- **Stav:** platí
- **Milníky:** M7
- **Související SDR:** [008](sdr-008-spisy.md), [009](sdr-009-ukony.md),
  [013](sdr-013-vyhledavani.md)

## Rozsah

Úvodní stránka `/`.

## Popis

Dashboard stojí nad reálnými daty tenantu:

- dlaždice počtů spisů podle stavu,
- poslední úkony podle data úkonu, s odkazem do detailu,
- naposledy změněné spisy,
- hledací pole (SDR-013).

Žádné lhůty. Prázdný tenant vede na založení prvního spisu.

## Rozhodnutí

- Dashboard: zaniká ve prospěch `/cases` / zůstává nad reálnými daty. Zůstává.
- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.

## Dopady

Hard-coded `SampleData` dnešní úvodní stránky zaniká v M7.
