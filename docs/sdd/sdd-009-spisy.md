# SDD-009 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDD:** [007](sdd-007-domenovy-model.md), [008](sdd-008-cislovani.md),
  [011](sdd-011-kontakty.md), [013](sdd-013-komentare.md), [019](sdd-019-stitky.md)

## Rozsah

Entita spisu, hierarchie, externí značka, stránky spisů a mazání. Číslování patří SDD-008,
úkony SDD-010, soubory SDD-012, komentáře SDD-013.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, `ExternalCaseNumber?`, explicitní datum (`DateOnly`), název,
popis, stav `Active` / `WaitingOnAuthority` / `Closed`, nepovinný kontakt protistrany (SDD-011)
a štítky (SDD-019).

Stav je jen štítek: na nic se neváže, spis ve stavu `Closed` jde editovat a přijímá úkony,
soubory i komentáře jako každý jiný. Nový spis vzniká jako `Active` a s dnešním datem.

Délky: název nejvýše 256 znaků, popis 4000, externí značka 128, spisová značka 64. Povinné jsou
název, datum a stav, v editaci i spisová značka; ostatní pole jsou nepovinná.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný: uložení, které by ho uzavřelo,
  vrací 409 (SDD-004).
- Podřízený spis se zakládá z detailu rodiče a rodiče na formuláři jen ukazuje; v editaci spisu
  jde rodič vybrat z kterýchkoli spisů tenantu kromě samotného spisu. Cyklus se pozná až při
  uložení.
- UI zobrazuje jen ploché seznamy: detail spisu ukazuje odkaz na rodiče a seznam přímých
  podřízených spisů. Žádný strom.

### Externí spisová značka

Spis nese nejvýše jednu značku, kterou mu dal jiný úřad: nepovinný volný text bez vazby na
kontakt. Zadává se na editaci spisu.

### Stránky

- `/cases` — seznam spisů: datum, název se štítky jako drobnými značkami s názvem (SDD-019); podřízený spis ukazuje na prvním řádku vlastní název se štítky a pod ním šedě název rodičovského spisu, jako úkon v seznamu úkonů, stav
  s barevnou tečkou a okamžik poslední změny. Výchozí řazení je podle
  data spisu sestupně; záhlaví sloupce řadí podle sebe a druhý klik otočí směr. Seznam je
  stránkovaný podle SDD-005 a nese celkový počet. Lišta je na jednom řádku: hledání, přepínač
  stavu, tlačítko Filtry a Jen rodičovské spisy. Hledací pole hledá v názvu, popisu, spisové značce a
  externí značce bez ohledu na diakritiku. Přepínač stavu nabízí Otevřené (vše kromě uzavřených), Aktivní, Čeká na úřad,
  Uzavřené a Vše; výchozí je Otevřené. Rozsah dat a štítky jsou pod tlačítkem Filtry, které je ve
  výchozím stavu sbalené a nese počet aktivních filtrů. Jen rodičovské spisy je ve výchozím stavu
  zapnuté a po vypnutí seznam ukazuje i podřízené spisy. Stav i rozsah jde předat odkazem, jako to
  dělají dlaždice přehledu (SDD-015). Příchod na adresu seznamu nastaví všechny filtry podle
  adresy; co adresa nezmiňuje, má výchozí hodnotu.
- `/cases/new` — založení, včetně kontaktu a štítků; `?parent={id}` zakládá podřízený spis.
- `/cases/{id}` — detail: údaje, štítky, podřízené spisy, úkony (SDD-010), komentáře (SDD-013),
  soubory (SDD-012). Řádek údajů nese spisovou značku i externí značku a obě jde zkopírovat.
  Nabídka vedle Upravit spis nese Nový úkon, Nový podřízený spis, Zkopírovat odkaz na spis
  a Smazat spis.
- `/cases/{id}/edit` — editace, včetně kontaktu, štítků a rodiče.
- Úkony v detailu spisu ukazují datum, název se štítky jako drobnými značkami s názvem, směr,
  kontakt a okamžik poslední změny. Kontakt, který je protistranou
  spisu, je potlačený; jiný kontakt je zvýrazněný. Lišta nese hledání a směr; rozsah dat
  a štítky jsou pod tlačítkem Filtry, které je ve výchozím stavu sbalené a nese počet
  aktivních filtrů.
- Podřízené spisy v detailu spisu ukazují počet otevřených a uzavřených a ve výchozím stavu jen
  otevřené (vše kromě uzavřených); přepínač ukáže i uzavřené.

### Mazání

Mazání řídí matice v SDD-007. Potvrzení jednou větou jmenuje, co kaskáda bere: podřízené spisy,
úkony, komentáře a soubory.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí kaskáda — celý podstrom zaniká.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy.
- Filtr stavu v seznamu `/cases`: select / přepínač. Platí přepínač.
- Podřízené spisy v seznamu `/cases`: skryté / volitelný přepínač. Platí přepínač, výchozí
  zapnutý.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.
- Stav spisu: řídí chování / jen štítek. Platí jen štítek.
- Externí značka: N řádků s kontaktem / jeden sloupec. Platí jeden sloupec bez kontaktu.
- Kontakt spisu: povinný / nepovinný. Platí nepovinný.
- Zadání kontaktu: jen editace / založení i editace. Platí obojí.
- Podřízené spisy v detailu spisu: všechny / jen otevřené s přepínačem. Platí jen otevřené
  s přepínačem.
- Číslo jednací a spisová značka v seznamech: ukázat / vynechat. Platí vynechat; ukazuje je
  detail.

## Dopady

—
