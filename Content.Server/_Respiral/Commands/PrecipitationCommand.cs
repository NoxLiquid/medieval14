using System.Linq;
using Content.Server._Respiral.Precipitation;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._Respiral.Precipitation;
using Robust.Shared.Console;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Respiral.Commands;

/// <summary>
///     Ручной запуск/остановка осадка на карте.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class PrecipitationCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public string Command => "precip";
    public string Description => "Запускает или останавливает осадок на карте игрока.";
    public string Help => $"Usage: {Command} start <precipId> | {Command} stop";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (shell.Player?.AttachedEntity is not { Valid: true } player)
        {
            shell.WriteError("Only a player can use this command.");
            return;
        }

        var mapUid = _entManager.GetComponent<TransformComponent>(player).MapUid;
        if (mapUid is not { } map || !_entManager.HasComponent<MapComponent>(map))
        {
            shell.WriteError("Player is not on a valid map.");
            return;
        }

        var system = _entManager.System<PrecipitationSystem>();

        switch (args[0])
        {
            case "start":
                if (args.Length != 2)
                {
                    shell.WriteLine(Help);
                    return;
                }

                if (!_protoManager.HasIndex<PrecipitationPrototype>(args[1]))
                {
                    shell.WriteError($"Precipitation prototype '{args[1]}' not found.");
                    return;
                }

                // Переиспользуем компонент, уже заведённый где-либо в z-сети, иначе ставим на карту игрока
                var startTarget = system.TryGetNetworkPrecipitation(map, out var existingStart)
                    ? existingStart.Owner
                    : map;

                if (system.StartPrecipitation(startTarget, args[1]))
                    shell.WriteLine($"Started precipitation '{args[1]}' on the map (z-network aware).");
                else
                    shell.WriteError("Failed to start precipitation.");
                break;

            case "stop":
                // Останавливаем осадок, где бы в сети ни жил его компонент
                if (system.TryGetNetworkPrecipitation(map, out var existingStop))
                {
                    system.StopPrecipitation(existingStop.Owner);
                    shell.WriteLine("Stopped precipitation on the map.");
                }
                else
                {
                    shell.WriteError("No precipitation on this map or its z-network.");
                }
                break;

            default:
                shell.WriteLine(Help);
                break;
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(new[] { "start", "stop" }, "<start|stop>"),
            2 when args[0] == "start" => CompletionResult.FromHintOptions(
                _protoManager.EnumeratePrototypes<PrecipitationPrototype>().Select(p => p.ID),
                "<precipId>"),
            _ => CompletionResult.Empty,
        };
    }
}
