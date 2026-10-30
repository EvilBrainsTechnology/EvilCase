# SDD-005 — API konvence

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [006](sdd-006-tenance-a-ucty.md),
  [016](sdd-016-navigace-a-vzhled.md), [023](sdd-023-mcp-server.md)

## Rozsah

Tvary API a klienta.

## Popis

### Tvar API

- Kontrolery jsou jediný zdroj pravdy; klient se generuje z jejich zdrojů; DTOs žijí
  v `EvilCase.Api.Contract`.
- Jeden proces, API pod `/api/**`, MCP server na `/mcp` (SDD-023), same-origin, bez CORS.
  Neznámá cesta pod `/api` je 404 jako Problem Details, nikdy `index.html` frontendu.
- Business služba vrací kontraktní DTO; žádná druhá sada modelů.
- Endpoint bez výslovné výjimky vyžaduje přihlášení. Anonymní jsou přihlášení, obnova tokenu,
  odhlášení, health checks, upload klientských logů, upload a download jednorázovým odkazem
  `/api/file-links` (přihlášením je odkaz sám, SDD-023), 404 pod `/api`, v každém prostředí
  `/openapi/v1.json` a v Development `/scalar`.
- Přihlášení nese cookie, nebo API token v hlavičce `Authorization: Bearer` (SDD-006). OpenAPI
  dokument deklaruje schéma Bearer.
- Založení odpovídá 201 s hlavičkou `Location` na detail nového záznamu; u souboru a verze míří na
  stažení obsahu, detail souboru není. Založení API tokenu odpovídá 201 bez `Location`, protože
  token nemá detail. Upload, který nic neuloží, protože se obsah shoduje s aktuální verzí,
  odpovídá 200. Komentář odpovídá 204 i na založení. Editace a smazání odpovídají 204.
- V těle jde hodnota výčtu jako název; číslo ani neznámý název se nepřijme (400). V query
  stringu ji váže framework, který číslo bere.
- Každý výpis bere stránku, směr řazení, svůj řadicí klíč a hledaný text; k tomu zúžení své
  agendy. Rozsah dat, kontakt a štítky berou výpisy spisů a úkonů; výpis kontaktů je nezná,
  protože kontakt nenese štítek, kontakt ani datum. Výpis nepřijímá parametr, který neuplatní.
- Stránka je povinná: `Skip` od nuly, `Take` od 1 do 100. Výpis bez `Take` je 400. Odpověď nese
  vedle položek celkový počet všeho, co filtr nechal, bez ohledu na stránku.
- Filtr štítků bere více štítků; položka musí nést všechny.
- Výjimka je hledání napříč entitami: skupina nese nejvýše 3 položky a celkový počet.

### Limity požadavků

Limitem prochází celé `/api/auth/**`, přihlášené endpointy včetně, a upload klientských logů;
partition je adresa volajícího: přihlášení 5 za minutu, obnova tokenu 60, zbytek `/api/auth/**` 10,
upload klientských logů 120.
Vše ostatní je bez limitu, health checks a požadavky s API tokenem včetně. Odmítnutí je 429
s `Retry-After`.

### Zdroje

| Zdroj | Routy |
| --- | --- |
| Spisy | `/api/cases`, `/api/cases/{id}`; počty podle stavu `/api/cases/counts` |
| Úkony | výpis a založení `/api/acts` (spis v těle), detail `/api/acts/{actId}` |
| Kontakty | `/api/contacts`, `/api/contacts/{id}` |
| Soubory | výpis, přejmenování a smazání na vlastníku; upload na vlastníku, archiv souborů vlastníka `…/files/archive` s volitelným `?includeVersions=true`, download `/api/files/{id}/content`, verze `/api/files/{id}/versions` (výpis, upload nové verze) a `/api/files/{id}/versions/{number}` (smazání), ověření podpisu datové zprávy `/api/files/{id}/signature` a podepisující podepsaného souboru `/api/files/{id}/signers` a jejich ověření `/api/files/{id}/signers/verification`; obsah, podpis i podepisující a jejich ověření berou volitelné `?version=` |
| Komentáře | na vlastníku, `…/comments`, `…/comments/{id}` |
| Hledání | `/api/search?q=`, viz [SDD-021](sdd-021-vyhledavani.md) |
| Štítky | `/api/labels`, `/api/labels/{id}`; přidělení na vlastníku `…/labels` |
| API tokeny | `/api/api-tokens`, zrušení `/api/api-tokens/{id}`; jen přihlášení v prohlížeči (SDD-006) |
| Jednorázové odkazy | `/api/file-links?secret=`: GET stáhne obsah, PUT s `&fileName=` nahraje surové bajty; bez přihlášení (SDD-023) |
| MCP | `/mcp`, nástroje nad všemi zdroji výše kromě API tokenů a archivu souborů; jen API token (SDD-023) |

- Id v routách je `Guid`.
- Tenanta i uživatele dodává kontext požadavku; endpoint ani dotaz nikdy neberou id tenanta
  parametrem (SDD-006).
- Upload a download jdou přes ručně psaný klient, ne přes generovaný: generátor neumí
  multipart formulář ani proud bajtů. Upload nové verze také. Výpis, přejmenování a smazání souboru
  a výpis a smazání verzí jdou přes generovaný. Jednorázové odkazy generovaný klient nemá; vydávají
  je nástroje MCP.
- Upload jednorázovým odkazem drží pravidla uploadu (SDD-012) a odpovídá jako upload na vlastníku.
  Download jednorázovým odkazem odpovídá jako download obsahu a nese `Cache-Control: no-store`.
  Neznámý, použitý nebo prošlý odkaz je 404.

## Rozhodnutí

- Úkony v API: ploché `/api/acts` / vnořené pod spis. Platí ploché: úkon se pozná jen podle
  svého id, spis nese tělo založení a filtr výpisu.
- Stránkování: bez něj / `Skip` a `Take` na každém výpisu. Platí `Skip` a `Take`, povinná
  velikost stránky a celkový počet v odpovědi.
- Dashboard: vlastní endpoint / skládání z API entit. Platí skládání z API entit; počty spisů
  drží zdroj spisů, žádný dashboardový endpoint není.
- Komentáře a soubory: vlastní ploché zdroje / pod vlastníkem. Platí pod vlastníkem.

## Dopady

Chybové odpovědi drží SDD-004.
