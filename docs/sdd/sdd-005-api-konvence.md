# SDD-005 — API konvence

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [006](sdd-006-tenance-a-ucty.md),
  [016](sdd-016-navigace-a-vzhled.md)

## Rozsah

Tvary API a klienta.

## Popis

### Tvar API

- Kontrolery jsou jediný zdroj pravdy; klient se generuje z jejich zdrojů; DTOs žijí
  v `EvilCase.Api.Contract`.
- Jeden proces, API pod `/api/**`, same-origin, bez CORS. Neznámá cesta pod `/api` je 404
  jako Problem Details, nikdy `index.html` frontendu.
- Business služba vrací kontraktní DTO; žádná druhá sada modelů.
- Endpoint bez výslovné výjimky vyžaduje přihlášení. Anonymní jsou jen přihlášení, obnova
  tokenu, odhlášení, health checks a upload klientských logů.
- Založení odpovídá 201 s hlavičkou `Location` na detail nového záznamu; výjimkou je komentář,
  který odpovídá 204 jako jeho editace a smazání. Editace a smazání odpovídají 204.
- Hodnota výčtu jde po drátě jako název; číslo ani neznámý název se nepřijme (400).
- Výpisy berou nejvýše 100 položek na požadavek; stránkování není.

### Limity požadavků

Limitem prochází jen to, co může anonymní volající zdražit, a partition je adresa volajícího:
přihlášení 5 za minutu, obnova tokenu 60, zbytek `/api/auth/**` 10, upload klientských logů 120.
Vše ostatní je bez limitu, health checks včetně. Odmítnutí je 429 s `Retry-After`.

### Zdroje

| Zdroj | Routy |
| --- | --- |
| Spisy | `/api/cases`, `/api/cases/{id}`; počty podle stavu `/api/cases/counts` |
| Úkony | `/api/cases/{caseId}/acts`, `/api/cases/{caseId}/acts/{actId}`; výpis napříč spisy `/api/acts` |
| Kontakty | `/api/contacts`, `/api/contacts/{id}` |
| Soubory | výpis a smazání na vlastníku; upload na vlastníku a download `/api/files/{id}/content` |
| Komentáře | na vlastníku, `…/comments`, `…/comments/{id}` |

- Id v routách je `Guid`.
- Tenanta i uživatele dodává kontext požadavku; endpoint ani dotaz nikdy neberou id tenanta
  parametrem (SDD-006).
- Upload a download jdou přes ručně psaný klient, ne přes generovaný: generátor neumí
  multipart formulář ani proud bajtů. Výpis a smazání souboru jdou přes generovaný.

## Rozhodnutí

- Úkony v API: ploché `/api/acts` / vnořené pod spis. Platí vnořené pod spis; ploché
  `/api/acts` je jen tenantový výpis pro dashboard (SDD-015).
- Dashboard: vlastní endpoint / skládání z API entit. Platí skládání z API entit; počty spisů
  drží zdroj spisů, žádný dashboardový endpoint není.
- Komentáře a soubory: vlastní ploché zdroje / pod vlastníkem. Platí pod vlastníkem.

## Dopady

Chybové odpovědi drží SDD-004.
