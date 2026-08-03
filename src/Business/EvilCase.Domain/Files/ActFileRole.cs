using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Domain.Files;

/// <summary>
/// What one file is to one act. The role is on the link, never on the asset. Serialized by name rather
/// than by number, so the wire format survives a reordering and the stored column stays readable.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ActFileRole>))]
public enum ActFileRole
{
    /// <summary>
    /// What the act was written in — the `.docx` behind a submission.
    /// </summary>
    Source = 0,

    /// <summary>
    /// What was actually filed or issued, normally the PDF.
    /// </summary>
    Final = 1,

    Attachment = 2,

    /// <summary>
    /// A <em>doručenka</em>.
    /// </summary>
    DeliveryReceipt = 3,

    /// <summary>
    /// A data-box envelope, a `.zfo`.
    /// </summary>
    Envelope = 4,
}
