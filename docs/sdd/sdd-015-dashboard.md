# SDD-015 — Dashboard

- **Stav:** platí
- **Milníky:** M7
- **Související SDD:** [005](sdd-005-api-konvence.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md), [014](sdd-014-vyhledavani.md)

## Rozsah

Úvodní stránka `/`.

## Popis

Dashboard stojí nad reálnými daty tenantu:

- dlaždice počtů spisů podle stavu,
- poslední úkony podle data úkonu, s odkazem do detailu,
- naposledy změněné spisy,
- hledací pole (SDD-014).

Naposledy změněné spisy řadí vlastní `Updated` spisu; zápis úkonu, komentáře nebo souboru ho
nemění — ta aktivita se ukazuje v dlaždici posledních úkonů. Seznamové dlaždice ukazují
nejvýše 5 položek.

Dashboard nemá vlastní API; skládá se na klientu z API entit (SDD-005): dlaždice počtů
a naposledy změněné spisy z výpisu spisů, poslední úkony z tenantového výpisu `/api/acts`.

Žádné lhůty. Prázdný tenant vede na založení prvního spisu.

## Rozhodnutí

- Dashboard: zaniká ve prospěch `/cases` / zůstává nad reálnými daty. Zůstává.
- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.
- Data: vlastní dashboardový endpoint / skládání z API entit. Platí skládání z API entit.

## Dopady

Hard-coded `SampleData` dnešní úvodní stránky zaniká v M7. Tenantový výpis `/api/acts`
(SDD-005) vzniká v M7 s dashboardem — nic dřívějšího ho nepotřebuje.
