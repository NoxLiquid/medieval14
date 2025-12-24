using Content.Shared.Durability;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Durability;

public sealed class DurabilityExamineSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedDurabilitySystem _durability = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DurabilityComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, DurabilityComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var message = GetDetailedDurabilityExamine(uid, component);
        if (!message.IsEmpty)
        {
            _examine.AddDetailedExamineVerb(args, component, message,
                Loc.GetString("durability-examine-verb-text"),
                "/Textures/Interface/VerbIcons/examine.svg.192dpi.png",
                Loc.GetString("durability-examine-verb-message")
            );
        }
    }

    public FormattedMessage GetDetailedDurabilityExamine(EntityUid uid, DurabilityComponent component)
    {
        var msg = new FormattedMessage();

        var percent = component.UsePartsDurability && component.PartsContainer != null && component.PartsContainer.Count > 0
            ? _durability.GetCompositeDurabilityPercent(uid, component)
            : _durability.GetDurabilityPercent(uid, component);

        var condition = GetConditionDescription(percent);
        var color = GetConditionColor(percent);

        msg.AddMarkupOrThrow(Loc.GetString("durability-detailed-simple",
            ("name", Name(uid)),
            ("condition", condition),
            ("color", color)));

        if (component.UsePartsDurability && component.PartsContainer != null && component.PartsContainer.Count > 0)
        {
            msg.PushNewline();
            foreach (var part in component.PartsContainer.ContainedEntities)
            {
                if (!TryComp<DurabilityComponent>(part, out var partDurability))
                    continue;

                var partPercent = _durability.GetDurabilityPercent(part, partDurability);
                var partCondition = GetConditionDescription(partPercent);
                var partColor = GetConditionColor(partPercent);
                var partName = Name(part);

                msg.PushNewline();

                var partText = Loc.GetString("durability-part-simple",
                    ("name", partName),
                    ("condition", partCondition),
                    ("color", partColor));

                if (TryComp<PartDurabilityComponent>(part, out var partDurabilityComp) && partDurabilityComp.IsCriticalPart)
                    partText += " " + Loc.GetString("durability-critical-mark");

                msg.AddMarkupOrThrow(partText);
            }
        }

        return msg;
    }

    private string GetConditionDescription(float percent)
    {
        return percent switch
        {
            >= 0.9f => Loc.GetString("durability-pristine"),
            >= 0.7f => Loc.GetString("durability-good"),
            >= 0.5f => Loc.GetString("durability-worn"),
            >= 0.3f => Loc.GetString("durability-poor"),
            >= 0.1f => Loc.GetString("durability-bad"),
            > 0f => Loc.GetString("durability-critical"),
            _ => Loc.GetString("durability-broken")
        };
    }

    private string GetConditionColor(float percent)
    {
        return percent switch
        {
            >= 0.9f => "#4CAF50",
            >= 0.7f => "#8BC34A",
            >= 0.5f => "#CDDC39",
            >= 0.3f => "#FF9800",
            >= 0.1f => "#F44339",
            > 0f => "#D32F2F",
            _ => "#795548"
        };
    }
}
