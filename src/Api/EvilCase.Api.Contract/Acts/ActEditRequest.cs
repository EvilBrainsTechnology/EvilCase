using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActEditRequest : IValidatableObject
{
    /// <summary>
    /// Hand-written over the issued one. The format and the tenant's uniqueness hold; the day inside it
    /// is not tied to <see cref="Date"/> (SDD-008).
    /// </summary>
    [Required]
    [StringLength(128)]
    public required string ActNumber { get; init; }

    /// <summary>
    /// The reference number another authority gave this act; optional free text (SDD-010).
    /// </summary>
    [StringLength(128)]
    public string? ExternalActNumber { get; init; }

    public ActDirection? Direction { get; init; }

    /// <summary>
    /// The act's own date. Moving it leaves the number as it was issued.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    /// <summary>
    /// The counterparty of the act; set exactly when <see cref="Direction"/> is (SDD-010).
    /// </summary>
    public Guid? ContactId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Direction is not null && this.ContactId is null)
            yield return new ValidationResult("A direction names a contact.", [nameof(this.ContactId)]);

        if (this.ContactId is not null && this.Direction is null)
            yield return new ValidationResult("A contact names a direction.", [nameof(this.Direction)]);
    }
}
