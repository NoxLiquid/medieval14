using System.Numerics;
using Content.Shared._Respiral.CCCVar;
using JetBrains.Annotations;
using Robust.Client.Placement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.LineEdit;
using Robust.Shared.Utility;

namespace Content.Client._Respiral.Controllers;

[UsedImplicitly]
public sealed class MedievalEntitySpawningUIController : UIController
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private EntitySpawnWindow? _window;
    private readonly List<EntityPrototype> _shownEntities = new();
    private bool _init;
    private bool _prefixFilterEnabled;
    private const string Prefix = "Medieval";
    private (int start, int end) _lastEntityIndices;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;

        _placement.DirectionChanged += OnDirectionChanged;
        _placement.PlacementChanged += ClearSelection;
        _cfg.OnValueChanged(CCCVars.MedievalPrefixFilterEnabled, OnPrefixFilterChanged, true);
    }

    private void OnPrefixFilterChanged(bool enabled)
    {
        _prefixFilterEnabled = enabled;

        if (_window == null || _window.Disposed)
            return;

        BuildEntityList(_window.SearchBar.Text);
    }

    private void OnEntityReplaceToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        if (args.Pressed)
            _placement.Replacement ^= true;

        args.Button.Pressed = args.Pressed;
    }

    private void OnEntityEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        if (args.Pressed)
            _placement.ToggleEraser();

        args.Button.Pressed = args.Pressed;
        _window.OverrideMenu.Disabled = args.Pressed;
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
            BuildEntityList(_window.SearchBar.Text);
            _window.Open();
            UpdateEntityDirectionLabel();
            _window.SearchBar.GrabKeyboardFocus();
        }
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.Close();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<EntitySpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        _window.OnClose += WindowClosed;
        _window.ReplaceButton.Pressed = _placement.Replacement;
        _window.ReplaceButton.OnToggled += OnEntityReplaceToggled;
        _window.EraseButton.Pressed = _placement.Eraser;
        _window.EraseButton.OnToggled += OnEntityEraseToggled;
        _window.OverrideMenu.OnItemSelected += OnEntityOverrideSelected;
        _window.SearchBar.OnTextChanged += OnEntitySearchChanged;
        _window.ClearButton.OnPressed += OnEntityClearPressed;
        _window.PrototypeScrollContainer.OnScrolled += UpdateVisiblePrototypes;
        _window.OnResized += UpdateVisiblePrototypes;
        BuildEntityList();
    }

    private void WindowClosed()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
            _window.SelectedButton = null;
        }

        _placement.Clear();
    }

    private void ClearSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed)
            return;

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
            _window.SelectedButton = null;
        }

        _window.EraseButton.Pressed = false;
        _window.OverrideMenu.Disabled = false;
    }

    private void OnEntityOverrideSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _window.OverrideMenu.SelectId(args.Id);

        if (_placement.CurrentMode != null)
        {
            var newObjInfo = new PlacementInformation
            {
                PlacementOption = _placement.AllModeNames[args.Id],
                EntityType = _placement.CurrentPermission!.EntityType,
                Range = 2,
                IsTile = _placement.CurrentPermission.IsTile
            };

            _placement.Clear();
            _placement.BeginPlacing(newObjInfo);
        }
    }

    private void OnEntitySearchChanged(LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        BuildEntityList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnEntityClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();
        _window.SearchBar.Clear();
        BuildEntityList(string.Empty);
    }

    private void BuildEntityList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed)
            return;

        _shownEntities.Clear();
        _window.PrototypeList.RemoveAllChildren();
        _lastEntityIndices = (0, -1);
        _window.SelectedButton = null;
        searchStr = searchStr?.ToLowerInvariant();

        var categoryFilter = _cfg.GetCVar(CVars.EntitiesCategoryFilter);
        _prototypes.TryIndex<EntityCategoryPrototype>(categoryFilter, out var filter);

        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (prototype.HideSpawnMenu)
                continue;

            if (!IsVisiblePrototype(prototype))
                continue;

            if (filter is not null && !prototype.Categories.Contains(filter))
                continue;

            if (searchStr != null && !DoesEntityMatchSearch(prototype, searchStr))
                continue;

            _shownEntities.Add(prototype);
        }

        _shownEntities.Sort((a, b) =>
        {
            var namesComparison = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            if (namesComparison == 0)
                return string.Compare(a.EditorSuffix, b.EditorSuffix, StringComparison.Ordinal);

            return namesComparison;
        });

        _window.PrototypeList.TotalItemCount = _shownEntities.Count;
        _window.PrototypeScrollContainer.SetScrollValue(new Vector2(0, 0));
        UpdateVisiblePrototypes();
    }

    private static bool DoesEntityMatchSearch(EntityPrototype prototype, string searchStr)
    {
        if (string.IsNullOrEmpty(searchStr))
            return true;

        if (prototype.ID.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (prototype.EditorSuffix != null &&
            prototype.EditorSuffix.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (string.IsNullOrEmpty(prototype.Name))
            return false;

        return prototype.Name.Contains(searchStr, StringComparison.InvariantCultureIgnoreCase);
    }

    private bool IsVisiblePrototype(EntityPrototype prototype)
    {
        return !_prefixFilterEnabled || prototype.ID.StartsWith(Prefix, StringComparison.Ordinal);
    }

    private void UpdateEntityDirectionLabel()
    {
        if (_window == null || _window.Disposed)
            return;

        _window.RotationLabel.Text = _placement.Direction.ToString();
    }

    private void OnDirectionChanged(object? sender, EventArgs e)
    {
        UpdateEntityDirectionLabel();
    }

    private void UpdateVisiblePrototypes()
    {
        if (_window == null || _window.Disposed)
            return;

        var height = _window.MeasureButton.DesiredSize.Y + PrototypeListContainer.Separation;
        var offset = Math.Max(-_window.PrototypeList.Position.Y, 0);
        var startIndex = (int)Math.Floor(offset / height);
        _window.PrototypeList.ItemOffset = startIndex;

        var (prevStart, prevEnd) = _lastEntityIndices;

        var endIndex = startIndex - 1;
        var spaceUsed = -height;

        while (spaceUsed < _window.PrototypeList.Parent!.Height)
        {
            spaceUsed += height;
            endIndex += 1;
        }

        endIndex = Math.Min(endIndex, _shownEntities.Count - 1);

        if (endIndex == prevEnd && startIndex == prevStart)
            return;

        _lastEntityIndices = (startIndex, endIndex);

        for (var i = prevStart; i < startIndex && i <= prevEnd; i++)
        {
            var control = (EntitySpawnButton) _window.PrototypeList.GetChild(0);
            DebugTools.Assert(control.Index == i);
            _window.PrototypeList.RemoveChild(control);
        }

        for (var i = prevEnd; i > endIndex && i >= prevStart; i--)
        {
            var control = (EntitySpawnButton) _window.PrototypeList.GetChild(_window.PrototypeList.ChildCount - 1);
            DebugTools.Assert(control.Index == i);
            _window.PrototypeList.RemoveChild(control);
        }

        for (var i = Math.Min(prevStart - 1, endIndex); i >= startIndex; i--)
        {
            InsertEntityButton(_shownEntities[i], true, i);
        }

        for (var i = Math.Max(prevEnd + 1, startIndex); i <= endIndex; i++)
        {
            InsertEntityButton(_shownEntities[i], false, i);
        }
    }

    private void InsertEntityButton(EntityPrototype prototype, bool insertFirst, int index)
    {
        if (_window == null || _window.Disposed)
            return;

        var button = _window.InsertEntityButton(prototype, insertFirst, index);
        button.ActualButton.OnToggled += OnEntityButtonToggled;
    }

    private void OnEntityButtonToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        var item = (EntitySpawnButton)args.Button.Parent!;
        if (_window.SelectedButton == item)
        {
            _window.SelectedButton = null;
            _window.SelectedPrototype = null;
            _placement.Clear();
            return;
        }

        if (_window.SelectedButton != null)
        {
            _window.SelectedButton.ActualButton.Pressed = false;
        }

        _window.SelectedButton = null;
        _window.SelectedPrototype = null;

        var overrideMode = _placement.AllModeNames[_window.OverrideMenu.SelectedId];
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = overrideMode != IPlacementManager.DefaultModeName ? overrideMode : item.Prototype.PlacementMode,
            EntityType = item.PrototypeID,
            Range = 2,
            IsTile = false
        };

        _placement.BeginPlacing(newObjInfo);

        _window.SelectedButton = item;
        _window.SelectedPrototype = item.Prototype;
    }
}