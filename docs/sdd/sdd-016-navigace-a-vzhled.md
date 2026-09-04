# SDD-016 — Navigace a vzhled

- **Stav:** platí
- **Milníky:** M1
- **Související SDD:** [008](sdd-008-cislovani.md), [015](sdd-015-dashboard.md)

## Rozsah

Routy aplikace, menu a vzhled. Obsah stránek drží SDD jednotlivých agend.

## Popis

### Routy

URL nesou UUID entit, nikdy jejich čísla:

| Routa | Obsah |
| --- | --- |
| `/` | dashboard (SDD-015) |
| `/cases` | seznam spisů |
| `/cases/new` | založení spisu |
| `/cases/{id}` | detail spisu |
| `/cases/{id}/edit` | editace spisu |
| `/cases/{id}/act/new` | založení úkonu |
| `/cases/{id}/act/{actId}` | detail úkonu |
| `/cases/{id}/act/{actId}/edit` | editace úkonu |
| `/contacts` | kontakty |
| `/contacts/{id}` | detail kontaktu |
| `/login` | přihlášení |

Neznámá routa vykreslí stav nenalezeno uvnitř aplikace, neznámé id v routě prázdný stav
s tím, který záznam chybí.

### Menu a vzhled

Menu nese Přehled, Spisy a Kontakty a zvýrazňuje položku i na podřízených routách. Vzhled je
Tabler a TabBlazor. Desktop je primární, každá denně používaná obrazovka je plnohodnotná i na
mobilu a seznam se na mobilu nikdy neposouvá vodorovně. Datum se všude píše `d. M. yyyy`,
okamžik `d. M. yyyy H:mm` v časovém pásmu prohlížeče.

Každý seznam má prázdný stav; kde jde záznam založit, nese výzvu k založení.

### Přihlášení

Každá stránka kromě `/login` vyžaduje přihlášení; nepřihlášeného aplikace přesměruje na
`/login` a po přihlášení ho vrátí tam, odkud přišel. Cíl mimo aplikaci se ignoruje a vede na `/`.

## Rozhodnutí

- Identifikátor v URL: `CaseNumber` / UUID. Platí UUID.

## Dopady

—
