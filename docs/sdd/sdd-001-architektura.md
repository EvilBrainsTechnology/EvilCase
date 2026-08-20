# SDD-001 — Architektura

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md),
  [003](sdd-003-testovani.md), [005](sdd-005-api-konvence.md),
  [006](sdd-006-tenance-a-ucty.md), [012](sdd-012-soubory.md), [018](sdd-018-prace-s-databazi.md)

## Rozsah

Celkový tvar aplikace: běhový model, projekty, vrstvy a povolené závislosti, technologie,
konfigurace a nasazení. Tvary API drží SDD-005, izolace tenantů SDD-006, souborové úložiště
SDD-012. `.claude/rules/` nese z tohoto návrhu invarianty pro práci s kódem.

## Popis

### Běhový model

Jedna aplikace, jeden proces. `EvilCase.Host` obsluhuje `/api/**` jako REST API a na každé
jiné cestě vrací `index.html` frontendu. Frontend je Blazor WebAssembly: běží v prohlížeči
a se serverem mluví jen přes API, typovaným klientem. Vše je same-origin; CORS neexistuje.

Stav drží PostgreSQL; bajty souborů leží na souborovém systému, databáze nese jejich
metadata (SDD-012). Mimo databázi server stav nedrží: přihlášení nese JWT access token
v paměti prohlížeče a rotující refresh token v `__Host-` cookie, celé za `IAuthService`
(`.claude/rules/auth.md`).

Nasazení je jeden kontejnerový image v compose stacku. Databáze do stacku nepatří;
connection string míří na existující PostgreSQL (`deploy/README.md`).

### Projekty

Kód žije v `src/`, řešení `EvilCase.slnx`:

| Projekt | Odpovědnost |
| --- | --- |
| `EvilCase.Host` | kompoziční root; jediný spustitelný projekt |
| `Api/EvilCase.Api` | kontrolery a HTTP pipeline, jako knihovna |
| `Api/EvilCase.Api.Contract` | DTO sdílená serverem i klientem |
| `Api/EvilCase.Api.Client` | typovaný klient generovaný ze zdrojů kontrolerů |
| `App/EvilCase.App` | Blazor WebAssembly frontend |
| `Business/EvilCase.Business` | business logika |
| `Business/EvilCase.Domain` | doménové jádro bez závislostí |
| `Data/EvilCase.Data` | EF Core model a přístup k databázi (SDD-018) |
| `Data/EvilCase.Data.Migrations` | migrace |
| `Common/EvilCase.Auth` | autentizace; uzavřený modul za `IAuthService` |
| `Tests/EvilCase.Tests` | testy aplikace (SDD-003) |
| `Utils/EvilBrains.*` | sdílené knihovny nezávislé na EvilCase |

### Vrstvy

```
Host → Api, App, Auth, Data, Data.Migrations
App → Api.Client → (HTTP) → Api → Business → Data
Api → Auth → Data ← Data.Migrations
Api.Client, Api, Business, Auth → Api.Contract
Api, Business, Auth, Data, Api.Contract → Domain
```

- Šipka je závislost; jiná neexistuje. Host je kompoziční root a skládá zbytek.
- Zakázané směry a nosné šipky drží `Tests/Architecture/LayerTests` (SDD-003).
- Frontend renderuje a sbírá vstup; rozhoduje server.
- Kontrolery jsou jediný zdroj pravdy API: kontrakt žije v `Api.Contract`, klient se
  generuje ze zdrojů kontrolerů a shodu vynucují diagnostiky `EB1xxx`.
- Jedna sada modelů: business služba vrací kontraktní DTO a projekce dotazu do něj míří
  přímo. Žádná mapovací vrstva.
- Čisté doménové pravidlo je statická třída v `Domain` bez `DbContext`, testovaná bez
  databáze (SDD-003).
- `EvilCase.Auth` stojí mimo vrstvení; dovnitř vede jen `IAuthService`.

### Technologie

.NET 10 — SDK pinuje `src/global.json`. ASP.NET Core, EF Core nad PostgreSQL, Blazor
WebAssembly s TabBlazor nad Tabler CSS, NUnit. Logování jde přes `EvilBrains.Logging.*` na
serveru i ve WebAssembly; Seq je volitelný a zapíná ho URL z prostředí (SDD-002).

### Konfigurace

Konfigurace a secrets přicházejí z proměnných prostředí v každém prostředí; klíče nesou
prefix `EvilBrains__EvilCase__`. Development navíc čte `src/EvilCase.Host/.env`. Repozitář
nenese žádný secret.

## Rozhodnutí

- Tvar: samostatné služby / monolit. Platí monolit; hranice drží projekty a `LayerTests`.
- Frontend: Blazor Server / Blazor WebAssembly. Platí WebAssembly — server nedrží okruhy
  a zůstává bezstavové API.
- Hosting frontendu: druhý proces / týž proces. Platí týž proces — same-origin drží CSRF
  obranu bez antiforgery tokenu (`.claude/rules/auth.md`).
- API klient: psaný ručně / generovaný z kontrolerů. Platí generovaný.
- Modely: business modely s mapováním na DTO / kontraktní DTO všude. Platí kontraktní DTO.
- Doménová logika: bohaté entity / služby nad schématem. Platí služby v `Business`
  s čistými pravidly v `Domain`; entity v `Data` jsou jen schéma.

## Dopady

Beze změny v kódu. `.claude/rules/api.md`, `app.md`, `business.md`, `data.md` a `auth.md`
nesou z tohoto návrhu invarianty; změna, která návrh falzifikuje, mění SDD i pravidlo ve
stejném commitu.
