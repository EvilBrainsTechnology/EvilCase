# SDD-006 — Tenance a účty

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [003](sdd-003-testovani.md), [004](sdd-004-validace-a-chyby.md),
  [005](sdd-005-api-konvence.md), [007](sdd-007-domenovy-model.md)

## Rozsah

Účty, tenanti, uživatelé, izolace dat mezi tenanty a autentizace. Doménové entity popisuje
SDD-007.

## Popis

### Model

- **Account** — id a název. Zastřešuje N tenantů.
- **Tenant** — id, název, `AccountId`. Hranice izolace dat.
- **User** — tenantová entita, patří právě jednomu tenantu: e-mail, hash hesla, role, lockout
  a `TenantId`.

Mimo tenant stojí jen Account, Tenant a refresh token; každá jiná entita nese `TenantId`.
Vlastníka `UserId` nese každá kromě kontaktu; kontakt patří tenantu (SDD-011). Obojí plní
zápis, ne volající. Tenanta ani uživatele nelze smazat, dokud drží řádky.

Uvnitř tenantu je záznam vidět a lze ho změnit i smazat bez ohledu na to, kdo ho založil;
`UserId` jen říká, kdo to byl. Výjimkou je komentář, ten smí upravit a smazat jen jeho autor
(SDD-013).

Account, Tenant a první administrátor vznikají jen seedem při startu
(`EvilBrains__EvilCase__Auth__Seed__*`, jen do prázdné tabulky uživatelů). Žádné UI pro
správu účtů, žádná registrace.

### Izolace

Data nesmí utéct mezi tenanty; únik je kritická chyba.

- Čtení nikdy nevidí řádek cizího tenantu; tenanta i uživatele dodává kontext požadavku.
  Výjimka je hledání uživatele, které tenanta ještě znát nemůže — přihlášení, obnova tokenu
  a oba seedy.
- Access token nese tenant claim i subject claim.
- Zápis doplní `TenantId` a `UserId` nové entity z kontextu a odmítne řádek, který jmenuje
  cizího tenanta nebo cizího uživatele.
- Mimo požadavek (seed, úloha na pozadí) se tenant a uživatel vstupují jen společně a zápis
  prochází stejnou kontrolou jako požadavek.
- Unikátní indexy tenantových entit jsou kompozitní s `TenantId`; e-mail uživatele je unikátní
  přes celé nasazení a ukládá se oříznutý a malými písmeny, takže přihlášení nerozlišuje
  velikost písmen.
- Konvenční test hlídá, že žádná tenantová entita filtr nepostrádá a že mimo tenant stojí jen
  Account, Tenant a refresh token (SDD-003).

### Autentizace

JWT access token v paměti prohlížeče, rotující refresh token v `__Host-` cookie, heslo uložené
jako hash odolný proti hrubé síle. Access token nese tenant claim odvozený z řádku uživatele
při každém vydání, obnovu tokenu včetně. CSRF obranu drží `SameSite=Strict` a same-origin;
antiforgery token není.

Pět po sobě jdoucích neúspěšných přihlášení účet na 15 minut uzamkne; uzamčený účet odpovídá
423 (SDD-004) a úspěšné přihlášení počitadlo vynuluje.

## Rozhodnutí

- Uživatelé v tenantu: více uživatelů sdílí tenant / 1 uživatel = 1 tenant. Platí
  1 uživatel = 1 tenant; sdílení je non-goal.
- Vynucení izolace: jen ruční scope v dotazech / filtr na čtení a kontrola na zápisu. Platí
  filtr a kontrola.
- Vznik účtů: registrace v UI / jen seed. Platí jen seed.
- Plnění `TenantId` a `UserId`: volající / zápis sám. Platí zápis sám, jen na nové řádce.
- Zápis cizí řádky uvnitř tenantu: povolený / odmítnutý. Platí povolený; jedinou výjimkou je
  komentář (SDD-013).

## Dopady

—
