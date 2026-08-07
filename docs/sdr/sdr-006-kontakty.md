# SDR-006 — Kontakty

- **Stav:** platí
- **Milníky:** M2, M6
- **Související SDR:** [004](sdr-004-spisy.md), [005](sdr-005-ukony.md),
  [014](sdr-014-validace-a-chyby.md)

## Rozsah

Entita kontaktu, defaultní kontakt uživatele, inline výběr a agenda kontaktů.

## Popis

### Entita

Contact — přejmenovaná Party, `Kind` zůstává (`Authority` / `Official` / `Person`): název,
id datové schránky, adresa jako jeden volný text tištěný po blocích. Rename entity vzniká
v M2 (SDR-002); agenda přichází v M6.

### Defaultní kontakt

Vzniká automaticky při založení uživatele s názvem z jeho e-mailu; ukazuje na něj
`User.DefaultContactId`. Jde přejmenovat, smazat nejde. Předvyplňuje se jako odesílatel
odchozích a příjemce příchozích úkonů (SDR-005).

### Výběr a založení

Jedna inline komponenta všude, kde spis, úkon nebo číslo kontakt jmenuje: vybrat existující,
nebo založit nový bez opuštění formuláře.

### Agenda

- `/contacts` — přehled kontaktů.
- `/contacts/{id}` — detail s výskyty: spisy přes značky, úkony přes odesílatele, příjemce
  a externí čísla.

### Mazání

Smazat jde jen kontakt, na který nic neodkazuje; jinak 409 (SDR-014). Defaultní kontakt
smazat nejde.

## Rozhodnutí

- `Kind`: zaniká / zůstává. Zůstává.
- Defaultní kontakt: obyčejný kontakt / chráněný před smazáním a přejmenovatelný. Platí
  chráněný a přejmenovatelný.
- Mazání odkazovaného kontaktu: přepojení referencí / zákaz. Platí zákaz.

## Dopady

Rename Party → Contact prochází kódem, kontraktem i UI (SDR-002, SDR-013).
