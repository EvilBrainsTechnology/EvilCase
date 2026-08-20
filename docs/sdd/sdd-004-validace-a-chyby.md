# SDD-004 — Validace a chyby

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [005](sdd-005-api-konvence.md), [006](sdd-006-tenance-a-ucty.md)

## Rozsah

Kde se co validuje, tvar chyb API a chování formulářů a potvrzení.

## Popis

### Vrstvy validace

- Anotace na DTO: povinnost, délky, formát.
- Business vrstva: pravidla nad daty — cyklus v hierarchii, formát a unikátnost čísla, jen
  autor komentáře, odkazovaný kontakt.
- Databázové constraints jsou poslední pojistka: unikátní indexy, check constraints, cizí
  klíče.

### Chyby API

Každá chybová odpověď je Problem Details (RFC 9457).

| Stav | Kdy |
| --- | --- |
| 400 | validace vstupu; chyby po polích v `errors` |
| 401 | bez přihlášení |
| 404 | neexistující id — i id z cizího tenantu |
| 409 | konflikt stavu: obsazené číslo, odkazovaný kontakt, cyklus v hierarchii |
| 500 | bez detailů |

Cizí tenant nikdy nevrací 403 — existence cizích dat nesmí uniknout.

### Frontend

- Formulář ukazuje chyby polí u polí, chybu požadavku nad formulářem.
- Mimo formulář: 401 po neúspěšném tichém refreshi přesměruje na `/login`, 404 vykreslí
  stav nenalezeno, selhání sítě ukáže toast.
- Destruktivní operace se potvrzuje dialogem; kaskádové smazání jmenuje, co bere s sebou.

## Rozhodnutí

- Cizí tenant: 403 / 404. Platí 404.
- Konflikt čísla: tiché přegenerování / 409. Platí 409 — uživatel řeší kolizi sám.

## Dopady

Platí pro každý endpoint z SDD-005 a každý formulář agend SDD-009 až 013.
