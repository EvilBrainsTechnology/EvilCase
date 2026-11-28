# Vizuální předloha

Statické HTML předlohy obrazovek, ze kterých se implementuje vzhled podle
[SDD-020](../sdd/sdd-020-vzhled.md). Jeden soubor je jedna obrazovka v šířce 1440 px, předloha s příponou `-mobil` v šířce 390 px.
Soubory jsou závazné na hodnoty: barvu, rozestup, rádius, velikost písma a ikonu čti odsud,
nedomýšlej je. Stavy, které předloha neukazuje (prázdný seznam, chyba, načítání, mobil
obrazovky bez mobilní předlohy),
řídí SDD-020 a SDD dané agendy.

Předlohy jsou statické: odkazy mezi nimi vedou na sousední předlohu, formuláře nic neodesílají,
data jsou pseudonymizovaný případ z [`test-data/case-01-speeding.md`](../../test-data/case-01-speeding.md).
Otevři je v prohlížeči přímo ze souboru.

| Soubor | Obrazovka | Routa |
| --- | --- | --- |
| [`nacitani.html`](nacitani.html) | Načítání aplikace | start aplikace |
| [`prihlaseni.html`](prihlaseni.html) | Přihlášení | `/login` |
| [`prehled.html`](prehled.html) | Přehled | `/` |
| [`prehled-mobil.html`](prehled-mobil.html) | Přehled na mobilu | `/` |
| [`spisy.html`](spisy.html) | Seznam spisů | `/cases` |
| [`spisy-mobil.html`](spisy-mobil.html) | Seznam spisů na mobilu, předloha lišty a řádků všech seznamů | `/cases` |
| [`novy-spis.html`](novy-spis.html) | Založení spisu, předloha i pro editaci | `/cases/new`, `/cases/{id}/edit` |
| [`detail-spisu.html`](detail-spisu.html) | Detail spisu | `/cases/{id}` |
| [`detail-spisu-mobil.html`](detail-spisu-mobil.html) | Detail spisu na mobilu | `/cases/{id}` |
| [`detail-ukonu.html`](detail-ukonu.html) | Detail úkonu | `/acts/{actId}` |
| [`novy-ukon.html`](novy-ukon.html) | Založení úkonu, předloha i pro editaci | `/acts/new?case={id}`, `/acts/{actId}/edit` |
| [`nahled-dokumentu.html`](nahled-dokumentu.html) | Náhled dokumentu Word v modalu souborů | detail úkonu |
| [`nahled-dokumentu-mobil.html`](nahled-dokumentu-mobil.html) | Náhled souboru na mobilu, předloha hlavičky a lišty všech náhledů | detail úkonu |
| [`soubory-verze.html`](soubory-verze.html) | Karta souborů s verzemi | detail úkonu |
| [`verze-souboru.html`](verze-souboru.html) | Verze souboru | detail úkonu |
| [`nahled-starsi-verze.html`](nahled-starsi-verze.html) | Náhled starší verze | detail úkonu |
| [`nahled-aktualni-verze.html`](nahled-aktualni-verze.html) | Náhled aktuální verze | detail úkonu |
| [`verze-souboru-mobil.html`](verze-souboru-mobil.html) | Verze souboru na mobilu | detail úkonu |
| [`soubory-spisu.html`](soubory-spisu.html) | Soubory spisu, strom podřízených spisů, úkonů a souborů | detail spisu, seznamy spisů |
| [`soubory-spisu-max.html`](soubory-spisu-max.html) | Soubory spisu maximalizované, rozbalený strom | detail spisu, seznamy spisů |
| [`soubory-spisu-mobil.html`](soubory-spisu-mobil.html) | Soubory spisu na mobilu | detail spisu, seznamy spisů |
| [`soubory-spisu-hluboko-mobil.html`](soubory-spisu-hluboko-mobil.html) | Soubory spisu na mobilu, hlubší úrovně | detail spisu, seznamy spisů |
| [`soubory-spisu-tlacitka.html`](soubory-spisu-tlacitka.html) | Tlačítko Soubory v detailu spisu a sloupec Soubory v seznamech spisů | `/cases/{id}`, seznamy spisů |
| [`soubory-spisu-tlacitka-mobil.html`](soubory-spisu-tlacitka-mobil.html) | Řádek Soubory v detailu spisu a dlaždice v seznamu spisů na mobilu | `/cases/{id}`, seznamy spisů |
| [`ikony-souboru.html`](ikony-souboru.html) | Ikony spisu, úkonu a typů souborů | všechny obrazovky |
| [`hledani.html`](hledani.html) | Hledání v horní liště, otevřená nabídka | všechny obrazovky |
| [`kontakty.html`](kontakty.html) | Kontakty | `/contacts` |
| [`detail-kontaktu.html`](detail-kontaktu.html) | Detail kontaktu | `/contacts/{id}` |
| [`nastaveni.html`](nastaveni.html) | Nastavení, sekce štítků | `/settings/labels` |
| [`nastaveni-api-tokeny.html`](nastaveni-api-tokeny.html) | Nastavení, sekce API tokenů | `/settings/api-tokens` |

Text v hranatých závorkách je zástupný: vzorová data pro něj hodnotu nemají. Implementace na
jeho místě ukazuje skutečnou hodnotu, nebo prázdný stav podle SDD dané agendy.

Zdrojem předloh je návrhové plátno v Claude; tyhle soubory jsou jeho vyexportovaná podoba.
Změna vzhledu se dělá tak, že se předloha vymění celá a SDD-020 se upraví ve stejném pull requestu.
