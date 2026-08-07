# Produktová vize

Zdroj pravdy o tom, co se z EvilCase staví. Produktový loop čte tento soubor na začátku každého
kola a bootstrap z něj odvozuje labely, milníky a backlog. Závazný detail návrhu žije v SDR pod
[`docs/sdr/`](../sdr/README.md); každý milník jmenuje SDR, která ho řídí. Roadmapa a otevřené
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
  ([SDR-001](../sdr/sdr-001-tenance-a-ucty.md)). Účty vznikají jen seedem; registrace, pozvánky
  a více uživatelů v tenantu přijdou později.

## Doménový model

Názvy v kódu jsou anglické: spis = `Case`, úkon = `Act`, soubor = `FileAsset`, kontakt =
`Contact`, komentář = `Comment`; spisová značka = `CaseNumber`, číslo jednací = `ActNumber`,
cizí, přidělené někým jiným, jsou `ExternalCaseNumber` a `ExternalActNumber`. Mapu drží
[SDR-002](../sdr/sdr-002-domenovy-model.md).

**Spis** — jedno řízení. Nese explicitní datum, název, popis, status (`Active`,
`WaitingOnAuthority`, `Closed`), externí značky, komentáře a soubory. Spisy tvoří hierarchii:
volitelný rodič, libovolná hloubka, UI ukazuje jen ploché seznamy. Bez tagů.
([SDR-004](../sdr/sdr-004-spisy.md))

**Úkon** — jednotka práce ve spisu: jedno podání, rozhodnutí, vyrozumění nebo výzva. Má směr
(příchozí/odchozí), povinného odesílatele a nepovinného příjemce, explicitní datum, N externích
čísel jednacích, popis, komentáře a soubory. Seznamy úkonů se řadí výhradně podle data úkonu.
([SDR-005](../sdr/sdr-005-ukony.md))

**Kontakt** — úřad, úřední osoba nebo člověk; plochý, sdílený napříč spisy; nese id datové
schránky a adresu jako jeden volný text. Vybírá se nebo zakládá inline všude, kde ho spis, úkon
nebo číslo jmenuje, a spravuje se v agendě, která ukazuje, kde všude figuruje. Každý uživatel
má automatický defaultní kontakt. Smazat jde jen kontakt, na který nic neodkazuje.
([SDR-006](../sdr/sdr-006-kontakty.md))

**Soubor** — patří právě jednomu spisu nebo úkonu; žádné odkazy mezi soubory a jinými
entitami. Bajty leží na disku, databáze nese metadata. Upload i download v prohlížeči, včetně
hromadného přetažením. ([SDR-007](../sdr/sdr-007-soubory.md))

**Externí čísla** — spis nese N cizích značek, úkon N cizích čísel jednacích; každé je svázané
s kontaktem, který ho přidělil — každý úřad v řetězu přiděluje své.

**Komentář** — volná poznámka ke spisu nebo úkonu, průběžný deník. Edituje a maže jen autor.
([SDR-008](../sdr/sdr-008-komentare.md))

Vše, co uživatel zadá, jde editovat i smazat; destruktivní operace se napřed potvrzuje.

## Číslování

Aplikace vydává vlastní čísla natvrdo, bez konfigurace: spis `EC/20260807-001`, úkon
`EC/20260807-001/20260812-001`; pořadí počítá den z data entity. Ruční přepis je možný,
unikátnost hlídá databáze. Pravidla a souběh drží [SDR-003](../sdr/sdr-003-cislovani.md).

## Aplikace

URL nesou UUID: `/cases`, `/cases/{id}`, `/cases/{id}/act/{actId}`, `/contacts`, `/login`
([SDR-011](../sdr/sdr-011-navigace-a-vzhled.md)). Dashboard `/` stojí nad reálnými daty
([SDR-010](../sdr/sdr-010-dashboard.md)). Hledání ignoruje diakritiku, pokrývá názvy, popisy
a čísla; přesná shoda čísla skočí rovnou na spis nebo úkon
([SDR-009](../sdr/sdr-009-vyhledavani.md)). Vzhled zůstává: Tabler + TabBlazor.

## Vzorová data

`EvilBrains__EvilCase__Database__SeedSampleData` (default `false`) naplní databázi při startu,
v jakémkoli prostředí, jen dokud tenant nemá žádný spis. Data jsou pseudonymizovaný případ
o překročení rychlosti z `test-data/case-01-speeding.md`: strom spisů, kontakty, značky, úkony
se syntetickými TXT soubory, komentáře — každá obrazovka se staví a ověřuje nad rozsahem
reálného případu. ([SDR-012](../sdr/sdr-012-seed-vzorovych-dat.md))

## Priority

V pořadí podle toho, co při práci s reálným spisem bolí nejvíc:

1. Všechno k případu na jednom místě — spis, podřízené spisy, úkony a dokumenty — místo složky
   na disku.
2. Zavedení nového úkonu s jeho dokumenty pod minutu.
3. Rychlé nalezení spisu i úkonu, textem i číslem; hledání ignoruje diakritiku.

## Milníky

| # | Milník | Dodá | Řídí |
| --- | --- | --- | --- |
| M1 | Úklid | smazání stránek a API mimo vizi: `/deadlines`, `/echo` s kontrolerem a kontraktem, `/settings` | SDR-011 |
| M2 | Datový model a seed | nové entity, tenance, interceptory, číslování, jedna Init migrace, rename Party → Contact, seed účtů i vzorových dat | SDR-001, 002, 003, 006, 012 |
| M3 | Spisy | seznam, detail a editace spisu; hierarchie, externí značky, komentáře spisů, kaskádové mazání | SDR-004, 008 |
| M4 | Úkony | detail a editace úkonu; směr, kontakty, externí čísla, komentáře úkonů | SDR-005, 008 |
| M5 | Soubory | úložiště, upload včetně hromadného přetažením, download, mazání | SDR-007 |
| M6 | Kontakty | agenda kontaktů s výskyty, inline výběr, defaultní kontakt v UI | SDR-006 |
| M7 | Dashboard a hledání | dashboard nad reálnými daty, fulltext s navigací přesnou shodou | SDR-009, 010 |

Průřezová SDR-013 až 016 platí pro každý milník. Základ je hotový, když jde reálný spis vést
rukou od začátku do konce.

## Non-goals pro teď

Konfigurovatelné číslování, tagy, vazby souborů mezi úkony, lhůty, timeline, import složek,
extrakce textu z dokumentů a fulltext nad obsahem souborů, .docx šablony a generovaná podání,
datové schránky (ISDS), e-mail, AI shrnutí, role, registrace, pozvánky, více uživatelů
v tenantu, billing, správa uživatelů nad rámec seedovaného administrátora. Model všem nechává
místo; nic z toho se nestaví.

## Soukromí

Repozitář je veřejný. `.claude/rules/github.md` drží reálný obsah spisů mimo každý zápis;
testovací fixtures jsou syntetické nebo pseudonymizované (`test-data/README.md`). Reálné složky
spisů na disku jsou jen ke čtení.
