using System.Globalization;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Respiral.Skill.Components;
using Content.Shared._Respiral.Skill.Prototypes;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Commands;

/// <summary>
///     Устанавливает сущности точный прогресс навыка (по умолчанию 1 — полностью изучен; можно и
///     понизить, вплоть до 0). В отличие от SkillSystem.AddSkillProgress задаёт абсолютное значение,
///     а не прибавляет. Если у сущности нет <see cref="SkillComponent"/>, он добавляется.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class SetSkillCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public string Command => "setskill";
    public string Description => "Устанавливает точный прогресс навыка сущности (0..1).";
    public string Help => $"Usage: {Command} <target|self> <skillId> [progress]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            shell.WriteLine(Help);
            return;
        }

        EntityUid target;
        if (args[0] == "self")
        {
            if (shell.Player?.AttachedEntity is not { Valid: true } playerEntity)
            {
                shell.WriteError("Only a player can use 'self'.");
                return;
            }

            target = playerEntity;
        }
        else if (NetEntity.TryParse(args[0], out var netEnt) && _entManager.TryGetEntity(netEnt, out var ent))
        {
            target = ent.Value;
        }
        else
        {
            shell.WriteError($"{args[0]} is not a valid entity.");
            return;
        }

        var skillId = args[1];
        if (!_protoManager.HasIndex<SkillPrototype>(skillId))
        {
            shell.WriteError($"Skill prototype '{skillId}' not found.");
            return;
        }

        var progress = 1f;
        if (args.Length == 3)
        {
            if (!float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out progress))
            {
                shell.WriteError($"{args[2]} is not a valid number.");
                return;
            }

            progress = Math.Clamp(progress, 0f, 1f);
        }

        var comp = _entManager.EnsureComponent<SkillComponent>(target);
        comp.Skills[skillId] = progress;
        _entManager.Dirty(target, comp);

        // Пересчитать компоненты, выдаваемые навыком (SkillPrototype.GrantComponents).
        _entManager.System<Content.Server._Respiral.Skill.SkillSystem>().RefreshSkillGrants(target, comp);

        shell.WriteLine($"Set skill '{skillId}' to {progress.ToString("0.##", CultureInfo.InvariantCulture)} " +
                        $"on {_entManager.ToPrettyString(target)}.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint("<target|self>"),
            2 => CompletionResult.FromHintOptions(
                _protoManager.EnumeratePrototypes<SkillPrototype>().Select(p => p.ID),
                "<skillId>"),
            3 => CompletionResult.FromHint("[progress 0..1]"),
            _ => CompletionResult.Empty,
        };
    }
}
