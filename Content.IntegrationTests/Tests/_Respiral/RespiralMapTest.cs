using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Respiral
{
    // Respiral - грузит каждую карту из Resources/Maps/_Respiral и инициализирует её.
    // Ловит битый YAML карт, отсутствующие/переименованные прототипы сущностей на картах и т.п.
    [TestFixture]
    public sealed class RespiralMapTest : GameTest
    {
        // Respiral - все map-файлы Respiral берём из VFS, чтобы тест автоматически подхватывал новые карты.
        private static readonly ResPath[] RespiralMaps =
            GameDataScrounger.FilesInDirectoryInVfs("/Maps/_Respiral", "*.yml");

        public override PoolSettings PoolSettings => new()
        {
            Dirty = true,
        };

        [Test, TestCaseSource(nameof(RespiralMaps))]
        public async Task RespiralMapsLoadableTest(ResPath path)
        {
            var pair = Pair;
            var server = pair.Server;

            var entMan = server.ResolveDependency<IEntityManager>();
            var mapLoader = entMan.System<MapLoaderSystem>();

            await server.WaitPost(() =>
            {
                var opts = new MapLoadOptions
                {
                    // Respiral - InitializeMaps прогоняет map-init: спавнит сущности карты и ловит ошибки.
                    DeserializationOptions = DeserializationOptions.Default with { InitializeMaps = true },
                };

                Assert.That(mapLoader.TryLoadGeneric(path, out var maps, out _, opts),
                    $"Не удалось загрузить Respiral-карту {path}");
                Assert.That(maps, Is.Not.Empty, $"Файл {path} не содержит ни одной map-сущности");
            });
        }
    }
}
