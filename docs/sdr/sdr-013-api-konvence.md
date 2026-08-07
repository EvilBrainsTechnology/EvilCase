# SDR-013 — API konvence

- **Stav:** platí
- **Milníky:** průřez
- **Související SDR:** [001](sdr-001-tenance-a-ucty.md), [011](sdr-011-navigace-a-vzhled.md),
  [014](sdr-014-validace-a-chyby.md)

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
| Úkony | `/api/cases/{caseId}/acts`, `/api/cases/{caseId}/acts/{actId}` |
| Kontakty | `/api/contacts`, `/api/contacts/{id}` |
| Soubory | upload a výpis na vlastníku; download `/api/files/{id}/content` |
| Komentáře | na vlastníku, `…/comments`, `…/comments/{id}` |
| Hledání | `/api/search?query=` |
| Dashboard | `/api/dashboard` |

- Id v routách je `Guid`.
- Tenanta dodává `ITenantContext`; endpoint ani dotaz nikdy neberou id tenanta parametrem
  (SDR-001).

## Rozhodnutí

- Úkony v API: ploché `/api/acts` / vnořené pod spis. Platí vnořené — kopírují URL aplikace.
- Komentáře a soubory: vlastní ploché zdroje / pod vlastníkem. Platí pod vlastníkem.

## Dopady

`EchoController`, jeho kontrakt a klient zanikají v M1 (SDR-011). Chybové odpovědi drží
SDR-014.
