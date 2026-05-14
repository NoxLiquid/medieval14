using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Respiral.Parallax.Biomes.Layers;

/// <summary>
/// Respiral - координатно-ограниченный биом.
/// Генерирует вложенный BiomeTemplate только внутри заданной области (прямоугольник или круг).
/// Приоритет определяется позицией в списке слоёв: чем позже в списке, тем выше приоритет.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BiomeRegionLayer : IBiomeLayer
{
    [DataField]
    public FastNoiseLite Noise { get; private set; } = new(0);

    [DataField]
    public float Threshold { get; private set; } = -1f;

    [DataField]
    public bool Invert { get; private set; }

    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Template { get; private set; }

    [DataField]
    public BiomeRegionShape Shape { get; private set; } = BiomeRegionShape.Rectangle;

    /// <summary>
    /// Начальная точка прямоугольника (левый нижний угол).
    /// </summary>
    [DataField]
    public Vector2i Origin { get; private set; }

    /// <summary>
    /// Конечная точка прямоугольника (правый верхний угол, не включительно).
    /// </summary>
    [DataField]
    public Vector2i End { get; private set; }

    /// <summary>
    /// Центр круга.
    /// </summary>
    [DataField]
    public Vector2i Center { get; private set; }

    /// <summary>
    /// Радиус круга в тайлах.
    /// </summary>
    [DataField]
    public float Radius { get; private set; }

    public bool ContainsPoint(Vector2i point)
    {
        return Shape switch
        {
            BiomeRegionShape.Rectangle =>
                point.X >= Origin.X && point.X < End.X &&
                point.Y >= Origin.Y && point.Y < End.Y,
            BiomeRegionShape.Circle =>
                (point.X - Center.X) * (point.X - Center.X) +
                (point.Y - Center.Y) * (point.Y - Center.Y) <= Radius * Radius,
            _ => false
        };
    }
}

[Serializable, NetSerializable]
public enum BiomeRegionShape : byte
{
    Rectangle,
    Circle
}
