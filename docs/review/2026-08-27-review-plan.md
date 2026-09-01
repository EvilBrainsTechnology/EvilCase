# Plán review — 2026-08-27

Review celého kódu před další prací na funkcích: jednotnost, soulad s pravidly a SDD,
bezpečnost, konzistence UI, složitost, závislosti. Build je čistý (0 warningů), CI na masteru
zelené, `dotnet list package --vulnerable --include-transitive` nehlásí nic, `.env` nikdy
nebyl v gitu. Lokální selhání testů v této session způsobil chybějící Docker (Testcontainers),
ne kód. Rozhodnutí vlastníka z 2026-08-27 jsou zapracovaná; sekce Rozhodnutí nese otázky
i odpovědi, sekce Provedení stav každého nálezu a pull request, který ho nese.

## Shrnutí

Kód je ve velmi dobrém stavu: vrstvení, tenance, autentizace, číslování i testová sada drží
svá SDD; žádný Critical ani High bezpečnostní nález. Největší problémy:

1. Set-based zápisy (`ExecuteUpdate`/`ExecuteDelete`) obcházejí pravidlo vlastnictví řádky
   z SDD-006; vlastník rozhodl opačně — mění se SDD-006, ne kód (R-001).
2. Detail kontaktu neukazuje výskyty přes externí čísla jednací; odkazovaný kontakt může
   ukázat nula výskytů a přesto vrátit 409 při mazání (R-006 — vlastník odložil, Q8).
3. ~600 řádků duplicit ve frontendu a testech s prokazatelně záporným součtem sdílení
   (R-034, R-035, R-037, R-038).
4. SDD ujela v detailech: tabulka stavů chyb, tabulka rout, pravidlo o toastech, login layout
   (R-007 … R-012).
5. ~950 mrtvých řádků v `EvilBrains.Collections` a nepoužitý projekt
   `EvilBrains.EntityFramework` — vlastník rozhodl knihovny ponechat celé
   (R-013, R-014 zamítnuty, Q4).

## Nálezy

### Bezpečnost

**R-001 · Medium · M** — Set-based zápisy nevynucují vlastnictví řádky uživatelem.
Kde: `CaseWriter.cs:111-121,153-155`, `ActWriter.cs:119-128,150-153`, `FileWriter.cs:55-58,82-85`,
`ExternalCaseNumberWriter.cs:54-57`, `ExternalActNumberWriter.cs:60-63`.
SDD-006 rozhoduje, že řádku jiného uživatele v tenantu nejde zapsat, změnit ani smazat;
SDD-018 ukládá `ExecuteUpdate`/`ExecuteDelete` cestám vynucovat, co jinak dělá interceptor.
Uživatele v predikátu zápisu opakuje jen `CommentWriter`. Dnes nedosažitelné (registrace
zavřená, 1 uživatel = 1 tenant), s druhým uživatelem v tenantu jde o IDOR.
Změna (Q1): pět writerů zrcadlí `CommentWriter` — přečíst vlastníka, cizímu odpovědět 403;
řádek 403 do SDD-004 nese R-007. Riziko: dnes žádné; mění budoucí sémantiku 404/403.
Vlastník rozhodnutí otočil při review #457; platí opačné pravidlo, viz rozhodnutí 1.

**R-002 · Low · S** — JSON binding enumů přijímá nedefinované číselné hodnoty.
Kde: `CaseStatus.cs:9`, `ActDirection.cs:9`, `ContactKind.cs:11` (`JsonStringEnumConverter<T>`
má výchozí `allowIntegerValues: true`); např. `CaseEditRequest.Status`.
`{"status": 999}` se naváže, uloží jako řetězec `"999"` a jede ve filtru Otevřené — obchází
validační vrstvu SDD-004. Změna: malý
`StrictJsonStringEnumConverter<T> : JsonStringEnumConverter<T>` s `allowIntegerValues: false`
v `Api.Contract`, na všech třech enumech; test na enum. Riziko: žádné; neznámá jména už dnes
vracejí 400.

**R-003 · Low · S** — Metadata uploadu se na serveru nevalidují.
Kde: `FileTransferController.cs:33-34` → `FileAsset.cs:28,42` (max délky 256/128).
Název souboru přes 256 znaků nebo dlouhý media type skončí `DbUpdateException` → 500 až po
zápisu blobu (útočníkem vyvolatelné osiřelé bloby); prázdný název projde. Změna: před
`StoreFile` ověřit délku a neprázdnost, odpovědět 400; test. Riziko: žádné.

**R-004 · Low · S** — Chybí hlavička `Permissions-Policy`; HSTS na výchozích hodnotách.
Kde: `SecurityHeadersMiddleware.cs:27-37`, `Program.cs:169`.
Změna (Q5): přidat minimální zakazující `Permissions-Policy`
(`camera=(), microphone=(), geolocation=()`), rozšířit `SecurityHeadersTests`; HSTS beze
změny. Riziko: žádné známé; CSP už je těsná.

**R-005 · Low · S** — Rate-limit partice berou každou IPv6 adresu jako samostatnou.
Kde: `Program.cs:219-222`. Jedno /64 dává neomezené login partice; lockout účtu dál omezuje
hádání per účet. Změna: IPv6 partitiovat po /64 prefixu v `ClientAddress`; test.
Riziko: žádné.

### Soulad s SDD

**R-006 · Odloženo (Q8) · kód je špatně (SDD-011)** — Výskytům kontaktu chybí zdroj přes
externí čísla jednací.
Kde: `ContactReader.cs:33-53`, `ContactOccurrenceQuery.cs` (žádný dotaz přes
`ExternalActNumber.AssignedByContactId`); mrtvé lešení `ContactActRole.cs:16` (`NumberIssuer`),
`ContactActRoleDisplay.cs:13`, `ContactActOccurrence.ExternalNumber` vždy null, věčně prázdný
sloupec `ContactActOccurrences.razor:37`.
SDD-011 jmenuje výskyty „úkony přes … externí čísla“; kontakt odkazovaný jen externím číslem
jednacím ukáže nula výskytů, a přesto na mazání odpoví 409. Vlastník doplnění odložil mimo
review (Q8); lešení zůstává pro budoucí implementaci, SDD-011 zůstává cílový stav.
Viz follow-up 5.

**R-007 · SDD zastaralé · S** — Tabulce stavů v SDD-004 chybí stavy, které kód vrací.
403 ne-autor u komentářů (`CommentWriteAnswer.cs:16-17`), 423 lockout (`AuthController.cs:34`),
429 rate limit, 413 upload. Změna: doplnit řádky; „cizí tenant nikdy 403“ zůstává — platí.

**R-008 · SDD zastaralé · S** — SDD-005 nezná implementované routy a výjimku generátoru
klienta.
Endpointy externích čísel (`CasesController.cs:98,127`, `ActsController.cs:99,129`),
`GET /api/contacts/default` (`ContactsController.cs:33`) a ručně psaný `FileTransferClient`
pro multipart/stream (generátor je neumí vyjádřit). Změna: rozšířit tabulku rout; jedna věta
jmenující výjimku file transferu.

**R-009 · rozhodnuto (Q2) · S** — 404 vs. 409 pro neexistující id odkazované v těle.
Neznámý kontakt na create/edit úkonu → 404 (`ActsController.cs:45,76`); neznámý kontakt na
přidání externího čísla → 409 (`CasesController.cs:115-118`); neznámý rodič → 409.
Změna (Q2): sjednotit — chybějící id v těle = 409, id v routě = 404; `ContactNotFound` na
create/edit úkonu přechází na 409; pravidlo do SDD-004. Riziko: změna chování API na
create/edit úkonu; hlášky frontendu se přizpůsobí.

**R-010 · rozhodnuto (Q3) · M** — „selhání sítě ukáže toast“ z SDD-004 není implementované
nikde.
`ToastContainer` je připojený, ale nikdy krmený (`MainLayout.razor:4`; žádné použití
`IToastService`); každé selhání konzistentně vykresluje inline `.empty`/alert. Nejhorší
případ: `ContactPicker.razor:96-102` spolkne selhání sítě do „Žádný kontakt neodpovídá.
Založte nový.“ Změna (Q3): SDD-004 přejímá inline vzor, `ToastContainer` se maže,
`ContactPicker` dostane skutečný stav selhání. Riziko: žádné.

**R-011 · nepřesnost SDD · S** — SDD-016 říká, že každá stránka žije v `MainLayout`;
`Login.razor:2` nutně používá `LoginLayout`. Změna: SDD-016 jmenuje výjimku loginu.

**R-012 · mezera SDD · S** — Hromadné přetažení odmítne dávku přes 100 souborů vcelku
(`FilesCard.razor:119,188-193`); SDD-012 říká, že se odmítá jen soubor, který selže.
Změna: SDD-012 uvede limit dávky (limit zůstává, dokumentuje se).

### Zjednodušení

**R-013 · Zamítnuto (Q4)** — ~95 % `EvilBrains.Collections` aplikace nepoužívá.
Užití je přesně `TrimEmptyToNull` (4 místa), řetězcový `NullIfEmpty` (2 místa),
`AsReadOnlyList` (1 místo). Mrtvé: `Factories/*` (397 řádků), `WhenAllExtensions`,
`StringJoinExtensions`, `AsReadOnlyAsyncExtensions`, `AsAsyncEnumerableExtensions`,
`EmptyIfNullExtensions`, kolekční přetížení `NullIfEmpty` — reinvence BCL (`Task.WhenAll`,
`string.Join`, collection expressions). Vlastník rozhodl knihovnu ponechat celou.

**R-014 · Zamítnuto (Q4)** — `EvilBrains.EntityFramework` je mrtvý projekt.
Nikde žádný using; odkazuje ho jen `EvilCase.Data.csproj:12` a `EvilCase.slnx:33`. Vlastník
rozhodl projekt ponechat.

**R-015 · S** — Mrtvé řádky závislostí mimo Utils.
`EvilCase.Api.csproj:22-23` odkazují `EvilBrains.Collections` a `EvilBrains.Cryptography` bez
jediného using; `Directory.Packages.props:17` pinuje `Microsoft.Extensions.Configuration`,
který žádný projekt nereferencuje. Změna: odstranit. Redundantní `Serilog.Extensions.Hosting`
v `EvilBrains.Logging.AspNetCore.csproj:9` zůstává — Utils se dle Q4 nedotýkáme.
Riziko: žádné.

**R-016 · S** — Mrtvá konfigurace Serilog enricherů.
`appsettings.json:67` jmenuje `WithMachineName`/`WithThreadId`; ani jeden enricher balíček
není referencovaný, položky se nemají na co navázat. Změna: obě položky odstranit (přidání
balíčků se nedoporučuje — nasazení má jeden kontejner). Rezervovaná jména
v `ReservedLogPropertyNames.cs:28-29` (Utils) zůstávají dle Q4. Riziko: žádné.

**R-017 · S** — Drobné foldy v Hostu a bootstrapu.
Parametry s konfigurační cestou s jediným konstantním volajícím (`AddEvilCaseAuth`/
`AddEvilCaseFiles`, `Program.cs:119-120`); privátní jednoužitkový generický
`AddLocalDbContext<TContext>` (`Data/Bootstrap.cs:42-65`); pozůstatek `"AllowedHosts": "*"`
v `appsettings.Development.json:22`. Změna: konstanty vložit dovnitř, helper inlinovat, řádek
smazat. ~12 řádků. Riziko: žádné.

### Jednotnost backendu

**R-018 · S/M** — Case a act trojice v `CommentWriter` jsou téměř identické (~58 duplikovaných
řádků, `CommentWriter.cs:14-71` vs. `:73-130`). Změna: privátní jádra (add/edit/remove)
beroucí probe vlastníka a scopované `IQueryable`; veřejné metody zůstanou obálky. ~−40 řádků.

**R-019 · S** — Delete dvojice ve `FileWriter` identická až na jeden krok dotazu
(`FileWriter.cs:41-66` vs. `:68-93`). Změna: privátní `DeleteFile(IQueryable<FileAsset>, …)`.
~−20 řádků.

**R-020 · S** — Upload preambule ve `FileTransferController` zduplikovaná doslova
(`FileTransferController.cs:25-36` vs. `:56-67`). Změna: privátní `BuildUpload(IFormFile)`.
~−12 řádků.

**R-021 · S** — Probe existence vlastníka vypsaný 11×
(`Cases.WithId(caseId).AnyAsync` 5×, `Acts.OfCase(caseId).WithId(actId).AnyAsync` 6×).
Změna: dva kroky dotazu vedle `EntityQuery`. ~−10 řádků, jedno znění pravidla vlastnictví.

**R-022 · S** — `ContactOccurrenceQuery.cs:48-61` vs. `:63-76` se liší jen konstantou `Role`.
Změna: jeden `AsActOccurrences(role)`. ~−12 řádků.

**R-023 · S** — `ExternalActNumberQuery.OfAct(actId)` scopuje jen přes úkon, zatímco soubory
a komentáře přes `(caseId, actId)`; vynucuje probe navíc
v `ExternalActNumberWriter.cs:56-58`. Změna: `OfAct(caseId, actId)` přes `Act!.CaseId`; probe
zaniká. ~−5 řádků.

**R-024 · S** — Titulky problémů inlinované jako literály tam, kde existují konstanty:
„Case not found“ 7×, „Contact not found“ 3×, „File not found“ 3×
(např. `CasesController.cs:55,71,93,114`, `ContactsController.cs:47,62,78`); App na tyto
řetězce matchuje. Změna: konstanty v `CaseProblems`/`ContactProblems`/`FileProblems`;
`ActProblems` si nechá jen své.

**R-025 · S** — Precedence outcome při update se liší: `ActWriter.cs:91-97` kontroluje
existenci první (pravidlo stojí v komentáři); `CaseWriter.cs:85-99` validuje číslo první,
takže chybějící spis se špatným číslem odpoví 400. Změna: existence první i v `UpdateCase`;
test chování. Riziko: změna chování API v jednom okrajovém případě.

**R-026 · S** — Create ukládá netrimovaný text, update trimuje (`CaseWriter.BuildCase`,
`ActWriter.BuildAct` vs. obě Update cesty). Změna: trim i v obou build metodách; jednovolající
`CaseWriter.Normalize` zaniká (pravidlo jednoho místa volání v code.md) — oba writery dostanou
týž inline tvar; test. Riziko: žádné.

**R-027 · S** — `ContactWriter.cs:60` používá inline predikát a jméno `entity` místo
`.WithId`; `:25-27` čte `dbSession.Current` dvakrát. Změna: `.WithId(contactId)`, lokální
`context`. ±0 řádků.

**R-028 · S** — Šest tříd s kroky dotazů je `public` (`ActListQuery`, `CaseListQuery`,
`ContactListQuery`, `CaseNumberQuery`, `ActNumberQuery`, `EntityQuery`), ač je konzumují jen
testy přes `InternalsVisibleTo`; osm sourozenců je `internal`. Změna: všechny internal.

**R-029 · S** — Všech 11 entitních recordů je nezapečetěných, zatímco každý kontraktní record
je sealed; z žádné entity nic nedědí. Změna: zapečetit.

**R-030 · S** — Mrtvá ramena switchů: explicitní `NotAuthor => throw new
UnreachableException()` duplikuje discard rameno hned pod ním (`CaseCommentsController.cs:34`,
`ActCommentsController.cs:36`). Změna: smazat. −2 řádky.

**R-031 · rozhodnuto (Q7) · S** — Pět bajtově identických outcome enumů `{Deleted, NotFound}`
(`CaseDeleteOutcome`, `ActDeleteOutcome`, `FileDeleteOutcome`, oba delete externích čísel).
Změna (Q7): jeden sdílený `DeleteOutcome`; −4 soubory, ~−32 řádků.

**R-032 · S** — Logování writerů je nevyrovnané: `ContactWriter` neloguje nic,
`CommentWriter` vynechává update, writery externích čísel vynechávají delete, zbytek loguje
každou mutaci. Změna: logovat každou business mutaci (drží styl identifikátorů z SDD-002).
~+6 řádků.

**R-033 · S** — Pojmenování outcome „chybí vlastník“ se liší: `CaseNotFound`/`ActNotFound`
vs. `UploadFileOutcome.OwnerNotFound` vs. `CommentWriteOutcome.NotFound`. Změna: členy
pojmenované vlastníkem všude; jen renamy, spolu s R-031.

### Testy

**R-034 · S** — Bajtově identický 13řádkový `AssertProblem` v 8 controller fixture
(`CasesControllerTests.cs:402` a 7 dalších). Změna: jeden sdílený helper. ~−90 řádků.

**R-035 · M** — SetUp/TearDown lešení `TestTenant` se opakuje ve 26 fixture
(`CommentWriterTests.cs:22-33` a `ActCommentWriterTests.cs:22-33` jsou doslova identické);
`UserWriteInterceptorTests` opakuje 3řádkový arrange ve všech 10 testech. Změna: malá base
fixture držící životní cyklus tenanta; pole v interceptor fixture. ~−280 řádků.
Riziko: dotýká se 26 souborů; mechanické.

**R-036 · S** — Utils fixtures používají sufix `…Test` u metod, který zbytek řešení nemá
(`DbSetAccessAnalyzerTests.cs:11-129` a další); jeden helper se jmenuje `Foo`
(`AwaitEnumerableTests.cs:29`). Změna: přejmenovat.

### Frontend

**R-037 · M** — Šest delete modálů sdílí 39řádkový rám (celkem 434 řádků,
`ActDeleteModal.razor` … `FileDeleteModal.razor`). Změna: sdílený `ConfirmDeleteModal`
(zpráva jako child content, delete delegát, chybové texty, volitelný extra mapper
`ApiException`); šest tenkých obálek. ~−190 řádků. Riziko: chování modálů musí zůstat
identické — busy stav, zavření po úspěchu, hlášky NotFound/Forbidden/Conflict.

**R-038 · M** — `ContactCreateModal` a `ContactEditModal` jsou téměř kopie (213 řádků,
48 řádků markupu identických). Změna: jeden `ContactFormModal` s volitelným `ContactDetail?`.
~−90 řádků.

**R-039 · S/M** — Trojice notFound/failure/spinner se doslova opakuje na 5 stránkách detailu
a editace (95 řádků). Změna: komponenta `LoadStateView`. ~−30 řádků. Až po R-037, který založí
vzor.

**R-040 · S** — `Contacts.razor:54-66` je jediný `QuickTable` (s komentovanou křehkou vazbou
na pořadí sloupců); každý jiný seznam je prostá Tabler `<table>`. Změna: prostá tabulka i na
kontaktech; asc/desc přepínač na názvu zaniká (nikde jinde není). Riziko: ztráta toho
přepínače.

**R-041 · S** — `app.css:68` říká `.list-group-item-actions` (množné číslo); skutečná třída
je `list-group-item-action`, takže výsledky `ContactPickeru` unikají pravidlu 44px touch
targetů. Změna: smazat `s`.

**R-042 · S** — `UserMenu.razor:9,19` větví na `xl`; app.md povoluje jen `lg`. Změna: `lg`.

**R-043 · S** — `PageTitle` chybí na `Home`, `Cases`, `Contacts`, `NewCase`, `NewAct`.
Změna: řádek na stránku.

**R-044 · S** — `NewAct.razor:132-137` nemapuje 404, takže smazaný vybraný kontakt dá
generickou chybu; `EditAct.razor:227-232` oba 404 titulky rozlišuje. Změna: týž catch
v NewAct.

**R-045 · S** — Editační formulář komentáře (`CommentsCard.razor:57-60`) obrací pořadí
Zrušit/Uložit a nemá busy spinner, který má každý jiný formulář. Změna: přeuspořádat, přidat
spinner.

**R-046 · S** — Popisky pickerů v `NewAct.razor:54,60` nemají `for`; `EditAct` je má.
Změna: doplnit.

**R-047 · S** — Drift české terminologie: Odesílatel/Příjemce vs. Od/Komu
(`ActListCard.razor:81,84`) vs. Vydal/Adresát (`ContactActRoleDisplay.cs:11-12`);
„ID datové schránky“ vs. „Datová schránka“; dvě formulace placeholderu hledání; modál
„Nový kontakt“ potvrzuje „Uložit“, kde ostatní zakládání říkají „Založit …“. Změna:
odesílatel/příjemce všude, „ID datové schránky“, jedna formulace placeholderu,
„Založit kontakt“.

**R-048 · S** — Drift prázdných stavů (věta s tečkou jako titulek
v `ContactCaseOccurrences.razor:11`, `FilesCard.razor:60` vs. jmenné popisky jinde); delete
tlačítka řádků prostá v `ExternalNumbersCard.razor:43,60` vs. `btn-danger` jinde.
Změna: obojí srovnat.

**R-049 · S** — `ContactCaseOccurrences.razor:31-32` linkuje číslo spisu, každá jiná tabulka
linkuje název. Změna: linkovat název.

### Pravidla

**R-050 · S** — Dva řádky `code.md` potřebují přeformulovat, bez nových řádků: pravidlo
identifikátorů nezná výjimku klíče entity (všech 11 entit správně deklaruje holé `Id`) —
doplnit „; an entity's key property is `Id`“; pravidlo assertion message neříká, kdy je
message povinná (půlka sady žádnou nemá) — doplnit „where the assert alone does not say it“.

**R-051 · rozhodnuto (Q9) · S** — Nezapsané konvence, které kód všude drží.
Vlastník povolil zvednout `maxLinesTotal` v `.claude/instruction-limits.json` z 539 na 550;
přidává se pět pravidel (znění každého se před commitem ověří proti kódu):

- business.md: `A business write returns an outcome enum named <Entity><Verb>Outcome; the
  action maps every member and throws UnreachableException for the rest.` — zachytí
  pojmenování `UploadFileOutcome` (R-033).
- api.md: `A contract DTO is a sealed record; every property is required with init.`
- code.md: `A test name is a declarative sentence in PascalCase; no Test suffix, no
  underscores.` — R-036 pak drží pravidlo.
- code.md: `A test double records under Recording*, behaves under Fake*, returns fixtures
  under Stub*; shared only once two fixtures use it.`
- code.md: `A database test runs under its own TestTenant; the fixture's doc comment names
  the rules it pins.`

## Rozhodnutí

Zodpovězeno vlastníkem 2026-08-27:

1. **R-001, vynucení vlastnictví v set-based zápisech** — vlastník rozhodnutí otočil při
   review #457: uvnitř tenantu smí kterýkoli uživatel entitu změnit i smazat. Vynucení se
   ruší a mění se věta v SDD-006. Komentář zůstává jen autorovi (SDD-013). Řádek 403
   v SDD-004 (R-007) platí dál — nese ho právě komentář.
2. **R-009, chybějící id odkazované v těle** — 409 pro id v těle, 404 pro id v routě;
   `ContactNotFound` na create/edit úkonu přechází na 409; pravidlo do SDD-004.
3. **R-010, UX selhání sítě** — platí inline vzor; SDD-004 se mění, `ToastContainer` se maže,
   `ContactPicker` dostane stav selhání.
4. **R-013/R-014, ořez Utils** — zamítnuto: vše v Utils zůstává. R-015 a R-016 zúženy tak,
   aby se Utils nedotýkaly.
5. **R-004, security hlavičky** — `Permissions-Policy` se přidá; HSTS beze změny.
6. **Editace externích čísel** — zatím platí smazat-a-přidat; editaci vlastník přidá později,
   mimo toto review. V rámci review bez změny SDD i vize.
7. **R-031, delete outcome enumy** — sloučit do jednoho sdíleného `DeleteOutcome`.
8. **R-006, výskyty přes externí čísla jednací** — odloženo: zdroj se teď nedoplňuje, lešení
   zůstává, SDD-011 zůstává cílový stav.
9. **R-051, limit instrukcí** — `maxLinesTotal` se zvedá na 550 a navržená pravidla se
   přidávají.

## Provedení

Provedeno 2026-08-27 ve třech stopách stacked pull requestů; každý staví na předchozím a CI je
u všech zelená. Slití obou kódových stop bylo ověřeno nanečisto: automatické, jediný sdílený
soubor `EditAct.razor`, sloučený výsledek buildí bez warningů.

### Backend

| # | PR | Nálezy |
| --- | --- | --- |
| B1 | #448 | R-002 |
| B2 | #450 | R-003 |
| B3 | #452 | R-005 |
| B4 | #454 | R-004 |
| B5 | #457 | R-025 |
| B6 | #459 | R-009, R-044 |
| B7 | #460 | R-015, R-016, R-017 |
| B8 | #462 | R-018, R-019, R-020 |
| B9 | #463 | R-021, R-022, R-023 |
| B10 | #464 | R-024 |
| B11 | #465 | R-026, R-027 |
| B12 | #466 | R-028, R-029 |
| B13 | #467 | R-031 |
| B14 | #468 | R-032 |
| B15 | #469 | R-034, R-035 |

### Frontend

| # | PR | Nálezy |
| --- | --- | --- |
| F1 | #447 | R-037 |
| F2 | #449 | R-038 |
| F3 | #451 | R-039 |
| F4 | #453 | R-010 (frontend) |
| F5 | #455 | R-040 |
| F6 | #456 | R-041, R-042, R-046 |
| F7 | #458 | R-043, R-045, R-048, R-049 |
| F8 | #461 | R-047 |

### Dokumentace

| # | PR | Nálezy |
| --- | --- | --- |
| D1 | #470 | R-007, R-008, R-010 (SDD), R-011, R-012 |
| D2 | #471 | R-050, R-051 |

### Stav nálezů

Hotovo: R-002 až R-005, R-007 až R-012, R-016 až R-029, R-031, R-032, R-034, R-035,
R-037 až R-050.

Neprovedeno, s důvodem:

- **R-001** — zamítnuto vlastníkem při review #457 (rozhodnutí 1): uvnitř tenantu smí
  kterýkoli uživatel entitu změnit i smazat. Vynucení se z B5 odstranilo, SDD-006 se opravilo;
  B5 nese už jen R-025.
- **R-006** — odloženo vlastníkem (rozhodnutí 8). Lešení v kódu zůstává, SDD-011 zůstává cílem.
- **R-013, R-014** — zamítnuto vlastníkem (rozhodnutí 4): `src/Utils/**` zůstává celé.
- **R-015** — provedeno mimo `src/Utils/**`; redundantní reference `Serilog.Extensions.Hosting`
  zůstává podle téhož rozhodnutí.
- **R-030** — zamítnuto při provádění: smazání explicitní větve `NotAuthor` shodí build na
  IDE0072, který vynucuje pojmenování každého členu enumu i vedle discard větve. Stejný tvar
  nese sdílený `CommentWriteAnswer`, takže jde o konvenci, ne o mrtvý kód.
- **R-033** — zamítnuto při provádění: navržené pravidlo už platí. Enum sloužící jednomu
  vlastníkovi ho jmenuje, enum sloužící dvěma je generický; `UploadFileOutcome.OwnerNotFound`
  je proto správně a přejmenování by vrátilo mrtvou větev, kvůli které vznikl.
- **R-036** — mimo rozsah: leží v `src/Utils/**` (rozhodnutí 4). Čeká na vlastníkovo slovo.
- **R-051** — přidána dvě pravidla z pěti. Zamítnuta: kontraktní DTO nemají všechny vlastnosti
  povinné (`CaseListRequest`, `ActListRequest` a `ContactListRequest` nesou volitelné filtry);
  pravidlo o pojmenování testů a pravidlo o pojmenování test doubles neplatí v
  `src/Utils/Tests/**`, tedy padají spolu s R-036.

Limit instrukcí zvednut na 550 řádků (rozhodnutí 9); po přidání pravidel 542.
## Co záměrně neměníme

- `ExternalCaseNumberWriter` vs. `ExternalActNumberWriter` (~90 % identické, po 38 řádcích):
  dvě místa, pod vlastním prahem 3+; generický fold stojí víc, než ušetří. Třetí vlastník
  externích čísel misku převáží.
- Dvojice case/act controllerů: vynucené šablonami rout a generátorem klienta; sdílený switch
  outcome už je vytažený (`CommentWriteAnswer`).
- `GET /api/auth/sessions` vrací holý seznam místo `*ListResponse`: změna wire formátu bez
  užitku uvnitř uzavřeného auth modulu.
- Ořez `EvilBrains.Collections` a smazání `EvilBrains.EntityFramework`: zamítnuto vlastníkem
  (Q4) — knihovny zůstávají celé, včetně redundantní reference `Serilog.Extensions.Hosting`
  a rezervovaných jmen enricherů.
- Vlastní `PasswordHasher`: PBKDF2 s parametry dle OWASP, constant-time porovnání, timing
  decoy, testovaný; přechod na Identity je změna autentizace bez přínosu.
- Generátor API klienta: explicitní rozhodnutí SDD-001; náhrada je přepis fungujícího.
- Ručně psané test doubles (žádná mock knihovna): záměrné, konzistentně umístěné; knihovna je
  rozhodnutí o závislosti se stylovou cenou.
- `IUserContext.UserIdOrDefault`, `ClientInfo.Unknown`, `Parse` poloviny číslovacích typů:
  posvěcená symetrie (pravidlo `Parse`/`ParseOrDefault`) nebo povrch užívaný testy.
- Podobnost `@code` hledání na Cases/Contacts: sdílená abstrakce je nula od nuly (~45 řádků
  ušetřených, ~40 přidaných).
- Rozdílné krájení souborů writer testů: churn bez obsahového zisku.
- `EvilBrains.Dispose` jako projekt s jednou třídou: fold do konzumenta kříží hranici
  Utils nezávislých na EvilCase.
- Model důvěry forwarded headers a mez záplavy anonymního uploadu logů: dokumentované
  kompromisy nasazení (`Program.cs:51-64,79`); viz follow-upy.
- E-mail seedovaného administrátora ve startovním logu: identifikátor operátora, v mezích
  SDD-002.
- Agenda `/contacts` bez tlačítka založení: SDD-011 definuje agendu jako přehled + detail;
  kontakty vznikají inline tam, kde jsou jmenovány, a pravidlo výzvy k založení v SDD-016
  platí jen tam, kde záznam založit jde.
- `dotnet r ci` vs. „nobody runs a local gate“ v github.md: dostupnost není povinnost; beze
  změny.

## Doporučené follow-up issues (mimo toto review)

1. Kvóta úložiště per tenant: jeden účet umí zaplnit sdílený svazek `files` i databázi
   (100 MB × neomezené uploady, neomezená těla komentářů) — produktové rozhodnutí.
2. Síťová záruka pro `BehindReverseProxy=true` (interní compose síť nebo autentizovaný hop
   proxy), aby „nedosažitelné jinak než přes proxy“ bylo vynucené, ne předpokládané.
3. `TabBlazor 0.15.48-beta`: pre-release UI závislost v produkční aplikaci; sledovat upstream.
4. Editace externích čísel: vlastník přidá později (rozhodnutí 6).
5. Výskyty kontaktu přes externí čísla jednací (R-006): vlastník doplní později
   (rozhodnutí 8).
6. Pojmenování testů a test doubles v `src/Utils/Tests/**` (R-036): rozhodnout, zda se Utils
   srovnají se zbytkem řešení. Dokud ne, nejdou zapsat dvě pravidla z R-051.
