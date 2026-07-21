using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Skill.Events;

[Serializable, NetSerializable]
public sealed partial class LearnDoAfterEvent : SimpleDoAfterEvent
{ }
