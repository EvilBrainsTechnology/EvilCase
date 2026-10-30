# SDD-023 — MCP server

- **Stav:** platí
- **Milníky:** průřez
- **Související SDD:** [002](sdd-002-logovani-a-observabilita.md), [004](sdd-004-validace-a-chyby.md),
  [005](sdd-005-api-konvence.md), [006](sdd-006-tenance-a-ucty.md), [012](sdd-012-soubory.md)

## Rozsah

MCP server pro AI agenty: přihlášení, nástroje, chyby, jednorázové odkazy na soubory a čtení
textu souboru. Tvary REST API drží SDD-005, API tokeny SDD-006.

## Popis

### Server

- `/mcp` mluví Streamable HTTP bez stavu: každá zpráva je samostatný POST, server nedrží relaci.
- Přihlašuje jen API token v hlavičce `Authorization: Bearer`. Bez tokenu, s cookie přihlášení
  nebo s access tokenem odpoví 401; neznámý, zrušený nebo prošlý token také.
- Požadavek s hlavičkou `Origin` jiného originu než aplikace odpoví 403, i s platným tokenem.
  Požadavek bez `Origin` projde.
- Nástroj jedná jako uživatel tokenu v jeho tenantu, stejně jako REST požadavek s tokenem.

### Nástroje

- Nástroje pokrývají každý zdroj REST API kromě API tokenů a archivu souborů (SDD-005): spisy,
  úkony, kontakty, štítky a jejich přidělení, komentáře, soubory a verze, podpisy a hledání.
  Upload a download obsahu jdou jen jednorázovým odkazem.
- Názvy nástrojů jsou snake_case (`list_cases`, `delete_label`), každý nástroj i parametr nese
  anglický popis.
- Nástroj pro čtení nese `readOnlyHint`, nástroj mazání `destructiveHint`. `openWorldHint` nesou
  jen nástroje, které ověřují podpis online u autority.
- Nástroje berou a vracejí kontraktní DTOs REST API; výpis bere stejnou stránku (`Take` 1 až 100)
  a stejná zúžení.
- Token jen pro čtení vidí ve výpisu nástrojů jen nástroje pro čtení. Volání jiného nástroje
  vrátí chybu nástroje a nic nezmění.
- Chyba validace, nenalezený záznam, konflikt i odepření vrátí chybu nástroje s titulkem
  a detailem Problem Details, jaké vrací REST (SDD-004). Text výjimky se nevrací nikdy.
- Obsah souboru nejde do argumentu nástroje ani z výsledku, kromě textu z `read_file_text`.

### Jednorázové odkazy

- `create_file_upload_link` vydá odkaz na upload nového souboru do spisu nebo úkonu, nebo nové
  verze souboru, pod jménem z argumentu. `create_file_download_link` vydá odkaz na stažení
  verze souboru, bez čísla aktuální; je to nástroj pro čtení.
- Výsledek nese metodu, absolutní adresu, okamžik vypršení a příklad volání curl.
- Adresa je `/api/file-links` se schématem a hostem požadavku na `/mcp`. Tajemství je 256
  náhodných bitů v base64url v parametru `secret`, jméno uploadu v parametru `fileName`.
- Odkaz platí 10 minut a spotřebuje ho první použití, i neúspěšné; ze souběžných použití projde
  jen jedno. Uloží se jen
  SHA-256 tajemství s uživatelem, tenantem, tokenem, cílem a vypršením. Prošlé odkazy se mažou
  každých 10 minut.
- Použití běží bez přihlášení jako uživatel odkazu v jeho tenantu. Cíl musí patřit tenantu při
  vydání i při použití.
- Zrušení tokenu, smazání uživatele nebo tenantu zruší jeho odkazy.
- Upload je PUT surových bajtů. Drží pravidla uploadu (SDD-012): 100 MB, jméno, `MediaType`
  z `Content-Type`, 200 beze změny, 201 se souborem; neplatné jméno odmítne dřív, než odkaz
  spotřebuje.
- Download je GET a odpovídá jako download obsahu s `Cache-Control: no-store`.
- Neznámý, použitý, prošlý nebo jinému účelu vydaný odkaz je 404.
- Adresa odkazu se nedostane do logu (SDD-002).

### Text souboru

- `read_file_text` vrátí text verze souboru po stránkách po 50 000 znacích, stránky od 1,
  s celkovou délkou textu a počtem stránek.
- Text se čte z textových formátů (txt, text, csv, tsv, log, md, json, xml, html, htm, yaml,
  yml) jako UTF-8 bez BOM, z textové vrstvy PDF a z odstavců DOCX. Jiný formát vrátí chybu
  nástroje.
- Soubor nad 30 MB se nečte. Soubor bez textu, třeba sken, to řekne chybou nástroje.

## Rozhodnutí

- Přihlášení: OAuth / API token. Platí API token.
- Relace serveru: se stavem / bez stavu. Platí bez stavu.
- Obsah souboru: v argumentech a výsledcích nástrojů / jednorázovým odkazem. Platí odkaz.
- Tajemství odkazu: v cestě / v query stringu. Platí query string, protože log požadavku nese
  jen cestu.
- Odkaz na stažení: nástroj zápisu / nástroj pro čtení. Platí nástroj pro čtení; token jen pro
  čtení soubor stáhne.
- Text datové zprávy, e-mailu a podepsaného souboru: číst / nečíst. Platí nečíst.

## Dopady

Anonymní endpointy a zdroje drží SDD-005, model mimo tenant SDD-006, log SDD-002, historizaci
SDD-018.
