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
- naposledy změněné spisy, bez ohledu na stav, ve výchozím rozsahu výpisu spisů (SDD-009),
- naposledy změněné úkony napříč spisy, s odkazem do detailu a jménem spisu, kterému úkon patří.

Obě seznamové dlaždice ukazují změnu jako první sloupec; dlaždice spisů je první, dlaždice
úkonů druhá.

Naposledy změněné spisy řadí vlastní `Updated` spisu; zápis úkonu, komentáře nebo souboru ho
nemění. Spis, který nikdo neupravil, se řadí a zobrazuje podle svého `Created`. Naposledy
změněné úkony řadí stejně vlastní `Updated` úkonu, nebo `Created` u úkonu, který nikdo
neupravil. Seznamové dlaždice ukazují nejvýše 5 položek.

Dashboard nemá vlastní API; skládá se na klientu z API entit (SDD-005): dlaždice počtů
z počtů spisů `/api/cases/counts`, naposledy změněné spisy z výpisu spisů, poslední úkony
z tenantového výpisu `/api/acts`.

Žádné lhůty. Tenant bez jediného spisu vede na založení prvního spisu; tenant se spisy a bez
úkonů si dlaždice ponechá a dlaždice úkonů ukáže vlastní prázdný stav.

## Rozhodnutí

- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.
- Data: vlastní dashboardový endpoint / skládání z API entit. Platí skládání z API entit.

## Dopady

—
