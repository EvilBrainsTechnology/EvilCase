# SDD-008 — Číslování

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md), [016](sdd-016-navigace-a-vzhled.md)

## Rozsah

Interní spisové značky (`CaseNumber`) a čísla jednací (`ActNumber`). Externí čísla jsou
volný text a patří SDD-009 a SDD-010.

## Popis

### Formáty

Natvrdo, bez konfigurace:

- `CaseNumber`: `EC/{yyyyMMdd}-{seq:3}` → `EC/20260807-001`.
- `ActNumber`: `{case-number}/{yyyyMMdd}-{seq:3}` → `EC/20260807-001/20260812-001`.

### Pravidla

- Datum ve značce je explicitní datum spisu, resp. úkonu, ne okamžik založení záznamu.
- `seq` spisu počítá per tenant a den, `seq` úkonu per spis a den; start `001`, přetečení
  přidá cifru. Pořadí se čte podle délky a pak podle znaků, takže `1000` následuje po `999`.
- Zpětně datovaný spis dostane další volné pořadí svého dne.
- `CaseNumber` i `ActNumber` jsou unikátní v tenantu.
- Číslo vzniká při založení entity k jejímu datu; založení číslo nepřebírá od volajícího.
  Změna data entity číslo nepřegenerovává.
- Editace číslo přepíše, hlídá se formát a unikátnost; delší než 64 znaků u spisu a 128
  u úkonu se nepřijme. Datum uvnitř přepsaného čísla se na datum entity neváže.
- Přepis `CaseNumber` nemění už vydaná `ActNumber` jeho úkonů.
- Do pořadí se počítají jen čísla s prefixem formátu; ručně přepsaná hodnota mimo formát
  prefix nemá.

### Souběh

Číslo se přiděluje z nejvyššího čísla dne a ukládá se rovnou. Souběžné uložení dvou entit
téhož dne skončí porušením unikátního indexu; zápis si vezme další volné číslo a zkusí to
znovu, nejvýše pětkrát. Pátý neúspěch je 500.

## Rozhodnutí

- Vzory: konfigurovatelné v Nastavení / natvrdo. Platí natvrdo.
- Souběh: sekvence v databázi per den / nejvyšší číslo a opakovaný zápis. Platí opakovaný zápis.
- Změna data entity: číslo se přegenerovává / nemění. Číslo se nemění.
- Ruční přepis: kdykoli / jen v editaci. Platí jen v editaci.

## Dopady

—
