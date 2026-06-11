using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests._Respiral
{
    // Respiral - тест спавнит только сущности, объявленные в папке Resources/Prototypes/_Respiral.
    // Используется как блокирующий гейт CI: проверяет именно наш контент, не трогая апстрим.
    [TestFixture]
    [TestOf(typeof(EntityUid))]
    public sealed class RespiralEntityTest : GameTest
    {
        // Respiral - папка, по которой определяется принадлежность сущности к Respiral.
        private static readonly ResPath RespiralPrototypeDir = new("/Prototypes/_Respiral/");

        public override PoolSettings PoolSettings => new()
        {
            Dirty = true,
        };

        /// <summary>
        /// Respiral - собирает id всех сущностей, объявленных под /Prototypes/_Respiral/.
        /// Префикс id ненадёжен (Medieval, Bush, Organ, Mob, ...), поэтому источник истины — папка.
        /// </summary>
        private static HashSet<string> CollectRespiralEntityIds(IResourceManager resources)
        {
            var ids = new HashSet<string>();
            var paths = resources.ContentFindFiles(RespiralPrototypeDir)
                .Where(p => p.Extension == "yml" && !p.Filename.StartsWith('.'));

            foreach (var path in paths)
            {
                using var reader = new StreamReader(resources.ContentFileRead(path), Encoding.UTF8);
                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                foreach (var document in yamlStream.Documents)
                {
                    if (document.RootNode is not YamlSequenceNode root)
                        continue;

                    foreach (var node in root.Cast<YamlMappingNode>())
                    {
                        if (!node.TryGetNode("type", out var typeNode) || typeNode.AsString() != "entity")
                            continue;
                        if (node.TryGetNode("id", out var idNode))
                            ids.Add(idNode.AsString());
                    }
                }
            }

            return ids;
        }

        [Test]
        public async Task SpawnAndDeleteAllRespiralEntities()
        {
            var pair = Pair;
            var server = pair.Server;

            var entityMan = server.ResolveDependency<IEntityManager>();
            var prototypeMan = server.ResolveDependency<IPrototypeManager>();
            var resourceMan = server.ResolveDependency<IResourceManager>();

            var respiralIds = CollectRespiralEntityIds(resourceMan);
            Assert.That(respiralIds, Is.Not.Empty, "Не найдено ни одной Respiral-сущности под /Prototypes/_Respiral/.");

            var map = await pair.CreateTestMap();

            List<string> protoIds = null!;
            await server.WaitPost(() =>
            {
                protoIds = prototypeMan
                    .EnumeratePrototypes<EntityPrototype>()
                    .Where(p => !p.Abstract)
                    .Where(p => respiralIds.Contains(p.ID))
                    .Where(p => !p.Components.ContainsKey("MapGrid")) // Иначе раздавит карту.
                    .Where(p => !p.Components.ContainsKey("RoomFill")) // Может удалить/наспавнить кучу сущностей.
                    .Select(p => p.ID)
                    .ToList();

                foreach (var protoId in protoIds)
                {
                    entityMan.SpawnEntity(protoId, map.GridCoords);
                }
            });

            await server.WaitRunTicks(450); // 15 секунд — хватает, чтобы прогнать большинство update-циклов.

            await server.WaitPost(() =>
            {
                var query = entityMan.AllEntityQueryEnumerator<MetaDataComponent>();
                var toDelete = new List<EntityUid>();
                while (query.MoveNext(out var uid, out var meta))
                {
                    if (!meta.EntityDeleted)
                        toDelete.Add(uid);
                }

                foreach (var uid in toDelete)
                    entityMan.DeleteEntity(uid);

                Assert.That(entityMan.EntityCount, Is.Zero);
            });
        }
    }
}
