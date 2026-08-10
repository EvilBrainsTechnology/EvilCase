# SDR-010 — Kontakty

- **Stav:** platí
- **Milníky:** M2, M3, M4, M6
- **Související SDR:** [003](sdr-003-validace-a-chyby.md), [008](sdr-008-spisy.md),
  [009](sdr-009-ukony.md)

## Rozsah

Entita kontaktu, defaultní kontakt uživatele, inline výběr a agenda kontaktů.

## Popis

### Entita

Contact — přejmenovaná Party, `Kind` zůstává (`Authority` / `Official` / `Person`): název,
id datové schránky, adresa jako jeden volný text tištěný po blocích. Rename entity vzniká
v M2 (SDR-006); agenda přichází v M6.

### Defaultní kontakt

Vzniká automaticky při založení uživatele s názvem z jeho e-mailu; ukazuje na něj
`User.DefaultContactId`. Jde přejmenovat, smazat nejde. Předvyplňuje se jako odesílatel
odchozích a příjemce příchozích úkonů (SDR-009). Přepojení `User.DefaultContactId` na jiný
kontakt se zatím nepodporuje.

### Výběr a založení

Jedna inline komponenta všude, kde spis, úkon nebo číslo kontakt jmenuje: vybrat existující,
nebo založit nový bez opuštění formuláře. Vyžadují ji už externí značky spisů (M3)
a odesílatel s příjemcem úkonu (M4). Našeptávač hledá podle názvu a id datové schránky.
Inline založení vyžaduje název a `Kind`; ostatní pole jsou nepovinná.

### Agenda

- `/contacts` — přehled kontaktů s hledacím polem (název, id datové schránky).
- `/contacts/{id}` — detail s výskyty: spisy přes značky, úkony přes odesílatele, příjemce
  a externí čísla; editace a smazání kontaktu.

Přehled i výskyty jsou bez stránkování.

### Mazání

Smazat jde jen kontakt, na který nic neodkazuje; jinak 409 (SDR-003). Defaultní kontakt
smazat nejde.

## Rozhodnutí

- `Kind`: zaniká / zůstává. Zůstává.
- Defaultní kontakt: obyčejný kontakt / chráněný před smazáním a přejmenovatelný. Platí
  chráněný a přejmenovatelný.
- Mazání odkazovaného kontaktu: přepojení referencí / zákaz. Platí zákaz.

## Dopady

Rename Party → Contact prochází kódem, kontraktem i UI (SDR-004, SDR-006).
