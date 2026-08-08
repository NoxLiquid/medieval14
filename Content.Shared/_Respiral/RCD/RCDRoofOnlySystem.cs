using Content.Shared._Respiral.RCD.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.RCD;
using Content.Shared.RCD.Systems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Respiral.RCD;

/// <summary>
/// Обрабатывает RCD-инструменты с <see cref="RCDRoofOnlyComponent"/> (кровельный молот):
/// запрещает демонтаж всего, кроме сущностей с тегом MedievalRoof, и демонтаж плитки пола.
/// Постройка ограничивается на уровне availablePrototypes самого RCD-компонента.
/// </summary>
public sealed partial class RCDRoofOnlySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<TagPrototype> RoofTag = "MedievalRoof";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDRoofOnlyComponent, DoAfterAttemptEvent<RCDDoAfterEvent>>(OnDoAfterAttempt);
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDRoofOnlyComponent component, DoAfterAttemptEvent<RCDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        if (!_protoMan.TryIndex(args.Event.StartingProtoId, out var prototype) || prototype.Mode != RcdMode.Deconstruct)
            return;

        var target = args.Event.Target;

        // Respiral - демонтаж плитки пола (target == null) и любых сущностей без тега MedievalRoof запрещён
        if (target == null || !_tag.HasTag(target.Value, RoofTag))
        {
            _popup.PopupClient(Loc.GetString("respiral-roof-rcd-not-a-roof"), uid, args.Event.User);
            args.Cancel();
        }
    }
}
