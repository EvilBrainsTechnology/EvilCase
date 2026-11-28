# SDD-009 — Spisy

- **Stav:** platí
- **Milníky:** M3
- **Související SDD:** [007](sdd-007-domenovy-model.md), [008](sdd-008-cislovani.md),
  [011](sdd-011-kontakty.md), [012](sdd-012-soubory.md), [013](sdd-013-komentare.md),
  [019](sdd-019-stitky.md)

## Rozsah

Entita spisu, hierarchie, externí značka, stránky spisů a mazání. Číslování patří SDD-008,
úkony SDD-010, soubory SDD-012, komentáře SDD-013.

## Popis

### Entita

Case: `ParentCaseId?`, `CaseNumber`, `ExternalCaseNumber?`, explicitní datum (`DateOnly`), název,
popis, stav `Active` / `WaitingOnAuthority` / `Closed`, nepovinný kontakt protistrany (SDD-011)
a štítky (SDD-019).

Stav je jen štítek: na nic se neváže, spis ve stavu `Closed` jde editovat a přijímá úkony
i komentáře jako každý jiný. Nový spis vzniká jako `Active` a s dnešním datem. Spis nepřijímá
soubory; soubor patří úkonu (SDD-012).

Délky: název nejvýše 256 znaků, popis 4000, externí značka 128, spisová značka 64. Povinné jsou
název, datum a stav, v editaci i spisová značka; ostatní pole jsou nepovinná.

### Hierarchie

- Rodič je volitelný, hloubka libovolná. Cyklus je zakázaný: uložení, které by ho uzavřelo,
  vrací 409 (SDD-004).
- Podřízený spis se zakládá z detailu rodiče a rodiče na formuláři jen ukazuje; v editaci spisu
  jde rodič vybrat z kterýchkoli spisů tenantu kromě samotného spisu. Cyklus se pozná až při
  uložení.
- Detail spisu ukazuje odkaz na rodiče a seznam přímých podřízených spisů, drobečky celou cestu
  od kořenového spisu (SDD-016). Strom ukazuje jen okno Soubory spisu.

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
- `/cases/{id}` — detail: údaje, štítky, úkony (SDD-010), podřízené spisy a komentáře (SDD-013);
  soubory nenese, ukazuje je okno Soubory spisu. Řádek údajů nese spisovou značku i externí značku a obě jde zkopírovat.
  Nabídka vedle Upravit spis nese Nový úkon, Nový podřízený spis, Zkopírovat odkaz na spis
  a Smazat spis.
- `/cases/{id}/edit` — editace, včetně kontaktu, štítků a rodiče.
- Úkony v detailu spisu ukazují datum, název se štítky jako drobnými značkami s názvem a pod ním
  kontakt, hlavní soubor, směr, stav a okamžik poslední změny (SDD-010). Kontakt, který je
  protistranou spisu, je šedý; jiný kontakt je tmavý. Lišta nese hledání a směr; rozsah dat
  a štítky jsou pod tlačítkem Filtry, které je ve výchozím stavu sbalené a nese počet
  aktivních filtrů. Seznam je stránkovaný po 20 a nese Nový úkon.
- Podřízené spisy v detailu spisu jsou seznam spisů zúžený tímto spisem: datum, název se štítky
  bez názvu rodiče, stav a okamžik poslední změny. Lišta nese hledání a výběr Stav s volbami
  seznamu `/cases`; výchozí je Otevřené. Seznam je stránkovaný po 20 a nese Nový podřízený spis.
  Spis bez podřízených spisů ukazuje místo lišty a seznamu jednu větu; spis, jehož podřízené
  spisy skryje jen výchozí stav, lištu nechává.
- Každý seznam spisů nese hned za názvem sloupec Soubory: dlaždice otevírá okno Soubory spisu
  daného spisu. Pod lg stojí dlaždice vpravo, svisle uprostřed řádku.

### Soubory spisu

- Okno ukazuje rozbalovací strom jednoho spisu: podřízené spisy do libovolné hloubky, úkony
  každého spisu ve stromu a soubory každého úkonu. Strom nic nemění.
- Na každé úrovni jsou nejdřív podřízené spisy v pořadí seznamu podřízených spisů v detailu,
  pak úkony spisu chronologicky. Strom ukazuje i spisy a úkony bez souborů.
- Po otevření jsou rozbalené úkony otevřeného spisu a sbalené jeho podřízené spisy. Klik na řádek
  spisu nebo úkonu ho rozbalí nebo sbalí; Rozbalit vše a Sbalit vše platí pro celý strom.
- Řádek spisu nese název, datum, počet úkonů, podřízených spisů a souborů celého podstromu,
  štítky, stav a odkaz na detail. Řádek úkonu nese název, datum, kontakt, počet souborů, štítky,
  směr (příchozí nebo odchozí jen ikonou s popiskem, jako v seznamech úkonů; úkon bez směru ho
  nenese), stav a odkaz na detail. Řádek souboru nese ikonu, název, verzi, velikost, okamžik nahrání,
  hvězdu u hlavního souboru a stažení. Pod lg řádky nenesou štítky, stav ukazují tečkou.
- Klik na název souboru otevře plný náhled (SDD-012) ve velikosti okna stromu; šipky náhledu
  listují jen soubory téhož úkonu. Změny z náhledu strom ukáže po jeho zavření.
- Stáhnout vše stáhne archiv celého stromu, Stáhnout vše včetně verzí i se staršími verzemi
  (SDD-012).
- Okno jde maximalizovat; pod lg zabírá celou obrazovku.
- Okno otevírá tlačítko Soubory s počtem souborů v hlavičce detailu spisu, pod lg řádek Soubory
  v údajích spisu s textem „26 souborů v 6 úkonech“, a dlaždice v seznamech spisů. Tlačítko,
  řádek i dlaždice jsou jen u spisu, jehož strom nese aspoň jeden soubor.

### Mazání

Mazání řídí matice v SDD-007. Potvrzení jednou větou jmenuje, co kaskáda bere: podřízené spisy,
úkony, komentáře a soubory.

## Rozhodnutí

- Podřízené spisy při smazání rodiče: kaskáda / osiření. Platí kaskáda — celý podstrom zaniká.
- Hierarchie v UI: strom / ploché seznamy. Platí ploché seznamy; strom ukazuje jen okno Soubory
  spisu.
- Filtr stavu v seznamu `/cases`: select / přepínač. Platí přepínač.
- Podřízené spisy v seznamu `/cases`: skryté / volitelný přepínač. Platí přepínač, výchozí
  zapnutý.
- Datum spisu: datum založení záznamu / explicitní pole. Platí explicitní pole.
- Stav spisu: řídí chování / jen štítek. Platí jen štítek.
- Externí značka: N řádků s kontaktem / jeden sloupec. Platí jeden sloupec bez kontaktu.
- Kontakt spisu: povinný / nepovinný. Platí nepovinný.
- Zadání kontaktu: jen editace / založení i editace. Platí obojí.
- Podřízené spisy v detailu spisu: karta / seznam spisů. Platí seznam spisů s výběrem stavu,
  výchozí Otevřené.
- Kontakt úkonu v detailu spisu: sloupec / řádek pod názvem. Platí řádek pod názvem.
- Číslo jednací a spisová značka v seznamech: ukázat / vynechat. Platí vynechat; ukazuje je
  detail.

## Dopady

—
