# SDD-016 — Navigace a vzhled

- **Stav:** platí
- **Milníky:** M1
- **Související SDD:** [008](sdd-008-cislovani.md), [014](sdd-014-vyhledavani.md),
  [015](sdd-015-dashboard.md)

## Rozsah

Routy aplikace, rušené stránky, menu a vzhled. Obsah stránek drží SDD jednotlivých agend.

## Popis

### Routy

URL nesou UUID entit, nikdy jejich čísla:

| Routa | Obsah |
| --- | --- |
| `/` | dashboard (SDD-015) |
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
- `/settings` — číslování je natvrdo (SDD-008).
- Widget lhůt na dashboardu včetně jeho vzorových dat; zbytek dnešního dashboardu žije do
  přepisu v M7 (SDD-015).

### Menu a vzhled

Menu nese dashboard, spisy a kontakty. Vzhled zůstává současný: Tabler + TabBlazor,
responsivita podle `.claude/rules/app.md`. Každá stránka žije v `MainLayout`. Každý seznam
má prázdný stav (vzor `.empty`); kde jde záznam založit, nese výzvu k založení.

## Rozhodnutí

- Identifikátor v URL: `CaseNumber` / UUID. Platí UUID.
- Rušené stránky: nechat ležet do náhrady / smazat v M1. Platí smazat v M1.

## Dopady

Rušené stránky, `EchoController`, jeho kontrakt ani klient v repozitáři nejsou. Sekce Verify
v `.claude/skills/run-app/SKILL.md` sonduje autentizovaný `GET /api/cases/list` a stránku
`/cases`. Dashboard se přepisuje v M7 (SDD-015).
