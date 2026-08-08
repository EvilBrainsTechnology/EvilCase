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
| `/cases/{id}` | detail spisu |
| `/cases/{id}/edit` | založení a editace spisu |
| `/cases/{id}/act/{actId}` | detail úkonu |
| `/cases/{id}/act/{actId}/edit` | založení a editace úkonu |
| `/contacts` | kontakty |
| `/contacts/{id}` | detail kontaktu |
| `/login` | přihlášení |

### Zaniká

- `/deadlines` — lhůty jsou non-goal.
- `/echo` včetně `EchoController` a jeho kontraktu.
- `/settings` — číslování je natvrdo (SDR-007).

### Menu a vzhled

Menu nese dashboard, spisy a kontakty. Vzhled zůstává současný: Tabler + TabBlazor,
responsivita podle `.claude/rules/app.md`. Každá stránka žije v `MainLayout`.

## Rozhodnutí

- Identifikátor v URL: `CaseNumber` / UUID. Platí UUID.
- Rušené stránky: nechat ležet do náhrady / smazat v M1. Platí smazat v M1.

## Dopady

M1 maže rušené stránky, `EchoController`, jeho kontrakt a klienta. Dashboard se přepisuje
v M7 (SDR-014).
