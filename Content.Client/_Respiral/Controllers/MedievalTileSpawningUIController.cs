using System.Linq;
using Content.Shared._Respiral.CCCVar;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Respiral.Controllers;

[UsedImplicitly]
public sealed partial class MedievalTileSpawningUIController : UIController
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;

    private TileSpawnWindow? _window;
    private bool _init;

    private readonly List<ITileDefinition> _shownTiles = new();
    private bool _clearingTileSelections;
    private bool _eraseTile;
    private bool _mirrorableTile;
    private bool _mirroredTile;
    private bool _prefixFilterEnabled;

    private const string Prefix = "Medieval";

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;
        _placement.PlacementChanged += ClearTileSelection;
        _placement.DirectionChanged += OnDirectionChanged;
        _placement.MirroredChanged += OnMirroredChanged;
        _cfg.OnValueChanged(CCCVars.MedievalPrefixFilterEnabled, OnPrefixFilterChanged, true);
    }

    private void OnPrefixFilterChanged(bool enabled)
    {
        _prefixFilterEnabled = enabled;

        if (_window == null || _window.Disposed)
            return;

        BuildTileList(_window.SearchBar.Text);
    }

    private void StartTilePlacement(int tileType)
    {
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = nameof(AlignTileAny),
            TileType = tileType,
            Range = 400,
            IsTile = true
        };

        _placement.BeginPlacing(newObjInfo);
    }

    private void OnTileEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();

        if (args.Pressed)
        {
            _eraseTile = true;
            StartTilePlacement(0);
        }
        else
        {
            _eraseTile = false;
        }

        args.Button.Pressed = args.Pressed;
    }

    private void OnTileMirroredToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Mirrored = args.Pressed;
        _mirroredTile = _placement.Mirrored;

        args.Button.Pressed = args.Pressed;
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            BuildTileList(_window.SearchBar.Text);
            _window.Open();
            UpdateEntityDirectionLabel();
            UpdateMirroredButton();
            _window.SearchBar.GrabKeyboardFocus();
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<TileSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemSelected += OnTileItemSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;
        _window.EraseButton.Pressed = _eraseTile;
        _window.EraseButton.OnToggled += OnTileEraseToggled;
        _window.MirroredButton.Disabled = !_mirrorableTile;
        _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        _window.MirroredButton.Pressed = _mirroredTile;
        _window.MirroredButton.OnToggled += OnTileMirroredToggled;
        BuildTileList();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
    }

    private void ClearTileSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed)
            return;

        _clearingTileSelections = true;
        _window.TileList.ClearSelected();
        _clearingTileSelections = false;
        _window.EraseButton.Pressed = false;
        _window.MirroredButton.Pressed = _placement.Mirrored;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
        _window.ClearButton.Disabled = true;
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        BuildTileList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnTileItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var definition = _shownTiles[args.ItemIndex];
        StartTilePlacement(definition.TileId);
        UpdateMirroredButton();
    }

    private void OnTileItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        if (_clearingTileSelections)
            return;

        _placement.Clear();
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateEntityDirectionLabel();
    }

    private void UpdateEntityDirectionLabel()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.RotationLabel.Text = _placement.Direction.ToString();
    }

    private void OnMirroredChanged(object? sender, EventArgs e)
    {
        UpdateMirroredButton();
    }

    private void UpdateMirroredButton()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_placement.CurrentPermission != null && _placement.CurrentPermission.IsTile)
        {
            var allowed = _tiles[_placement.CurrentPermission.TileType].AllowRotationMirror;
            _mirrorableTile = allowed;
            _window.MirroredButton.Disabled = !_mirrorableTile;
            _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        }

        _mirroredTile = _placement.Mirrored;
        _window.MirroredButton.Pressed = _mirroredTile;
    }

    private void BuildTileList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.TileList.Clear();

        IEnumerable<ITileDefinition> tileDefs = _tiles.Where(def =>
            !def.EditorHidden &&
            IsVisibleTile(def));

        if (!string.IsNullOrEmpty(searchStr))
        {
            tileDefs = tileDefs.Where(tile =>
                Loc.GetString(tile.Name).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                tile.ID.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
        }

        tileDefs = tileDefs.OrderBy(tile => Loc.GetString(tile.Name));

        _shownTiles.Clear();
        _shownTiles.AddRange(tileDefs);

        foreach (var entry in _shownTiles)
        {
            Texture? texture = null;
            var path = entry.Sprite?.ToString();

            if (path != null)
                texture = _resources.GetResource<TextureResource>(path);

            _window.TileList.AddItem(Loc.GetString(entry.Name), texture);
        }
    }

    private bool IsVisibleTile(ITileDefinition tile)
    {
        return !_prefixFilterEnabled || tile.ID.StartsWith(Prefix, StringComparison.Ordinal);
    }
}