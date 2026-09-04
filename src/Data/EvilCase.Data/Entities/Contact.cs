using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(TenantId))]
public sealed record Contact : ITenantEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required ContactKind Kind { get; init; }

    [MaxLength(256)]
    public required string Name { get; init; }

    [MaxLength(1024)]
    public string? Address { get; init; }

    /// <summary>
    /// The ISDS identifier, seven characters today.
    /// </summary>
    [MaxLength(16)]
    public string? DataBoxId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public ICollection<Act> Acts { get; init; } = [];

    public ICollection<Case> Cases { get; init; } = [];
}
