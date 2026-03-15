using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Lock.Events;

[Serializable, NetSerializable]
public sealed partial class UseKeyDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed partial class DuplicateKeyDoAfterEvent : SimpleDoAfterEvent
{

}
