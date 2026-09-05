using System.Numerics;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Forensics.Components;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Respiral.Destructible
{
    [Serializable]
    [DataDefinition]
    public sealed partial class ProbSpawnEntitiesBehavior : IThresholdBehavior
    {
        [DataField]
        public List<EntitySpawnEntry>? Spawns;

        [DataField]
        public int Amount;

        [DataField]
        public float Offset { get; set; } = 0.5f;

        [DataField]
        public bool DoTransferForensics;

        [DataField]
        public bool SpawnInContainer;

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {

            if (Spawns == null || Spawns.Count == 0)
                return;

            var tSys = system.EntityManager.System<TransformSystem>();
            var position = tSys.GetMapCoordinates(owner);

            var getRandomVector = () => new Vector2(system.Random.NextFloat(-Offset, Offset), system.Random.NextFloat(-Offset, Offset));

            var spawnList = EntitySpawnCollection.GetSpawns(Spawns, system.Random);

            var amount = 0;
            foreach (var entityId in spawnList)
            {
                if (entityId == null)
                    continue;

                if (amount >= Amount)
                    return;

                var spawned = SpawnInContainer
                    ? system.EntityManager.SpawnNextToOrDrop(entityId, owner)
                    : system.EntityManager.SpawnEntity(entityId, position.Offset(getRandomVector()));

                if (EntityPrototypeHelpers.HasComponent<StackComponent>(entityId, system.PrototypeManager, system.EntityManager.ComponentFactory))
                    system.StackSystem.SetCount(spawned, 1);

                TransferForensics(spawned, system, owner);
                amount++;
            }
        }

        public void TransferForensics(EntityUid spawned, DestructibleSystem system, EntityUid owner)
        {
            if (!DoTransferForensics ||
                !system.EntityManager.TryGetComponent<ForensicsComponent>(owner, out var forensicsComponent))
                return;

            var comp = system.EntityManager.EnsureComponent<ForensicsComponent>(spawned);
            comp.DNAs = forensicsComponent.DNAs;

            if (!system.Random.Prob(0.4f))
                return;

            comp.Fingerprints = forensicsComponent.Fingerprints;
            comp.Fibers = forensicsComponent.Fibers;
        }
    }
}
