using Content.Shared._Respiral.Tabard.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Tabard;

/// <summary>
/// Отрисовка прикреплённого табарда: <c>equipped-TABARD</c> поверх экипированной одежды
/// (через <see cref="GetEquipmentVisualsEvent"/> после ClothingSystem) и <c>icon</c> поверх иконки
/// самого предмета. Данные берутся из сетевого кэша <see cref="MedievalTabardAttachableComponent"/>.
/// </summary>
public sealed partial class MedievalTabardVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedItemSystem _item = default!;

    private const string IconLayerKey = "medieval-tabard-icon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedievalTabardAttachableComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MedievalTabardAttachableComponent, ComponentShutdown>(OnShutdown);
    }

    // Накладка на носителя: добавляем слой поверх слоёв одежды в том же слоте.
    private void OnGetEquipmentVisuals(Entity<MedievalTabardAttachableComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.RsiPath is not { } rsi || ent.Comp.EquippedState is not { } state)
            return;

        var key = $"{args.Slot}-tabard";
        args.Layers.Add((key, new PrototypeLayerData
        {
            RsiPath = rsi,
            State = state,
            MapKeys = new() { key },
        }));
    }

    private void OnStartup(Entity<MedievalTabardAttachableComponent> ent, ref ComponentStartup args)
    {
        UpdateIcon(ent);
    }

    private void OnHandleState(Entity<MedievalTabardAttachableComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateIcon(ent);
        _item.VisualsChanged(ent.Owner); // перерисовать экипированную одежду (equipped-накладка)
    }

    private void OnShutdown(Entity<MedievalTabardAttachableComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.RemoveLayer((ent.Owner, sprite), IconLayerKey, logMissing: false);
    }

    // Накладка на иконку самого предмета одежды (в руках/инвентаре/на земле).
    private void UpdateIcon(Entity<MedievalTabardAttachableComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var hasLayer = _sprite.LayerExists((ent.Owner, sprite), IconLayerKey);

        if (ent.Comp.RsiPath is { } rsi && ent.Comp.IconState is { } icon)
        {
            if (!hasLayer)
            {
                _sprite.AddLayer((ent.Owner, sprite), new PrototypeLayerData
                {
                    RsiPath = rsi,
                    State = icon,
                    MapKeys = new() { IconLayerKey },
                }, null);
            }
        }
        else if (hasLayer)
        {
            _sprite.RemoveLayer((ent.Owner, sprite), IconLayerKey, logMissing: false);
        }
    }
}
