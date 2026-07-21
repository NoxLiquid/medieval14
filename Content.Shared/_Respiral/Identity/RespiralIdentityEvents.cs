using Content.Shared.IdentityManagement.Components;

namespace Content.Shared._Respiral.Identity;

// Точки расширения идентичности: upstream поднимает эти события, обработчики — в _Respiral.

/// <summary>Позволяет переопределить предполагаемое имя/должность (грамоты Respiral). Поднимается на цели.</summary>
[ByRefEvent]
public readonly struct ModifyPresumedIdentityEvent
{
    public readonly IdentityRepresentation Representation;

    public ModifyPresumedIdentityEvent(IdentityRepresentation representation)
    {
        Representation = representation;
    }
}

/// <summary>Даёт подписчику заменить показанное имя цели. Поднимается на зрителе.</summary>
[ByRefEvent]
public struct ResolveIdentityNameEvent
{
    public readonly EntityUid Target;
    public readonly EntityUid Viewer;
    public string Name; // на входе — имя по умолчанию

    public ResolveIdentityNameEvent(EntityUid target, EntityUid viewer, string name)
    {
        Target = target;
        Viewer = viewer;
        Name = name;
    }
}

/// <summary>Спрашивает, видит ли зритель сквозь аноним цели. Поднимается на зрителе.</summary>
[ByRefEvent]
public struct CanSeeThroughIdentityEvent
{
    public readonly EntityUid Target;
    public readonly EntityUid Viewer;
    public bool CanSee;

    public CanSeeThroughIdentityEvent(EntityUid target, EntityUid viewer)
    {
        Target = target;
        Viewer = viewer;
    }
}
