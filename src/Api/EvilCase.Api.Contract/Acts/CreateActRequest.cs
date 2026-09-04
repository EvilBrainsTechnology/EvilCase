using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record CreateActRequest : IValidatableObject
{
    public ActDirection? Direction { get; init; }

    /// <summary>
    /// The act number is issued from this date, not the write's.
    /// </summary>
    public required DateOnly Date { get; init; }

    [Required]
    [StringLength(256)]
    public required string Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    public Guid? ContactId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (this.Direction is not null && this.ContactId is null)
            yield return new ValidationResult("A direction names a contact.", [nameof(this.ContactId)]);

        if (this.ContactId is not null && this.Direction is null)
            yield return new ValidationResult("A contact names a direction.", [nameof(this.Direction)]);
    }
}
