using Content.Shared._Respiral.Identity;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement.Components;

namespace Content.Shared.IdentityManagement;

/// <summary>
///     Static content API for getting the identity entities/names for a given entity.
///     This should almost always be used in favor of metadata name, if the entity in question is a human player that
///     can have identity.
/// </summary>
public static class Identity
{
    /// <summary>
    ///     Returns the name that should be used for this entity for identity purposes.
    /// </summary>
    /// <remarks>
    /// This will return the true identity of the entity if called before the
    /// identity component has been initialized — this may occur for example if
    /// the client raises an event in response to an entity entering PVS for
    /// the first time.
    /// </remarks>
    public static string Name(EntityUid uid, IEntityManager ent, EntityUid? viewer = null)
    {
        if (!uid.IsValid())
            return string.Empty;

        var meta = ent.GetComponent<MetaDataComponent>(uid);
        if (meta.EntityLifeStage <= EntityLifeStage.Initializing)
            return meta.EntityName; // Identity component and such will not yet have initialized and may throw NREs

        var uidName = meta.EntityName;

        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return uidName;

        var ident = identity.IdentityEntitySlot?.ContainedEntity;
        if (ident is null)
            return uidName;

        var identName = ent.GetComponent<MetaDataComponent>(ident.Value).EntityName;

        // Respiral-Start - себя/призрак видят настоящее имя; остальные — аноним, если подписчик не заменит
        if (viewer == null)
            return identName;

        if (uid == viewer.Value || ent.HasComponent<GhostComponent>(viewer.Value))
            return uidName == identName ? uidName : $"{uidName} ({identName})";

        // Знакомые (KnownFaces) заменяют имя через событие — сам тип живёт в Respiral
        var ev = new ResolveIdentityNameEvent(uid, viewer.Value, identName);
        ent.EventBus.RaiseLocalEvent(viewer.Value, ref ev);
        return ev.Name;
        // Respiral-End
    }

    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    ///     This is an extension method because of its simplicity, and if it was any harder to call it might not
    ///     be used enough for loc.
    /// </summary>
    /// <param name="viewer">
    ///     If this entity can see through identities, this method will always return the actual target entity.
    /// </param>
    /// <inheritdoc cref="Name" path="remarks" />
    public static EntityUid Entity(EntityUid uid, IEntityManager ent, EntityUid? viewer = null)
    {
        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return uid;

        if (viewer != null && CanSeeThroughIdentity(uid, viewer.Value, ent))
            return uid;

        return identity.IdentityEntitySlot?.ContainedEntity ?? uid;
    }

    // Respiral-Start
    public static bool CanSeeThroughIdentity(EntityUid uid, EntityUid viewer, IEntityManager ent)
    {
        if (ent.HasComponent<GhostComponent>(viewer))
            return true;

        if (uid == viewer)
            return true;

        var ev = new CanSeeThroughIdentityEvent(uid, viewer);
        ent.EventBus.RaiseLocalEvent(viewer, ref ev);
        return ev.CanSee;
    }
    // Respiral-End
}
