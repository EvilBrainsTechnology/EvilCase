using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One label on one case XOR one act. Postgres keeps NULLs distinct, so each unique index binds
/// only the rows whose owner column is filled.
/// </summary>
[Index(nameof(TenantId), nameof(LabelId), nameof(CaseId), IsUnique = true)]
[Index(nameof(TenantId), nameof(LabelId), nameof(ActId), IsUnique = true)]
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
public sealed record LabelAssignment : ITenantEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required Guid LabelId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Label? Label { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }
}
