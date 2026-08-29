using Content.Shared._Respiral.Identity;
using Content.Shared._Respiral.Tabard.Components;
using Content.Shared.Inventory;

namespace Content.Shared._Respiral.Tabard;

/// <summary>
/// Ношение брони с прикреплённым домовым табардом задаёт аффилиацию (PresumedJob), видимую окружающим
/// при скрытом лице носителя. Логика идентификации общая (shared), читает сетевой кэш флаг-компонента.
/// Обновление имени инициируется при прикреплении/снятии на сервере (QueueIdentityUpdate).
/// </summary>
public sealed partial class MedievalTabardIdentitySystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, ModifyPresumedIdentityEvent>(OnModifyIdentity);
    }

    private void OnModifyIdentity(Entity<InventoryComponent> ent, ref ModifyPresumedIdentityEvent args)
    {
        var slots = _inventory.GetSlotEnumerator((ent.Owner, ent.Comp));
        while (slots.NextItem(out var item, out _))
        {
            if (TryComp<MedievalTabardAttachableComponent>(item, out var tabard) && tabard.Affiliation is { } affiliation)
            {
                args.Representation.PresumedJob = Loc.GetString(affiliation);
                return;
            }
        }
    }
}
