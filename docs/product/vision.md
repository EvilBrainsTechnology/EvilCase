# Produktová vize

Zdroj pravdy o tom, co se z EvilCase staví. Produktový loop čte tento soubor na začátku každého
kola; labely, milníky a backlog z něj jednorázově zakládá skill `bootstrap-backlog`. Závazný
detail návrhu žije v SDD pod
[`docs/sdd/`](../sdd/README.md); každý milník jmenuje SDD, která ho řídí. Roadmapa a otevřené
otázky žijí v GitHub Issues, ne tady.

## Co je EvilCase

Systém pro vedení spisů správních a soudních řízení. Spis v čase roste: přibývají úkony,
dokumenty přicházejí a odcházejí, pod hlavním řízením se větví podřízená. Aplikace to všechno
drží na jednom místě a v každém okamžiku odpoví, co se stalo a co ve spisu je.

Současný cíl je **první použitelný základ**: jeden člověk vede reálný spis rukou — každý spis,
úkon, dokument, kontakt i poznámka žije v aplikaci místo ve složce na disku. Záměrně malý; co
základ vynechává, je dole v non-goals a přijde později, po krocích.

## Horizont

- Teď: jeden tenant s jedním uživatelem, ruční zadávání. Optimalizuje se rychlost práce
  s reálným spisem.
- Tenance není rezervovaný sloupec, ale vynucený model: Account zastřešuje N tenantů, každá
  tenantová entita nese `TenantId` a izolaci vynucují query filtry
  ([SDD-006](../sdd/sdd-006-tenance-a-ucty.md)). Účty vznikají jen seedem; registrace, pozvánky
  a více uživatelů v tenantu přijdou později.

## Doménový model

Názvy v kódu jsou anglické: spis = `Case`, úkon = `Act`, soubor = `FileAsset`, kontakt =
`Contact`, komentář = `Comment`; spisová značka = `CaseNumber`, číslo jednací = `ActNumber`,
cizí, přidělené někým jiným, jsou `ExternalCaseNumber` a `ExternalActNumber`. Mapu drží
[SDD-007](../sdd/sdd-007-domenovy-model.md).

**Spis** — jedno řízení. Nese explicitní datum, název, popis, status (`Active`,
`WaitingOnAuthority`, `Closed`), externí spisovou značku, komentáře a soubory. Spisy tvoří
hierarchii: volitelný rodič, libovolná hloubka, UI ukazuje jen ploché seznamy. Bez tagů.
([SDD-009](../sdd/sdd-009-spisy.md))

**Úkon** — jednotka práce ve spisu: jedno podání, rozhodnutí, vyrozumění nebo výzva. Má směr
(příchozí/odchozí), povinného odesílatele a nepovinného příjemce, explicitní datum, název,
externí číslo jednací, popis, komentáře a soubory.
([SDD-010](../sdd/sdd-010-ukony.md))

**Kontakt** — úřad, úřední osoba nebo člověk; plochý, sdílený napříč spisy; nese id datové
schránky a adresu jako jeden volný text. Vybírá se nebo zakládá inline všude, kde ho úkon jmenuje,
a spravuje se v agendě, která ukazuje, ve kterých úkonech figuruje.
([SDD-011](../sdd/sdd-011-kontakty.md))

**Soubor** — patří právě jednomu spisu nebo úkonu; žádné odkazy mezi soubory a jinými
entitami. Bajty leží na disku, databáze nese metadata. Upload i download v prohlížeči, včetně
hromadného přetažením. ([SDD-012](../sdd/sdd-012-soubory.md))

**Externí čísla** — spis nese nejvýše jednu cizí spisovou značku, úkon nejvýše jedno cizí číslo
jednací; obojí je nepovinný volný text bez vazby na kontakt.

**Komentář** — volná poznámka ke spisu nebo úkonu, průběžný deník. Edituje a maže jen autor.
([SDD-013](../sdd/sdd-013-komentare.md))

Vše, co uživatel zadá, jde editovat i smazat; destruktivní operace se napřed potvrzuje.

## Číslování

Aplikace čísluje spisy a úkony sama, bez konfigurace; tvar čísel, ruční přepis i souběh drží
[SDD-008](../sdd/sdd-008-cislovani.md).

## Aplikace

URL nesou UUID: `/cases`, `/cases/{id}`, `/cases/{id}/act/{actId}`, `/contacts`, `/login`
([SDD-016](../sdd/sdd-016-navigace-a-vzhled.md)). Dashboard `/` stojí nad reálnými daty
([SDD-015](../sdd/sdd-015-dashboard.md)). Vzhled zůstává: Tabler + TabBlazor.

## Vzorová data

Zdrojem je pseudonymizovaný případ překročení rychlosti z `test-data/case-01-speeding.md`;
kdy a jak ho seed plní, drží [SDD-017](../sdd/sdd-017-seed-vzorovych-dat.md). Každá
obrazovka se staví a ověřuje nad rozsahem reálného případu.

## Priority

V pořadí podle toho, co při práci s reálným spisem bolí nejvíc:

1. Všechno k případu na jednom místě — spis, podřízené spisy, úkony a dokumenty — místo složky
   na disku.
2. Zavedení nového úkonu s jeho dokumenty pod minutu.

## Milníky

| # | Milník | Dodá | Řídí |
| --- | --- | --- | --- |
| M1 | Úklid | smazání stránek a API mimo vizi: `/deadlines`, `/echo` s kontrolerem a kontraktem, `/settings` | SDD-016 |
| M2 | Datový model a seed | nové entity, tenance, interceptory, číslování, jedna Init migrace, rename Party → Contact, jádro souborového úložiště, seed účtů i vzorových dat | SDD-006, 007, 008, 011, 012, 017 |
| M3 | Spisy | seznam, založení, detail a editace spisu; hierarchie, externí značka, komentáře spisů, kaskádové mazání | SDD-009, 011, 013 |
| M4 | Úkony | založení, detail, editace a mazání úkonu; směr, kontakty, externí číslo jednací, komentáře úkonů | SDD-010, 011, 013 |
| M5 | Soubory | UI souborů: upload včetně hromadného přetažením, download, mazání | SDD-012 |
| M6 | Kontakty | agenda kontaktů s výskyty, defaultní kontakt v UI | SDD-011 |
| M7 | Dashboard | dashboard nad reálnými daty | SDD-015 |

Průřezová SDD-001 až 005 platí pro každý milník. Základ je hotový, když jde reálný spis vést
rukou od začátku do konce.

## Non-goals pro teď

Konfigurovatelné číslování, tagy, vazby souborů mezi úkony, lhůty, timeline, import složek,
hledání nad spisy a úkony, extrakce textu z dokumentů a fulltext nad obsahem souborů, .docx
šablony a generovaná podání, datové schránky (ISDS), e-mail, AI shrnutí, role, registrace,
pozvánky, více uživatelů v tenantu, billing, správa uživatelů nad rámec seedovaného
administrátora. Model všem nechává místo; nic z toho se nestaví.

## Soukromí

Repozitář je veřejný; `.claude/rules/github.md` drží reálný obsah spisů mimo každý zápis.
Testovací fixtures jsou syntetické nebo pseudonymizované (`test-data/README.md`).
