# SDD-006 — Tenance a účty

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md), [003](sdd-003-testovani.md),
  [004](sdd-004-validace-a-chyby.md), [005](sdd-005-api-konvence.md),
  [007](sdd-007-domenovy-model.md), [019](sdd-019-stitky.md), [023](sdd-023-mcp-server.md)

## Rozsah

Účty, tenanti, uživatelé, izolace dat mezi tenanty, autentizace a API tokeny. Doménové entity
popisuje SDD-007.

## Popis

### Model

- **Account** — id a název. Zastřešuje N tenantů.
- **Tenant** — id, název, `AccountId`. Hranice izolace dat.
- **User** — tenantová entita, patří právě jednomu tenantu: e-mail, hash hesla, role, lockout
  a `TenantId`.

Mimo tenant stojí jen Account, Tenant, refresh token, API token a jednorázový odkaz na soubor
(SDD-023); každá jiná entita nese
`TenantId`.
Vlastníka `UserId` nese každá kromě kontaktu; kontakt patří tenantu (SDD-011). Obojí plní
zápis, ne volající. Tenanta ani uživatele nelze smazat, dokud drží řádky.

Uvnitř tenantu je záznam vidět bez ohledu na to, kdo ho založil, a změnit i smazat ho smí kdokoli
z tenantu. Výjimkou je komentář: změnit a smazat ho smí jen jeho autor, jinak API odpoví 403
(SDD-013).

Account, Tenant a první administrátor vznikají jen seedem při startu
(`EvilBrains__EvilCase__Auth__Seed__*`, jen do prázdné tabulky uživatelů). Žádné UI pro
správu účtů, žádná registrace.

### Izolace

Data nesmí utéct mezi tenanty; únik je kritická chyba.

- Čtení nikdy nevidí řádek cizího tenantu; tenanta i uživatele dodává kontext požadavku.
  Výjimka je hledání uživatele, které tenanta ještě znát nemůže — přihlášení, obnova tokenu,
  ověření API tokenu, použití jednorázového odkazu a oba seedy.
- Access token nese tenant claim i subject claim.
- Zápis doplní `TenantId` a `UserId` nové entity z kontextu a odmítne řádek, který jmenuje
  cizího tenanta nebo cizího uživatele.
- Mimo požadavek (seed, úloha na pozadí) se tenant a uživatel vstupují jen společně a zápis
  prochází stejnou kontrolou jako požadavek.
- Unikátní indexy tenantových entit jsou kompozitní s `TenantId`; e-mail uživatele je unikátní
  přes celé nasazení a ukládá se oříznutý a malými písmeny, takže přihlášení nerozlišuje
  velikost písmen.
- Konvenční test hlídá, že žádná tenantová entita filtr nepostrádá a že mimo tenant stojí jen
  Account, Tenant, refresh token, API token a jednorázový odkaz na soubor (SDD-003).

### Autentizace

JWT access token i rotující refresh token v HttpOnly `__Host-` cookie (`SameSite=Strict`), takže
prohlížeč je k API přihlášený i odkazem (`<a>`, `<img>`, `<iframe>`); JavaScript token nevidí.
Heslo je uložené jako hash odolný proti hrubé síle. Access token nese tenant claim odvozený
z řádku uživatele při každém vydání, obnovu tokenu včetně. CSRF obranu drží `SameSite=Strict`,
same-origin a kontrola `Sec-Fetch-Site` (případně `Origin`) na zápisech do `/api`: zápis mimo
vlastní origin odpoví 403. Antiforgery token není. Hlavička `Authorization: Bearer` zůstává
přijatá pro klienty mimo prohlížeč; nese access token, nebo API token.

Přihlášení vydrží 30 dní od posledního použití a nejdéle 90 dní od přihlášení. Access token
platí 15 minut a aplikace ho obnovuje sama; obnova z více záložek téhož prohlížeče uživatele
neodhlásí.

Pět po sobě jdoucích neúspěšných přihlášení účet na 15 minut uzamkne; uzamčený účet odpovídá
423 (SDD-004) a úspěšné přihlášení počitadlo vynuluje. Uzamčení blokuje jen přihlášení heslem;
živá přihlášení neukončí a jejich obnova běží dál.

### API tokeny

Integrace a AI agent se přihlašují osobním API tokenem. Token patří jednomu uživateli a jedná
jako on: tenanta, uživatele i roli bere z řádku uživatele při každém požadavku. Token nese název
(nejvýše 64 znaků), oprávnění „Jen čtení“ nebo „Čtení i zápis“ a povinnou platnost 30, 90 nebo
365 dní. Uživatel drží nejvýše 20 neprošlých tokenů; další založení odpoví 400.

Token je `evc_` a 256 náhodných bitů v base64url. Ukáže se jednou, při založení; uloží se jen
jeho hash. Posílá se v hlavičce `Authorization: Bearer` a schéma vybere jeho prefix; token
v hlavičce vyhraje nad cookie přihlášení. Neznámý, zrušený nebo prošlý token odpoví 401. Zrušení
token smaže a platí od dalšího požadavku, nic token necachuje.

- Token jen pro čtení odpoví 403 na každý zápis: POST, PUT, PATCH, DELETE. Na `/mcp` vidí
  a volá jen nástroje pro čtení (SDD-023).
- `/mcp` přijme jen API token; cookie ani access token tam neplatí (SDD-023).
- Na `/api/auth/**` a na správu tokenů odpoví token 403; obojí zůstává přihlášení v prohlížeči.
- Uzamčení účtu token nezastaví, stejně jako živé přihlášení.
- Požadavky s tokenem limit požadavků nemají (SDD-005).
- „Naposledy použit“ se zapíše nejvýše jednou za minutu.
- Logovací scope požadavku s tokenem nese id a název tokenu (SDD-002); jiný záznam o změnách
  agenta není. Token sám se do logu nedostane nikdy.

Tokeny spravuje sekce nastavení API tokeny (SDD-019).

## Rozhodnutí

- Uživatelé v tenantu: více uživatelů sdílí tenant / 1 uživatel = 1 tenant. Platí
  1 uživatel = 1 tenant; sdílení je non-goal.
- Vynucení izolace: jen ruční scope v dotazech / filtr na čtení a kontrola na zápisu. Platí
  filtr a kontrola.
- Vznik účtů: registrace v UI / jen seed. Platí jen seed.
- Access token v prohlížeči: v paměti stránky / v HttpOnly cookie. Platí cookie; soubor se
  otevře i prostým odkazem.
- Plnění `TenantId` a `UserId`: volající / zápis sám. Platí zápis sám, jen na nové řádce.
- Změna a smazání cizího záznamu uvnitř tenantu: jen vlastník / kdokoli z tenantu. Platí kdokoli
  z tenantu, komentář jen jeho autor (SDD-013).
- Uložení API tokenu: pomalý hash jako u hesla / SHA-256. Platí SHA-256: 256 náhodných bitů nejde
  uhodnout, a ověření běží při každém požadavku.
- API token na `/api/auth/**` a na správě tokenů: 401 / 403. Platí 403: token je platný, jen tam
  nesmí.

## Dopady

—
