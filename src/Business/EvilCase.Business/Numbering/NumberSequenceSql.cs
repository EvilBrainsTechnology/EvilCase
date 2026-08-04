namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Advancing a series is one statement. Two callers on the same series in the same second serialise on
/// the row the upsert touches, and each takes a value of its own; reading the counter and writing it
/// back would hand both the same one.
/// </summary>
public static class NumberSequenceSql
{
    /// <summary>
    /// Parameters: the owner, then the scope. Returns the value taken.
    /// </summary>
    public const string TakeNext = """
        WITH "taken" AS (
            INSERT INTO "NumberSequences" ("OwnerId", "Scope", "LastValue")
            VALUES ({0}, {1}, 1)
            ON CONFLICT ("OwnerId", "Scope")
            DO UPDATE SET "LastValue" = "NumberSequences"."LastValue" + 1
            RETURNING "LastValue"
        )
        SELECT "LastValue" AS "Value" FROM "taken"
        """;
}
