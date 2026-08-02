# Case 01 — speeding, operator's liability

One real case file, pseudonymised (see `README.md`). It runs from an automated speed measurement
through a first-instance penalty, an appeal, and an administrative action still open at the court —
with seven parallel sub-case lines hanging off it, most of them freedom-of-information requests used
to gather evidence for the main line.

**Root folder** `2025-05-28 - Speeding Vzorov - 121 in 110`
**Status** `Active` · **Tags** `speeding`, `operator-liability`, `information-request`

## Parties

| Key | Kind | Name | Role in this case |
| --- | --- | --- | --- |
| `subject` | Person | Ing. Petr Vzorek | the operator of the vehicle; appellant, claimant |
| `first-instance` | Authority | Městský úřad Vzorov, odbor vnitřních věcí | issued the order and the decision |
| `appellate` | Authority | Krajský úřad Vzorového kraje | decided the appeal; defendant in the court action |
| `court` | Authority | Krajský soud ve Vzorově | hears the administrative action |
| `police` | Authority | Policie Vzorového kraje | operated the measurement |
| `ministry-transport` | Authority | Ministerstvo dopravy | answered an information request |
| `ministry-interior` | Authority | Ministerstvo vnitra | non-pecuniary damage claim, review petition |
| `roads` | Authority | Ředitelství silnic a dálnic | answered an information request |
| `bar` | Authority | Česká advokátní komora | received a disciplinary petition |
| `mayor` | Official | starosta Městského úřadu Vzorov | appealed the offence decision against him |
| `officer` | Official | pověřená úřední osoba | handled the information requests |

## File marks

| Mark | Assigned by | What it identifies |
| --- | --- | --- |
| `VV41/2025/08464` | `first-instance` | the administrative proceeding (internal) |
| `MUVZ/2025/80535` | `first-instance` | the order (*příkaz*) |
| `MUVZ/2025/82743` | `first-instance` | notice of continuation of the proceeding |
| `MUVZ/2025/93547` | `first-instance` | the first-instance decision |
| `KUVZ 109838/2025` | `appellate` | the appeal decision |
| `10 A 1/2025` | `court` | the administrative action |

## Acts

Direction is from the subject's point of view: **out** is filed by them, **in** arrives at them.
*Classified from* says whether the act's kind came from reading the document or only from its name.

| # | Direction | Act | Date | Mark | Files | Classified from |
| --- | --- | --- | --- | --- | --- | --- |
| 01 | in | Call to pay a determined sum (operator's liability) | — | — | pdf | name |
| 02 | in | Order imposing a penalty (*příkaz*) | 2025-07-31 | `MUVZ/2025/80535` | pdf | content of act 03 |
| 03 | out | Objection against the order (*odpor*), which annuls it in full | 2025-08-04 | `VV41/2025/08464` | docx + pdf | content |
| 04 | in | Notice of continuation and invitation to comment on the evidence | 2025-08-06 | `MUVZ/2025/82743` | pdf | content of act 05 |
| 05 | out | Comment on the evidence before the decision | 2025-08-18 | `VV41/2025/08464` | docx + pdf | content |
| 06 | in | First-instance decision — guilty, fine 2 000 CZK, costs 2 500 CZK | 2025-09-02 | `MUVZ/2025/93547` | pdf | content of act 07 |
| 07 | out | Appeal against the decision, in full scope | 2025-09 | `MUVZ/2025/93547` | docx + pdf | content |
| 08 | in | Appeal decision | 2025-09-26 | `KUVZ 109838/2025` | pdf | content of act 09 |
| 09 | out | Administrative action, with an application for suspensive effect | 2025-10 | `10 A 1/2025` | docx + pdf | content |
| 10 | in | Instruction on possible bias of the bench | — | `10 A 1/2025` | pdf | name |
| 11 | in | Order to pay the court fee | 2025-10-09 | `10 A 1/2025-31` | pdf | content of act 16 |
| 12 | out | Reply to the instruction, consent to a decision without a hearing, statement of costs | 2025-10-07 | `10 A 1/2025` | docx + pdf | content |
| 13 | out | Supplement to the action — the material element of the offence | — | `10 A 1/2025` | docx + pdf | content |
| 13 | out | Supplement to the action — unlawfulness of the speed measurement | — | `10 A 1/2025` | docx + pdf | content |
| 14 | in | Refusal of suspensive effect | — | `10 A 1/2025` | pdf | name |
| 15 | in | Letter from the court | — | `10 A 1/2025` | pdf | name |
| 15 | in | Defendant's statement of case | — | `10 A 1/2025` | pdf | name |
| 16 | out | Statement of the costs of proceedings | 2025-10-18 | `10 A 1/2025` | docx + pdf | content |
| 17 | out | Supplement to the action — contradiction between decisions, reply to the defendant | — | `10 A 1/2025` | docx + pdf | content |
| 18 | out | Request for an extension to supplement the action | — | `10 A 1/2025` | docx + pdf | content |
| 19 | out | Second request for an extension | 2025-11-25 | `10 A 1/2025` | docx + pdf | content |
| 20 | out | Third request for an extension | 2026-12-30 *(as written in the document)* | `10 A 1/2025` | docx + pdf | content |
| 21 | out | Final supplement to the action | 2026-03-13 | `10 A 1/2025` | docx + pdf, + `21a` evidence bundle (zip) | content |

## Sub-cases

Nine of these are information requests filed to build evidence for the main line; the rest are
separate proceedings that branch off it. Closed ones are marked as such in the folder name.

| Ordinal | Sub-case | Closed | Acts | Against |
| --- | --- | --- | --- | --- |
| 01 | Information request — Ministry of Transport | yes | 4 (+7 attachments incl. the contract and the site plan) | `ministry-transport` |
| 01 | Information request — Police | yes | 6 | `police` |
| 01 | Information request — Vzorov 1 | yes | 2 | `first-instance` |
| 01 | Information request — Vzorov 2, file index sheet | yes | 5 | `first-instance` |
| 01 | Information request — Vzorov 3 | yes | 17, incl. a complaint of inaction and two measures against inaction | `first-instance`, `appellate` |
| 01 | Information request — Vzorov 4, letter | yes | 4 | `first-instance` |
| 01 | Information request — Vzorov 4, letter (regional authority) | yes | 2 | `appellate` |
| 01 | Information request — Vzorov 5, instructions to the authorised officer | yes | 2 | `first-instance` |
| 01 | Information request — Road authority | yes | 2 | `roads` |
| 02 | Pre-action notice | no | 2 (+2 attachments) | `first-instance` |
| 03 | Open letter to the council | no | 1 (+3 attachments) | `first-instance` |
| 04 | Non-pecuniary damage — Ministry of the Interior | no | 4 (+4 attachments), plus a nested information request of its own | `ministry-interior` |
| 05 | Offence report against the mayor | no | 18, incl. the mayor's own appeal and a review petition | `first-instance`, `ministry-interior` |
| 06 | Disciplinary petition to the Bar | no | 4 (+7 attachments) | `bar` |
| 07 | Legal expenses insurance claim | no | 1 (+9 attachments) | insurer |

## What this case says about the model

Written down because it is what the exercise was for. Each of these is a real property of a real case
file that the model as it stands today does not hold.

1. **Two acts can share one ordinal.** Ordinals 13 and 15 each cover two unrelated documents — two
   separate supplements to the action, and a court letter alongside the defendant's statement. The
   ordinal is not a key.
2. **Sub-cases carry ordinals too, and they repeat.** Nine sub-cases are all `01`. The number groups a
   *line* of related sub-cases rather than ordering them, so a sub-case ordinal is neither unique nor
   a sort key — decision #67 chose creation order for display, which this does not contradict, but the
   number is information the model currently drops.
3. **Some folders are not sub-cases.** `Spis` and `Poskytnuté informace` hold a bundle of documents
   belonging to their parent — an authority's own file, handed over. Treating every sub-folder as a
   sub-case would invent two cases that do not exist.
4. **Sub-cases nest inside sub-cases.** The damage claim contains an information request of its own,
   which in turn contains a bundle folder.
5. **Not every file carries an ordinal.** The insurance sub-case names files `Oznámení škodné
   události.docx` and `Příloha 01 - …`, with no number at all. The parser as written reports every one
   of them as unreadable.
6. **`99` is a folder here, not a file.** It holds ten generated summary PDFs. The rule settled in #75
   was about files.
7. **The same document appears in many places.** One letter shows up as an attachment in five
   different sub-cases. This is exactly the case content-addressing is for, and it is common rather
   than rare.
8. **An attachment's title is often just a date** — `03a - 30.04.2024.pdf` — so an attachment needs its
   parent act for any of it to make sense.
9. **`.zfo` envelopes appear as acts and as attachments**, sometimes both for the same message.
10. **Delivery receipts arrive as separate files** named `dorucenka_<id>.pdf`, with no ordinal and no
    obvious link to the act they belong to except the id.
