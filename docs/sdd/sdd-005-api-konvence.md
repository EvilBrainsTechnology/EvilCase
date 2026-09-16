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
- Endpoint bez výslovné výjimky vyžaduje přihlášení. Anonymní jsou přihlášení, obnova tokenu,
  odhlášení, health checks, upload klientských logů, 404 pod `/api` a v Development `/openapi`
  a `/scalar`.
- Založení odpovídá 201 s hlavičkou `Location` na detail nového záznamu; u souboru míří na
  stažení obsahu, detail souboru není. Komentář odpovídá 204 i na založení. Editace a smazání
  odpovídají 204.
- V těle jde hodnota výčtu jako název; číslo ani neznámý název se nepřijme (400). V query
  stringu ji váže framework, který číslo bere.
- Každý výpis bere tytéž parametry: hledaný text, rozsah dat, kontakt, štítky, směr řazení
  a stránku; k tomu vlastní řadicí klíč a zúžení své agendy. Výpis kontaktů z nich uplatňuje jen
  hledaný text a typ kontaktu — kontakt nenese štítek, kontakt ani datum.
- Stránka je povinná: `Skip` od nuly, `Take` od 1 do 100. Výpis bez `Take` je 400. Odpověď nese
  vedle položek celkový počet všeho, co filtr nechal, bez ohledu na stránku.
- Filtr štítků bere více štítků; položka musí nést všechny.

### Limity požadavků

Limitem prochází celé `/api/auth/**`, přihlášené endpointy včetně, a upload klientských logů;
partition je adresa volajícího: přihlášení 5 za minutu, obnova tokenu 60, zbytek `/api/auth/**` 10,
upload klientských logů 120.
Vše ostatní je bez limitu, health checks včetně. Odmítnutí je 429 s `Retry-After`.

### Zdroje

| Zdroj | Routy |
| --- | --- |
| Spisy | `/api/cases`, `/api/cases/{id}`; počty podle stavu `/api/cases/counts` |
| Úkony | výpis `/api/acts`; založení `/api/cases/{caseId}/acts`, detail `/api/cases/{caseId}/acts/{actId}` |
| Kontakty | `/api/contacts`, `/api/contacts/{id}` |
| Soubory | výpis a smazání na vlastníku; upload na vlastníku a download `/api/files/{id}/content` |
| Komentáře | na vlastníku, `…/comments`, `…/comments/{id}` |
| Štítky | `/api/labels`, `/api/labels/{id}`; přidělení na vlastníku `…/labels` |

- Id v routách je `Guid`.
- Tenanta i uživatele dodává kontext požadavku; endpoint ani dotaz nikdy neberou id tenanta
  parametrem (SDD-006).
- Upload a download jdou přes ručně psaný klient, ne přes generovaný: generátor neumí
  multipart formulář ani proud bajtů. Výpis a smazání souboru jdou přes generovaný.

## Rozhodnutí

- Úkony v API: ploché `/api/acts` / vnořené pod spis. Platí ploché: `/api/acts` je jediný výpis
  úkonů a zužuje se spisem. Vnořené zůstává založení a detail.
- Stránkování: bez něj / `Skip` a `Take` na každém výpisu. Platí `Skip` a `Take`, povinná
  velikost stránky a celkový počet v odpovědi.
- Dashboard: vlastní endpoint / skládání z API entit. Platí skládání z API entit; počty spisů
  drží zdroj spisů, žádný dashboardový endpoint není.
- Komentáře a soubory: vlastní ploché zdroje / pod vlastníkem. Platí pod vlastníkem.

## Dopady

Chybové odpovědi drží SDD-004.
