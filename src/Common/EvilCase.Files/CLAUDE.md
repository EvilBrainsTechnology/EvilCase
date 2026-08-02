# File storage

`EvilCase.Files` holds a file asset's bytes behind `IFileStore`, registered by `AddEvilCaseFiles` from `EvilBrains:EvilCase:Files`. Nothing outside that namespace learns where they are.

- **Content-addressed.** What identifies stored content is its SHA-256, so storing the same bytes twice is one write and one copy — `StoredFile.AlreadyPresent` says which happened, and that is what makes running an import twice safe.
- **Local disk, at `<root>/<first two hex characters>/<hash>`.** `RootPath` is resolved against the content root, so the default (`App_Data/files`) works from a clone and a deployment points it at a mounted volume. The fan-out directory is not decoration: one real case file is around three hundred documents.
- **A write is atomic** — written under a `.pending-*` name and moved into place. A hash promises exact bytes, so a reader must never meet a half-written file sitting under one. Two callers storing identical content race at the move, and the loser reports `AlreadyPresent`.
- **Missing content is an answer, not an exception.** `Open` returns null; a database row can outlive what it pointed at. A hash that is not 64 lower-case hex characters *is* an exception, because it means a caller lost track of what it was holding.
- **From here on the database alone is not a backup.** Documents are in the volume and the database holds only metadata pointing at them; `deploy/README.md` says so where the stack is described.
