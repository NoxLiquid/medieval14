using Content.Client.Paper;
using Content.Shared._Respiral.Identity;
using Content.Shared._Respiral.Identity.Components;
using Content.Shared.Paper;
using Robust.Client.GameObjects;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Client._Respiral.Identity;

/// <summary>
/// Управляет внешним видом грамоты в зависимости от того, развёрнута ли она
/// </summary>
public sealed class MedievalCredentialVisualizerSystem : VisualizerSystem<MedievalCredentialVisualizerComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MedievalCredentialVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);
        var presented = AppearanceSystem.TryGetData<bool>(uid, MedievalCredentialVisuals.Presented, out var value, args.Component) && value;

        // Обложка: свёрнутая ↔ развёрнутая.
        SpriteSystem.LayerSetRsiState(sprite, MedievalCredentialVisualLayers.Base, presented ? component.OpenState : component.ClosedState);

        // Иконка поверх обложки видна только у свёрнутой грамоты.
        SpriteSystem.LayerSetVisible(sprite, MedievalCredentialVisualLayers.Icon, !presented);

        // Текст виден только когда грамота развёрнута и заполнена.
        var written = AppearanceSystem.TryGetData<PaperStatus>(uid, PaperVisuals.Status, out var status, args.Component)
                      && status == PaperStatus.Written;
        SpriteSystem.LayerSetVisible(sprite, PaperVisualLayers.Writing, presented && written);

        // Печать видна только когда грамота развёрнута.
        var stamped = AppearanceSystem.TryGetData<string>(uid, PaperVisuals.Stamp, out var stampState, args.Component)
                      && stampState != string.Empty;
        if (stamped)
            SpriteSystem.LayerSetRsiState(sprite, PaperVisualLayers.Stamp, stampState);
        SpriteSystem.LayerSetVisible(sprite, PaperVisualLayers.Stamp, presented && stamped);
    }
}

/// <summary>Слои базового спрайта грамоты (мапятся в прототипе).</summary>
public enum MedievalCredentialVisualLayers : byte
{
    Base,
    Icon,
}
