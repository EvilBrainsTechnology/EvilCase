# SDD-004 — Validace a chyby

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [005](sdd-005-api-konvence.md), [006](sdd-006-tenance-a-ucty.md)

## Rozsah

Kde se co validuje, tvar chyb API a chování formulářů a potvrzení.

## Popis

### Vrstvy validace

- Kontrakt API nese povinnost, délky a formát každého pole.
- Business vrstva: pravidla nad daty — cyklus v hierarchii, formát a unikátnost čísla, jen
  autor komentáře, odkazovaný kontakt.
- Databáze je poslední pojistka: unikátnost, XOR vlastníka, cizí klíče.

Text se ukládá oříznutý a nepovinné pole, které přijde prázdné, se ukládá jako prázdná hodnota.

### Chyby API

Každá chybová odpověď je Problem Details (RFC 9457).

| Stav | Kdy |
| --- | --- |
| 400 | validace vstupu; chyby po polích v `errors` |
| 401 | bez přihlášení |
| 403 | úprava nebo smazání komentáře, který napsal jiný uživatel |
| 404 | neexistující id v routě — i id z cizího tenantu |
| 409 | konflikt stavu: obsazené číslo, odkazovaný kontakt, cyklus v hierarchii; neexistující id v těle požadavku |
| 413 | upload nad limit velikosti |
| 423 | uzamčený účet |
| 429 | překročený limit požadavků |
| 500 | bez detailů |

Cizí tenant nikdy nevrací 403 — existence cizích dat nesmí uniknout. Uvnitř tenantu je zápis
volný (SDD-006); jediné 403 nese komentář, který smí upravit a smazat jen jeho autor (SDD-013).

Id, které požadavek jmenuje a které neexistuje: v routě 404, v těle 409. Platí pro odkazovaný
kontakt v těle úkonu stejně jako pro chybějící spis nebo úkon v routě.

Obsazené číslo je 409 jen při editaci; založení si číslo přiděluje samo (SDD-008). Ručně
zapsané číslo mimo formát je 400 s chybou u pole.

### Frontend

- Formulář ukazuje chyby polí u polí, chybu požadavku nad formulářem.
- Mimo formulář: 401 po neúspěšném tichém refreshi přesměruje na `/login`, 404 vykreslí
  stav nenalezeno, selhání sítě vykreslí inline — chybu na formuláři, nebo prázdný stav
  na seznamu.
- Destruktivní operace se potvrzuje dialogem; kaskádové smazání jmenuje, co bere s sebou.

## Rozhodnutí

- Cizí tenant: 403 / 404. Platí 404.
- Konflikt čísla: tiché přegenerování / 409. Platí 409 — uživatel řeší kolizi sám.

## Dopady

Platí pro každý endpoint z SDD-005 a každý formulář agend SDD-009 až 013.
