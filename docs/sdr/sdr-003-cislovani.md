# SDR-003 — Číslování

- **Stav:** platí
- **Milníky:** M2
- **Související SDR:** [004](sdr-004-spisy.md), [005](sdr-005-ukony.md),
  [009](sdr-009-vyhledavani.md)

## Rozsah

Interní spisové značky (`CaseNumber`) a čísla jednací (`ActNumber`). Externí čísla jsou
volný text a patří SDR-004 a SDR-005.

## Popis

### Formáty

Natvrdo, bez konfigurace:

- `CaseNumber`: `EC/{yyyyMMdd}-{seq:3}` → `EC/20260807-001`.
- `ActNumber`: `{case-number}/{yyyyMMdd}-{seq:3}` → `EC/20260807-001/20260812-001`.

### Pravidla

- Datum ve značce je explicitní datum spisu, resp. úkonu, ne okamžik založení záznamu.
- `seq` spisu počítá per tenant a den, `seq` úkonu per spis a den; start `001`, přetečení
  přidá cifru.
- Zpětně datovaný spis dostane další volné pořadí svého dne.
- `CaseNumber` i `ActNumber` jsou unikátní v tenantu.
- Číslo vzniká při založení entity k jejímu datu. Změna data entity číslo nepřegenerovává.
- Ruční přepis je možný; hlídá se formát (regex) a unikátnost. Datum uvnitř přepsaného čísla
  se na datum entity neváže.
- Přepis `CaseNumber` nemění už vydaná `ActNumber` jeho úkonů.

### Souběh

Generátor čte MAX pořadí dne a vkládá; souběh chytá unikátní index a insert se opakuje
s dalším pořadím.

### Implementace

Skládání, parsování a validace čísla je čistá doménová logika — statická třída bez
`DbContext`, testovaná bez databáze (SDR-015).

## Rozhodnutí

- Vzory: konfigurovatelné v Nastavení / natvrdo. Platí natvrdo; obrazovka Nastavení zaniká
  (SDR-011).
- Souběh: DB sekvence per den / MAX + insert s retry. Platí MAX + insert s retry na unique
  violation.
- Změna data entity: číslo se přegenerovává / nemění. Číslo se nemění.

## Dopady

Konfigurace vzorů v databázi a obrazovka Nastavení zanikají (SDR-011). Přesná shoda čísla
naviguje rovnou na entitu (SDR-009).
