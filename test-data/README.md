# Test data

Hand-built fixtures describing whole case files, in a shape a script can later turn into an SQL seed.
Not code, not a fixture the test suite reads — a description of what one real case file looks like, so
the domain model can be checked against reality before there is any importer.

## Provenance

`case-01-speeding.md` was derived from one real case folder on the owner's disk, read once, read-only,
on 2 August 2026. The folder was not modified and is not in this repository.

Two kinds of fact went in:

- **From document content.** Parties, file numbers (*čísla jednací*), file marks (*spisové značky*),
  dates and the act's own description come from reading the `.docx` submissions — the text says what
  the document is, which is what makes this a semantic classification rather than a reading of names.
- **From the folder structure.** Ordinals, attachment markers and the sub-case tree. Incoming
  documents arrive as PDFs only and no text could be extracted from them here, so what an incoming act
  *is* was taken from its name and is marked as such.

## Pseudonymised, deliberately

Every person, authority, address, identifier and file mark is replaced by a stable placeholder.
`docs/product/vision.md` keeps real case content out of this repository, which is public, and the real
folder carries more than the owner's own data — officials and a lawyer are named in it, and they did
not consent to anything.

The substitution is consistent within the file, so the shape, the cross-references and the act
sequence are exactly the real ones. Nothing about the structure was simplified.

| Placeholder | Stands for |
| --- | --- |
| `Ing. Petr Vzorek` | the person the case belongs to |
| `Městský úřad Vzorov` | the first-instance authority |
| `Krajský úřad Vzorového kraje` | the appellate authority |
| `Krajský soud ve Vzorově` | the administrative court |
| `MUVZ/2025/…`, `KUVZ …/2025`, `10 A 1/2025` | file numbers and marks |

## Turning it into SQL

Not written yet. The intended path is a script that reads these files and emits inserts for `Cases`,
`CaseTags`, `Parties` and — once they exist — acts, file assets and comments. The tables the model has
today are only a part of what a case file needs, which is the point of writing this down first.
