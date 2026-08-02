namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Who owns what the current request is about. The one place ownership is resolved.
/// </summary>
/// <remarks>
/// The seam M8 needs. Every aggregate root already carries an <c>OwnerId</c>; what M8 adds is filtering
/// every query by it, and that is cheap to do once and ruinous to do at ninety call sites that each read
/// the principal their own way. Nothing outside the implementation reads a claim to find out who is
/// asking.
/// <para>
/// It is also where a tenant would go. Today an owner is a user; when the vision's "multi-tenant SaaS
/// for law firms" becomes real, an owner becomes a tenant and this interface is what changes rather than
/// every query in the application.
/// </para>
/// </remarks>
public interface IOwnerContext
{
    /// <summary>
    /// The owner of the current request, or null where there is no authenticated caller — a health
    /// probe, the sign-in endpoint, a migration at startup.
    /// </summary>
    public long? OwnerId { get; }

    /// <summary>
    /// The owner, or an exception. For code that has no sensible answer without one: a query that would
    /// otherwise silently return another owner's rows, or none at all, is a bug either way.
    /// </summary>
    public long RequireOwnerId();
}
