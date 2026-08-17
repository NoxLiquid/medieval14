using System.Linq;
using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Client.GameObjects;

namespace Content.Client._Respiral.Inhand;

public sealed partial class MedievalIconInhandSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private const string Prefix = "medieval-iconinhand";
    private static readonly Vector2 IconScale = new(0.5f, 0.5f);

    private const float HandOffsetSide = 0.28f;
    private const float HandOffsetY = -0.1f;
    private const float HandOffsetForward = 0f;
    private const float HandOffsetBack = 0.12f;
    private const float HandOffsetFrontY = -0.12f;
    private const float HandOffsetBackY = -0.10f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
        SubscribeLocalEvent<MedievalIconInhandComponent, MoveEvent>(OnMove);
    }

    public void BuildFallback(EntityUid uid, GetInhandVisualsEvent args)
    {
        if (args.Layers.Count > 0)
            return;

        if (!TryComp<SpriteComponent>(uid, out var itemSprite))
            return;

        var loc = args.Location.ToString().ToLowerInvariant();
        var fallback = new HandIconFallback();
        var idx = 0;

        foreach (var layer in itemSprite.AllLayers)
        {
            if (!layer.Visible)
                continue;

            var stateName = layer.RsiState.Name;
            if (string.IsNullOrEmpty(stateName))
                continue;

            var rsi = layer.ActualRsi ?? layer.Rsi;
            var scale = new Vector2(layer.Scale.X * IconScale.X, layer.Scale.Y * IconScale.Y);
            var overKey = $"{Prefix}-over-{loc}-{idx}";
            var underKey = $"{Prefix}-under-{loc}-{idx}";

            args.Layers.Add((overKey, MakeData(stateName, rsi?.Path.ToString(), layer.Color, scale, layer.Rotation, overKey)));
            fallback.OverKeys.Add(overKey);
            fallback.UnderData.Add(MakeData(stateName, rsi?.Path.ToString(), layer.Color, scale, layer.Rotation, underKey));
            idx++;
        }

        if (idx == 0)
            return;

        var comp = EnsureComp<MedievalIconInhandComponent>(args.User);
        comp.Hands[args.Location] = fallback;
        comp.LastDirection = Direction.Invalid;
    }

    private static PrototypeLayerData MakeData(string state, string? rsiPath, Color color, Vector2 scale, Angle rotation, string key)
    {
        return new PrototypeLayerData
        {
            State = state,
            RsiPath = rsiPath,
            Color = color,
            Scale = scale,
            Rotation = rotation,
            Visible = true,
            MapKeys = new() { key },
        };
    }

    private void OnHeldVisualsUpdated(EntityUid uid, ItemComponent item, HeldVisualsUpdatedEvent args)
    {
        if (!TryComp<MedievalIconInhandComponent>(args.User, out var comp))
            return;

        if (!TryComp<SpriteComponent>(args.User, out var sprite))
            return;

        foreach (var loc in comp.Hands.Keys.ToList())
        {
            var stale = comp.Hands[loc];
            if (stale.OverKeys.Count == 0 || !_sprite.LayerMapTryGet((args.User, sprite), stale.OverKeys[0], out _, false))
                comp.Hands.Remove(loc);
        }

        if (comp.Hands.Count == 0)
        {
            RemCompDeferred<MedievalIconInhandComponent>(args.User);
            return;
        }

        foreach (var (_, fb) in comp.Hands)
        {
            if (fb.OverKeys.Count == 0 || !args.RevealedLayers.Contains(fb.OverKeys[0]))
                continue;

            fb.UnderKeys.Clear();
            for (var i = 0; i < fb.UnderData.Count; i++)
            {
                var data = fb.UnderData[i];
                var key = data.MapKeys!.First();

                if (!_sprite.LayerMapTryGet((args.User, sprite), key, out _, false))
                {
                    _sprite.AddLayer((args.User, sprite), data, i);
                    args.RevealedLayers.Add(key);
                }

                fb.UnderKeys.Add(key);
            }

            break;
        }

        comp.LastDirection = Direction.Invalid;
        ApplyOrder(args.User, sprite, comp);
    }

    private void OnMove(EntityUid uid, MedievalIconInhandComponent comp, ref MoveEvent args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            ApplyOrder(uid, sprite, comp);
    }

    private void ApplyOrder(EntityUid holder, SpriteComponent sprite, MedievalIconInhandComponent comp)
    {
        var dir = _xform.GetWorldRotation(holder).GetCardinalDir();
        if (dir == comp.LastDirection)
            return;

        comp.LastDirection = dir;

        foreach (var (loc, fb) in comp.Hands)
        {
            var onTop = ItemOnTop(loc, dir);
            var offset = HandOffset(loc, dir);

            foreach (var key in fb.OverKeys)
            {
                if (!_sprite.LayerMapTryGet((holder, sprite), key, out var oi, false))
                    continue;
                _sprite.LayerSetVisible((holder, sprite), oi, onTop);
                _sprite.LayerSetOffset((holder, sprite), oi, offset);
            }

            foreach (var key in fb.UnderKeys)
            {
                if (!_sprite.LayerMapTryGet((holder, sprite), key, out var ui, false))
                    continue;
                _sprite.LayerSetVisible((holder, sprite), ui, !onTop);
                _sprite.LayerSetOffset((holder, sprite), ui, offset);
            }
        }
    }

    private static Vector2 HandOffset(HandLocation location, Direction dir)
    {
        var left = location == HandLocation.Left;

        switch (dir)
        {
            case Direction.South:
                return new Vector2(left ? HandOffsetSide : -HandOffsetSide, HandOffsetY);
            case Direction.North:
                return new Vector2(left ? -HandOffsetSide : HandOffsetSide, HandOffsetY);
            case Direction.East:
                return ItemOnTop(location, dir)
                    ? new Vector2(HandOffsetForward, HandOffsetFrontY)
                    : new Vector2(HandOffsetBack, HandOffsetBackY);
            case Direction.West:
                return ItemOnTop(location, dir)
                    ? new Vector2(-HandOffsetForward, HandOffsetFrontY)
                    : new Vector2(-HandOffsetBack, HandOffsetBackY);
            default:
                return new Vector2(left ? HandOffsetSide : -HandOffsetSide, HandOffsetY);
        }
    }

    private static bool ItemOnTop(HandLocation location, Direction dir)
    {
        if (location == HandLocation.Left)
        {
            return dir switch
            {
                Direction.South => true,
                Direction.North => false,
                Direction.East => false,
                Direction.West => true,
                _ => true,
            };
        }

        return dir switch
        {
            Direction.South => true,
            Direction.North => false,
            Direction.East => true,
            Direction.West => false,
            _ => true,
        };
    }
}
