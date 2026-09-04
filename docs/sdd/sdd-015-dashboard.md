# SDD-015 — Dashboard

- **Stav:** platí
- **Milníky:** M7
- **Související SDD:** [005](sdd-005-api-konvence.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md)

## Rozsah

Úvodní stránka `/`.

## Popis

Dashboard stojí nad reálnými daty tenantu:

- dlaždice počtů spisů podle stavu, za celý tenant,
- poslední úkony podle data úkonu sestupně, shodná data řadí `Created`, s odkazem do detailu,
- naposledy změněné spisy, bez ohledu na stav.

Naposledy změněné spisy řadí vlastní `Updated` spisu; zápis úkonu, komentáře nebo souboru ho
nemění — ta aktivita se ukazuje v dlaždici posledních úkonů. Spis, který nikdo neupravil, se
řadí a zobrazuje podle svého `Created`. Seznamové dlaždice ukazují nejvýše 5 položek.

Dashboard nemá vlastní API; skládá se na klientu z API entit (SDD-005): dlaždice počtů
z počtů spisů `/api/cases/counts`, naposledy změněné spisy z výpisu spisů, poslední úkony
z tenantového výpisu `/api/acts`. Oba výpisy berou nejvýše 100 položek na požadavek.

Žádné lhůty. Tenant bez jediného spisu vede na založení prvního spisu; tenant se spisy a bez
úkonů si dlaždice ponechá a dlaždice úkonů ukáže vlastní prázdný stav.

## Rozhodnutí

- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.
- Data: vlastní dashboardový endpoint / skládání z API entit. Platí skládání z API entit.

## Dopady

—
