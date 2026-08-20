# SDD-005 — API konvence

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [006](sdd-006-tenance-a-ucty.md),
  [016](sdd-016-navigace-a-vzhled.md)

## Rozsah

Tvary API a klienta pro nové agendy. Z velké části dnešní stav, který platí dál.

## Popis

### Platí dál

- Kontrolery jsou jediný zdroj pravdy; klient se generuje z jejich zdrojů; DTOs žijí
  v `EvilCase.Api.Contract`. Diagnostiky `EB1xxx` jsou spec (`.claude/rules/api.md`).
- Jeden proces, API pod `/api/**`, same-origin, bez CORS.
- Business služba vrací kontraktní DTO; žádná druhá sada modelů
  (`.claude/rules/business.md`).

### Nové agendy

| Zdroj | Routy |
| --- | --- |
| Spisy | `/api/cases`, `/api/cases/{id}` |
| Úkony | `/api/cases/{caseId}/acts`, `/api/cases/{caseId}/acts/{actId}`; výpis napříč spisy `/api/acts` |
| Kontakty | `/api/contacts`, `/api/contacts/{id}` |
| Soubory | upload a výpis na vlastníku; download `/api/files/{id}/content` |
| Komentáře | na vlastníku, `…/comments`, `…/comments/{id}` |
| Hledání | `/api/search?query=` |

- Id v routách je `Guid`.
- Tenanta dodává `ITenantContext`; endpoint ani dotaz nikdy neberou id tenanta parametrem
  (SDD-006).

## Rozhodnutí

- Úkony v API: ploché `/api/acts` / vnořené pod spis. Platí vnořené pod spis; ploché
  `/api/acts` je jen tenantový výpis pro dashboard (SDD-015).
- Dashboard: vlastní endpoint / skládání z API entit. Platí skládání z API entit; žádný
  dashboardový endpoint není.
- Komentáře a soubory: vlastní ploché zdroje / pod vlastníkem. Platí pod vlastníkem.

## Dopady

`EchoController`, jeho kontrakt a klient zanikají v M1 (SDD-016). Chybové odpovědi drží
SDD-004.
