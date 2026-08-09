using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Rope;

[Serializable, NetSerializable]
public sealed partial class TieRopeDoAfterEvent : DoAfterEvent
{
    public NetEntity First;

    public TieRopeDoAfterEvent(NetEntity first)
    {
        First = first;
    }

    public override DoAfterEvent Clone() => this;
}
