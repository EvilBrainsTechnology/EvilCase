# SDD-015 — Dashboard

- **Stav:** platí
- **Milníky:** M7
- **Související SDD:** [005](sdd-005-api-konvence.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md)

## Rozsah

Úvodní stránka `/`.

## Popis

Dashboard stojí nad reálnými daty tenantu:

- dlaždice počtů spisů podle stavu, za celý tenant; dlaždice vede na seznam spisů zúžený na svůj
  stav včetně podřízených spisů (SDD-009),
- naposledy změněné spisy, bez ohledu na stav, včetně podřízených spisů: datum spisu, název,
  stav a změna,
- naposledy změněné úkony napříč spisy: datum úkonu, v jedné buňce název se štítky a pod ním
  název spisu, směr, kontakt a změna; řádek vede do detailu úkonu.

Obě seznamové dlaždice ukazují změnu jako poslední sloupec; dlaždice spisů je první, dlaždice
úkonů druhá. Dnešní změnu píšou jako „dnes 21:45“, včerejší jako „včera 9:10“, starší jako
okamžik (SDD-016); celý okamžik nese tooltip.

Naposledy změněné spisy řadí vlastní `Updated` spisu; zápis úkonu, komentáře, souboru nebo verze
souboru ho nemění. Spis, který nikdo neupravil, se řadí a zobrazuje podle svého `Created`. Naposledy
změněné úkony řadí stejně vlastní `Updated` úkonu, nebo `Created` u úkonu, který nikdo
neupravil. Seznamové dlaždice ukazují nejvýše 5 položek.

Dashboard nemá vlastní API; skládá se na klientu z API entit (SDD-005): dlaždice počtů
z počtů spisů `/api/cases/counts`, naposledy změněné spisy z výpisu spisů, naposledy změněné
úkony z výpisu úkonů. Obě seznamové dlaždice jsou týž seznam jako agenda, jen bez hledání,
filtrů a stránkování a se stránkou o pěti položkách.

Žádné lhůty. Tenant bez jediného spisu vede na založení prvního spisu; tenant se spisy a bez
úkonů si dlaždice ponechá a dlaždice úkonů ukáže vlastní prázdný stav.

## Rozhodnutí

- Obsah: konfigurovatelné widgety / pevná sestava výše. Platí pevná sestava.
- Cíl dlaždice počtu: celý seznam spisů / seznam zúžený jejím stavem včetně podřízených spisů.
  Platí zúžený seznam.
- Data: vlastní dashboardový endpoint / skládání z API entit. Platí skládání z API entit.

## Dopady

—
