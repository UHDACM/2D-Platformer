using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltimateGrid2D;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

// Dependency-free Edit Mode checks. Also runnable with -executeMethod CameraAndGridChecks.Run.
public static class CameraAndGridChecks
{
    static readonly List<Object> created = new List<Object>();
    static int checks;

    [MenuItem("Tools/Ultimate Grid 2D/Run Camera And Rotation Checks")]
    public static void Run()
    {
        checks = 0;
        try
        {
            RotationGeometry();
            PlacementTransactions();
            CancelRestoresPoseAndPhysics();
            CameraFraming();
#if ENABLE_INPUT_SYSTEM
            if (Application.isPlaying) DragWithKeyboardAndMouse();
#endif
            Debug.Log($"Camera and grid checks passed: {checks}");
        }
        finally
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }
    }

    static void Check(bool success, string message)
    {
        if (!success) throw new Exception(message);
        checks++;
    }

    static GameObject Make(string name)
    {
        var go = new GameObject(name);
        created.Add(go);
        return go;
    }

    static PlaceableDefinition2D Definition(Vector2Int size, Vector2 pivot)
    {
        var definition = ScriptableObject.CreateInstance<PlaceableDefinition2D>();
        created.Add(definition);
        definition.size = size;
        definition.pivot = pivot;
        return definition;
    }

    static GridPlaceable2D Item(PlacementGrid2D grid, PlaceableDefinition2D definition)
    {
        var item = Make("Test piece").AddComponent<GridPlaceable2D>();
        item.grid = grid;
        item.definition = definition;
        return item;
    }

    static void RotationGeometry()
    {
        var definition = Definition(new Vector2Int(3, 2), new Vector2(.25f, 1.25f));
        definition.map = new[] { Vector2Int.zero, Vector2Int.right, new Vector2Int(2, 0), Vector2Int.up };
        var expected = new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), Vector2Int.zero };
        Check(new HashSet<Vector2Int>(definition.Cells(1)).SetEquals(expected), "L footprint quarter-turn mismatch");
        Check(definition.RotatedPivot(1) == new Vector2(.75f, .25f), "Asymmetric pivot mismatch");
        Check(definition.Cells(4).SequenceEqual(definition.Cells()), "Four turns must restore cells");
        Check(definition.RotatedPivot(4) == definition.pivot, "Four turns must restore pivot");
        Check(definition.Cells(-1).SequenceEqual(definition.Cells(3)), "Negative turns must normalize");
        for (int rotation = 0; rotation < 4; rotation++)
        {
            var cells = definition.Cells(rotation).ToArray();
            var original = definition.Cells().ToArray();
            for (int i = 0; i < cells.Length; i++)
            {
                Vector3 visual = Quaternion.Euler(0, 0, rotation * 90) *
                    (Vector3)((Vector2)original[i] + Vector2.one * .5f - definition.pivot);
                Vector2 occupied = (Vector2)cells[i] + Vector2.one * .5f - definition.RotatedPivot(rotation);
                Check(Vector2.Distance(visual, occupied) < .0001f, "Visual and footprint pivots diverged");
            }
        }
        definition.map = Array.Empty<Vector2Int>();
        definition.size = new Vector2Int(3, 1);
        Check(new HashSet<Vector2Int>(definition.Cells(1)).SetEquals(new[] { Vector2Int.zero, Vector2Int.up, new Vector2Int(0, 2) }), "3x1 must become 1x3");
    }

    static void PlacementTransactions()
    {
        var grid = Make("Test board").AddComponent<PlacementGrid2D>();
        grid.dimensions = new Vector2Int(6, 6);
        grid.cellSize = .5f;
        grid.transform.position = new Vector3(-3, -4, 0);
        var platform = Item(grid, Definition(new Vector2Int(3, 1), new Vector2(1.5f, .5f)));
        var block = Item(grid, Definition(Vector2Int.one, Vector2.one * .5f));
        Check(grid.TryPlace(platform, new Vector2Int(1, 1)), "Initial placement failed");
        Check(grid.TryPlace(block, new Vector2Int(1, 3)), "Block placement failed");
        Vector3 original = platform.transform.position;
        Check(!grid.TryPlace(platform, new Vector2Int(1, 1), 1), "Rotated overlap accepted");
        Check(platform.QuarterTurns == 0 && platform.transform.position == original, "Rejected rotation changed pose");
        Check(!grid.CanPlace(block, new Vector2Int(3, 1)), "Rejected rotation released original cells");
        Check(!grid.TryPlace(platform, new Vector2Int(3, 4), 1), "Rotated boundary overflow accepted");
        Check(grid.TryPlace(platform, new Vector2Int(3, 1), 1), "Valid rotation failed");
        Check(grid.CanPlace(block, new Vector2Int(1, 1)), "Old cells were not released");
        Check(!grid.CanPlace(block, new Vector2Int(3, 3)), "Rotated cells were not reserved");
        Check(grid.SnapAnchor(platform.transform.position, platform.definition.RotatedPivot(1)) == platform.Anchor, "Negative-origin half-cell snap mismatch");
        Check(Quaternion.Angle(platform.transform.rotation, Quaternion.Euler(0, 0, 90)) < .001f, "Root did not rotate with footprint");
        platform.definition.ignoreBoundaries = true;
        Check(grid.TryPlace(platform, new Vector2Int(-1, -2), 1), "Ignore boundaries rejected outside rotation");
        Check(!grid.TryPlace(platform, new Vector2Int(1, 2), 1), "Ignore boundaries bypassed occupancy");
        grid.Remove(platform);
        Check(grid.CanPlace(block, new Vector2Int(-1, -2)) == false, "Block must still respect its own boundaries");
        platform.definition.ignoreBoundaries = false;
        platform.definition.size = new Vector2Int(2, 2);
        platform.definition.map = new[] { Vector2Int.zero, Vector2Int.right, Vector2Int.up };
        Check(grid.TryPlace(platform, new Vector2Int(3, 3), 1), "L placement failed");
        Check(grid.CanPlace(block, new Vector2Int(3, 4)), "Rotated L hole should remain free");
        Object.DestroyImmediate(platform.gameObject);
        Check(grid.CanPlace(block, new Vector2Int(4, 4)), "Destroy must release rotated cells");
    }

    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

    static void CancelRestoresPoseAndPhysics()
    {
        var grid = Make("Cancel board").AddComponent<PlacementGrid2D>();
        var item = Item(grid, Definition(new Vector2Int(3, 1), new Vector2(1.5f, .5f)));
        Check(grid.TryPlace(item, new Vector2Int(2, 2), 1), "Cancel setup failed");
        var controller = Make("Drag controller").AddComponent<GridDragController2D>();
        controller.grid = grid;
        var body = item.gameObject.AddComponent<Rigidbody2D>();
        for (int mode = 0; mode < 3; mode++)
        {
            Vector3 position = item.transform.position;
            Quaternion rotation = item.transform.rotation;
            Set(controller, "dragging", item);
            Set(controller, "originalPosition", position);
            Set(controller, "originalRotation", rotation);
            Set(controller, "bodies", new[] { body });
            Set(controller, "simulationStates", new[] { mode != 1 });
            body.simulated = false;
            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (mode == 0) controller.CancelDrag();
            else if (mode == 1) controller.SetBuildMode(false);
            else Invoke(controller, "OnDisable");
            Check(item.transform.position == position && Quaternion.Angle(item.transform.rotation, rotation) < .001f, "Cancel failed to restore full pose");
            Check(item.QuarterTurns == 1 && !grid.CanPlace(Item(grid, Definition(Vector2Int.one, Vector2.one * .5f)), new Vector2Int(2, 4)), "Cancel changed committed occupancy");
            Check(body.simulated == (mode != 1), "Cancel failed to restore prior simulation state");
        }
    }

    static void CameraFraming()
    {
        var bounds = new Bounds(Vector3.zero, new Vector3(20, 10, 0));
        Check(Mathf.Approximately(GroupFollowCamera2D.RequiredSize(bounds, Vector3.zero, 2, 1, 2), 6), "Landscape fit mismatch");
        Check(Mathf.Approximately(GroupFollowCamera2D.RequiredSize(bounds, Vector3.zero, .5f, 1, 2), 22), "Portrait fit mismatch");
        Check(Mathf.Approximately(GroupFollowCamera2D.RequiredSize(bounds, new Vector3(-5, 0, 0), 1, 1, 2), 16), "Lag-aware fit mismatch");
        var camera = Make("Test group camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.aspect = 1;
        camera.transform.position = new Vector3(0, 0, -10);
        var follow = camera.gameObject.AddComponent<GroupFollowCamera2D>();
        Invoke(follow, "Awake");
        follow.positionSmoothTime = 0;
        follow.zoomSmoothTime = 0;
        var left = Make("Left player").transform;
        var right = Make("Right player").transform;
        left.position = new Vector3(-20, 0, 0);
        right.position = new Vector3(20, 10, 0);
        follow.Register(left);
        follow.Register(right);
        follow.Register(right);
        Invoke(follow, "LateUpdate");
        Check(camera.transform.position == new Vector3(0, 5, -10), "Group center or camera depth mismatch");
        Check(camera.orthographicSize >= 22, "Both players must fit");
        right.gameObject.SetActive(false);
        Invoke(follow, "LateUpdate");
        Check(camera.transform.position.x == -20, "Inactive player should leave frame");
        right.gameObject.SetActive(true);
        follow.Unregister(left);
        Invoke(follow, "LateUpdate");
        Check(camera.transform.position.x == 20, "Unregistered player should leave frame");
        Object.DestroyImmediate(right.gameObject);
        Invoke(follow, "LateUpdate");
        Check(camera.transform.position.x == 20, "Empty group must hold camera pose");
        follow.Register(left);
        Invoke(follow, "LateUpdate");
        Check(camera.transform.position.x == -20, "New group after empty frame must snap cleanly");
    }

#if ENABLE_INPUT_SYSTEM
    static void DragWithKeyboardAndMouse()
    {
        // Batch Mode has no focused Game view. Use temporary settings, then restore the user's settings.
        var originalSettings = InputSystem.settings;
        var testSettings = Object.Instantiate(originalSettings);
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = testSettings;
        var mouse = InputSystem.AddDevice<Mouse>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            var grid = Make("Input board").AddComponent<PlacementGrid2D>();
            var item = Item(grid, Definition(new Vector2Int(3, 1), new Vector2(1.5f, .5f)));
            item.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(3, 1);
            Check(grid.TryPlace(item, new Vector2Int(3, 3)), "Input placement failed");
            var camera = Make("Input camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            camera.pixelRect = new Rect(0, 0, 800, 600);
            camera.transform.position = new Vector3(10, 6, -10);
            var controller = Make("Input controller").AddComponent<GridDragController2D>();
            controller.grid = grid;
            controller.worldCamera = camera;
            Vector2 screen = camera.WorldToScreenPoint(item.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left));
            InputSystem.Update();
            Check(mouse.leftButton.wasPressedThisFrame, "Synthetic mouse press was not processed");
            Invoke(controller, "Update");
            Check(typeof(GridDragController2D).GetField("dragging", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller) == item, "Synthetic pointer did not pick up the piece");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            InputSystem.Update();
            Check(keyboard.rKey.wasPressedThisFrame, "Synthetic R press was not processed");
            Invoke(controller, "Update");
            Check(Quaternion.Angle(item.transform.rotation, Quaternion.Euler(0, 0, 90)) < .001f, "R must rotate drag preview");
            Check(item.QuarterTurns == 0, "Preview must not commit rotation before drop");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen });
            InputSystem.Update();
            Invoke(controller, "Update");
            Check(item.QuarterTurns == 1, "Mouse release must commit valid rotated placement");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            InputSystem.Update();
            Invoke(controller, "Update");
            Check(item.QuarterTurns == 1, "R without dragging must leave piece unchanged");

            // Fixed pieces cannot be picked up or rotated, even with R and click together.
            item.movable = false;
            screen = camera.WorldToScreenPoint(item.transform.position);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen }.WithButton(MouseButton.Left));
            InputSystem.Update();
            Invoke(controller, "Update");
            Check(item.QuarterTurns == 1 && Quaternion.Angle(item.transform.rotation, Quaternion.Euler(0, 0, 90)) < .001f, "Fixed piece must ignore rotation");
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = originalSettings;
            Object.DestroyImmediate(testSettings);
        }
    }
#endif
}
