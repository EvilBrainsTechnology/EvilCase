using System.Text.Json.Serialization;

namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// What one file is to one act. The role is on the link and never on the asset: the same bytes are the
/// final decision in the act that issued it and an attachment in the five acts that cite it.
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
    /// A <em>doručenka</em>. It arrives as its own file, named after the message it acknowledges rather
    /// than after the act.
    /// </summary>
    DeliveryReceipt = 3,

    /// <summary>
    /// A data-box envelope, a `.zfo`. The same envelope is sometimes an act's own and an attachment of
    /// another act quoting it.
    /// </summary>
    Envelope = 4,
}
