# SDD-011 — Kontakty

- **Stav:** platí
- **Milníky:** M2, M4, M6
- **Související SDD:** [004](sdd-004-validace-a-chyby.md), [009](sdd-009-spisy.md),
  [010](sdd-010-ukony.md)

## Rozsah

Entita kontaktu, inline výběr, zakládání a agenda kontaktů.

## Popis

### Entita

Contact: `Kind` (`Authority` / `Official` / `Person`), název, id datové schránky, adresa jako
jeden volný text tištěný po blocích. Povinné jsou název a `Kind`. Délky: název nejvýše 256 znaků,
id datové schránky 16, adresa 1024.

### Výběr a založení

Jedna inline komponenta všude, kde kontakt jmenuje spis nebo úkon: vybrat existující, nebo
založit nový bez opuštění formuláře. Vyžaduje ji kontakt spisu (SDD-009) i kontakt úkonu
(SDD-010). Našeptávač hledá podle názvu a id datové schránky, nabízí nejvýše osm shod a nabídne
založení nového kontaktu, když neodpovídá žádný. Týž formulář zakládá kontakt i v agendě
a edituje existující.

### Agenda

- `/contacts` — přehled kontaktů: název, typ, id datové schránky, adresa. Řadí se podle názvu.
  Hledací pole hledá v názvu a id datové schránky bez ohledu na diakritiku. Zakládá se odsud
  nový kontakt.
- `/contacts/{id}` — detail s výskyty: spisy kontaktu a ty jeho úkony, jejichž kontakt se liší
  od kontaktu jejich spisu, obojí od nejnovějšího data; editace a smazání kontaktu.

Přehled i výskyty jsou bez stránkování.

### Mazání

Smazat jde jen kontakt, na který neodkazuje žádný spis ani úkon; jinak 409 (SDD-004).

## Rozhodnutí

- Mazání odkazovaného kontaktu: přepojení referencí / zákaz. Platí zákaz.
- Výskyty úkonů: všechny úkony kontaktu / jen ty, jejichž kontakt se liší od spisu. Platí jen
  odlišné.
- Zakládání kontaktu: jen inline / inline i v agendě. Platí obojí, jedním formulářem.

## Dopady

—
