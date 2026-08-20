# SDD-008 — Číslování

- **Stav:** platí
- **Milníky:** M2
- **Související SDD:** [009](sdd-009-spisy.md), [010](sdd-010-ukony.md),
  [014](sdd-014-vyhledavani.md), [016](sdd-016-navigace-a-vzhled.md)

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
  přidá cifru.
- Zpětně datovaný spis dostane další volné pořadí svého dne.
- `CaseNumber` i `ActNumber` jsou unikátní v tenantu.
- Číslo vzniká při založení entity k jejímu datu. Změna data entity číslo nepřegenerovává.
- Ruční přepis je možný; hlídá se formát (regex) a unikátnost. Datum uvnitř přepsaného čísla
  se na datum entity neváže.
- Přepis `CaseNumber` nemění už vydaná `ActNumber` jeho úkonů.

### Souběh

Generátor čte MAX pořadí dne a vkládá; souběh chytá unikátní index a insert se opakuje
s dalším pořadím. MAX se počítá parsováním čísel odpovídajících formátu; ručně přepsané
neodpovídající hodnoty se do pořadí nepočítají.

### Implementace

Skládání, parsování a validace čísla je čistá doménová logika bez `DbContext`, testovaná
bez databáze (SDD-003).

## Rozhodnutí

- Vzory: konfigurovatelné v Nastavení / natvrdo. Platí natvrdo; obrazovka Nastavení zaniká
  (SDD-016).
- Souběh: DB sekvence per den / MAX + insert s retry. Platí MAX + insert s retry na unique
  violation.
- Změna data entity: číslo se přegenerovává / nemění. Číslo se nemění.

## Dopady

Placeholder stránka Nastavení zaniká (SDD-016). Přesná shoda čísla naviguje rovnou na
entitu (SDD-014).
