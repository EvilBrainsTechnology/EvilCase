# Vizuální předloha

Statické HTML předlohy obrazovek, ze kterých se implementuje vzhled podle
[SDD-020](../sdd/sdd-020-vzhled.md). Jeden soubor je jedna obrazovka v šířce 1440 px.
Soubory jsou závazné na hodnoty: barvu, rozestup, rádius, velikost písma a ikonu čti odsud,
nedomýšlej je. Stavy, které předloha neukazuje (prázdný seznam, chyba, načítání, mobil),
řídí SDD-020 a SDD dané agendy.

Předlohy jsou statické: odkazy mezi nimi vedou na sousední předlohu, formuláře nic neodesílají,
data jsou pseudonymizovaný případ z [`test-data/case-01-speeding.md`](../../test-data/case-01-speeding.md).
Otevři je v prohlížeči přímo ze souboru.

| Soubor | Obrazovka | Routa |
| --- | --- | --- |
| [`prihlaseni.html`](prihlaseni.html) | Přihlášení | `/login` |
| [`prehled.html`](prehled.html) | Přehled | `/` |
| [`spisy.html`](spisy.html) | Seznam spisů | `/cases` |
| [`novy-spis.html`](novy-spis.html) | Založení spisu, předloha i pro editaci | `/cases/new`, `/cases/{id}/edit` |
| [`detail-spisu.html`](detail-spisu.html) | Detail spisu | `/cases/{id}` |
| [`detail-ukonu.html`](detail-ukonu.html) | Detail úkonu | `/cases/{id}/act/{actId}` |
| [`novy-ukon.html`](novy-ukon.html) | Založení úkonu, předloha i pro editaci | `/cases/{id}/act/new`, `/cases/{id}/act/{actId}/edit` |
| [`kontakty.html`](kontakty.html) | Kontakty | `/contacts` |
| [`detail-kontaktu.html`](detail-kontaktu.html) | Detail kontaktu | `/contacts/{id}` |
| [`nastaveni.html`](nastaveni.html) | Nastavení, sekce štítků | `/settings` |

Text v hranatých závorkách je zástupný: vzorová data pro něj hodnotu nemají. Implementace na
jeho místě ukazuje skutečnou hodnotu, nebo prázdný stav podle SDD dané agendy.

Zdrojem předloh je návrhové plátno v Claude; tyhle soubory jsou jeho vyexportovaná podoba.
Změna vzhledu se dělá tak, že se předloha vymění celá a SDD-020 se upraví ve stejném pull requestu.
