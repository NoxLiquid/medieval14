using Content.Shared._Respiral.FriendlyFaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.NPC.Systems;

namespace Content.Shared._Respiral.FriendlyFaction;

/// <summary>
/// TODO эта система должна иметь ещё какую-нибудь логику? Может быть её стоит переименовать?
///
/// p.s. я не понимаю, почему автор не связал систему с <see cref="NpcFactionSystem"/>, изначально.
/// система содержит готовый метод IsEntityFriendly, который делает ровно то же самое, что было в изначальной логике
/// даже больше: учитывает отношения Friendly из прототипов фракций, + с системой связана Npc.
/// При такой реализации не будет странностей, по типу: NPC из враждебной фракции бъёт сущность не нанося урон.
/// </remarks>
public sealed partial class FriendlyFactionSystem : EntitySystem
{
    [Dependency] private NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FriendlyFactionComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnAttackAttempt(EntityUid uid, FriendlyFactionComponent component, AttackAttemptEvent args)
    {
        // Разоружающие приемы не наносят урона, поэтому союзники могут разоружать друг друга.
        if (args.Cancelled || args.Disarm || args.Target is not { } target || target == uid)
            return;

        if (_faction.IsEntityFriendly(uid, target))
            args.Cancel();
    }
}
