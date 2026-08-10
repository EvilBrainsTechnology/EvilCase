# SDR-015 — Navigace a vzhled

- **Stav:** platí
- **Milníky:** M1
- **Související SDR:** [007](sdr-007-cislovani.md), [013](sdr-013-vyhledavani.md),
  [014](sdr-014-dashboard.md)

## Rozsah

Routy aplikace, rušené stránky, menu a vzhled. Obsah stránek drží SDR jednotlivých agend.

## Popis

### Routy

URL nesou UUID entit, nikdy jejich čísla:

| Routa | Obsah |
| --- | --- |
| `/` | dashboard (SDR-014) |
| `/cases` | seznam spisů |
| `/cases/new` | založení spisu |
| `/cases/{id}` | detail spisu |
| `/cases/{id}/edit` | editace spisu |
| `/cases/{id}/act/new` | založení úkonu |
| `/cases/{id}/act/{actId}` | detail úkonu |
| `/cases/{id}/act/{actId}/edit` | editace úkonu |
| `/contacts` | kontakty |
| `/contacts/{id}` | detail kontaktu |
| `/login` | přihlášení |

### Zaniká

- `/deadlines` — lhůty jsou non-goal.
- `/echo` včetně `EchoController` a jeho kontraktu.
- `/settings` — číslování je natvrdo (SDR-007).
- Widget lhůt na dashboardu včetně jeho vzorových dat; zbytek dnešního dashboardu žije do
  přepisu v M7 (SDR-014).

### Menu a vzhled

Menu nese dashboard, spisy a kontakty. Vzhled zůstává současný: Tabler + TabBlazor,
responsivita podle `.claude/rules/app.md`. Každá stránka žije v `MainLayout`. Každý seznam
má prázdný stav (vzor `.empty`); kde jde záznam založit, nese výzvu k založení.

## Rozhodnutí

- Identifikátor v URL: `CaseNumber` / UUID. Platí UUID.
- Rušené stránky: nechat ležet do náhrady / smazat v M1. Platí smazat v M1.

## Dopady

M1 maže rušené stránky, `EchoController`, jeho kontrakt a klienta. Sekce Verify
v `.claude/skills/run-app/SKILL.md` sonduje `POST /api/echo/post` a stránku `/echo`; M1 je
nahrazuje autentizovaným `GET /api/cases/list` a stránkou `/cases` ve stejném commitu.
Dashboard se přepisuje v M7 (SDR-014).
