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
a se serverem mluví jen přes API, typovaným klientem (SDD-005).

Stav drží PostgreSQL; bajty souborů leží na souborovém systému, databáze nese jejich
metadata (SDD-012). Mimo databázi server stav nedrží: přihlášení nese JWT access token
v paměti prohlížeče a rotující refresh token v `__Host-` cookie (SDD-006).

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
| `Data/EvilCase.Data` | EF Core model, přístup k databázi (SDD-018) |
| `Data/EvilCase.Data.Migrations` | migrace |
| `Common/EvilCase.Auth` | autentizace; uzavřený modul za `IAuthService` |
| `Common/EvilCase.Files` | souborové úložiště; uzavřený modul za `IFileBlobStore` |
| `Tests/EvilCase.Tests` | testy aplikace (SDD-003) |
| `Utils/EvilBrains.*` | sdílené knihovny nezávislé na EvilCase, s vlastními testy |

### Vrstvy

```
Host → Api, App, Business, Auth, Files, Data, Data.Migrations
App → Api.Client → (HTTP) → Api → Business → Data
Api → Auth → Data ← Data.Migrations
Api.Client, Api, Business, Auth → Api.Contract
Api, Business, Auth, Data, Api.Contract → Domain
```

- Šipka je závislost; jiná neexistuje. Host je kompoziční root a skládá zbytek.
- Zakázané směry drží testy vrstvení (SDD-003); pinují část šipek, ne všechny.
- Frontend renderuje a sbírá vstup; rozhoduje server.
- Tvary API — jediný zdroj pravdy v kontrolerech, generovaný klient, jedna sada modelů —
  drží SDD-005.
- Čisté doménové pravidlo žije v `Domain` a testuje se bez databáze (SDD-003).
- `EvilCase.Auth` a `EvilCase.Files` stojí mimo vrstvení; dovnitř vede jen `IAuthService`, resp.
  `IFileBlobStore`.

### Technologie

.NET 10, ASP.NET Core, EF Core nad PostgreSQL, Blazor WebAssembly s TabBlazor nad Tabler CSS,
NUnit. Logování jde přes `EvilBrains.Logging.*` na serveru i ve WebAssembly; Seq je volitelný
a zapíná ho URL z prostředí (SDD-002).

### Konfigurace

Nastavení bez tajemství — vydavatel a publikum tokenu, jeho platnosti, lockout, přepínače
migrace a seedu — jsou v `appsettings.json` a přepisují se proměnnými prostředí s prefixem
`EvilBrains__EvilCase__`. Secrets přicházejí jen z proměnných prostředí; Development je navíc
čte z `src/EvilCase.Host/.env`. Repozitář nenese žádný secret.

## Rozhodnutí

- Tvar: samostatné služby / monolit. Platí monolit; hranice drží projekty a testy vrstvení.
- Frontend: Blazor Server / Blazor WebAssembly. Platí WebAssembly; server je bezstavové API.
- Hosting frontendu: druhý proces / týž proces. Platí týž proces — same-origin drží CSRF
  obranu bez antiforgery tokenu (SDD-006).
- API klient: psaný ručně / generovaný z kontrolerů. Platí generovaný.
- Modely: business modely s mapováním na DTO / kontraktní DTO všude. Platí kontraktní DTO.
- Doménová logika: bohaté entity / služby nad schématem. Platí služby v `Business`
  s čistými pravidly v `Domain`; entity v `Data` jsou jen schéma.

## Dopady

—
