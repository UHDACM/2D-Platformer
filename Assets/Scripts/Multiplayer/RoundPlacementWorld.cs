using System.Collections.Generic;
using System.Linq;
using UltimateGrid2D;
using UnityEngine;

// Rebuilds ordinary grid objects from replicated state, so late joiners also see committed pieces.
public sealed class RoundPlacementWorld : MonoBehaviour
{
    public GameManager Manager { get; set; }
    public PlacementGrid2D Grid { get; private set; }
    public bool Ready => Grid != null;
    readonly Dictionary<int, GridPlaceable2D> items = new Dictionary<int, GridPlaceable2D>();
    readonly Dictionary<int, RoundObstacleData> applied = new Dictionary<int, RoundObstacleData>();
    readonly List<GridDragController2D> controllers = new List<GridDragController2D>();
    Texture2D texture;
    Sprite square;
    Transform boardMinimum, boardMaximum;
    GroupFollowCamera2D buildCamera;
    bool cleared;
    public Vector3 DefaultSpawnPosition => Grid.PivotPosition(new Vector2Int(2, 3), Vector2.one * .5f);

    void Initialize()
    {
        if (Grid != null) return;
        Grid = FindObjectsOfType<PlacementGrid2D>().OrderBy(grid => grid.name).FirstOrDefault();
        bool emptyMap = Grid == null;
        if (emptyMap)
        {
            // Empty maps still get the same default board on every peer.
            var board = new GameObject("Round placement grid");
            board.SetActive(false);
            board.transform.SetParent(transform);
            board.transform.position = new Vector3(-10, -6, 0);
            Grid = board.AddComponent<PlacementGrid2D>();
            var overlay = board.AddComponent<GridOverlay2D>();
            overlay.grid = Grid;
            board.SetActive(true);
        }
        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        square = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        boardMinimum = new GameObject("Board minimum").transform;
        boardMaximum = new GameObject("Board maximum").transform;
        boardMinimum.SetParent(transform);
        boardMaximum.SetParent(transform);
        boardMinimum.position = Grid.transform.position;
        boardMaximum.position = Grid.PivotPosition(Grid.dimensions, Vector2.zero);
        if (emptyMap)
        {
            var ground = CreateBox("Default ground", Grid.PivotPosition(Vector2Int.zero,
                new Vector2(Grid.dimensions.x * .5f, -.5f)), new Vector2(Grid.dimensions.x, 1) * Grid.cellSize, Color.gray);
            ground.AddComponent<BoxCollider2D>();
        }
        // Map designers can supply their own finish trigger and spawn points.
        if (!FindObjectsOfType<RoundOutcomeZone2D>().Any(zone => zone.finishLine))
        {
            var finish = CreateBox("Finish line", Grid.PivotPosition(new Vector2Int(Grid.dimensions.x - 2, 2), Vector2.one * .5f),
                new Vector2(.3f, 3) * Grid.cellSize, Color.green);
            finish.AddComponent<RoundOutcomeZone2D>().finishLine = true;
        }
    }
    GameObject CreateBox(string name, Vector3 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.color = color;
        return go;
    }

    void Update()
    {
        if (cleared || Manager == null || Manager.Object == null || !Manager.Object.IsValid ||
            (SceneRouter.Instance != null && !SceneRouter.Instance.GameSceneReady)) return;
        Synchronize();
        if (buildCamera == null) buildCamera = GroupFollowCamera2D.ForMainCamera();
        if (buildCamera != null)
        {
            if (Manager.Phase == RoundPhase.Build)
            {
                buildCamera.Register(boardMinimum);
                buildCamera.Register(boardMaximum);
            }
            else
            {
                buildCamera.Unregister(boardMinimum);
                buildCamera.Unregister(boardMaximum);
            }
        }
        if (controllers.Count == 0 && Camera.main != null)
        {
            controllers.AddRange(FindObjectsOfType<GridDragController2D>().Where(controller => controller.grid == Grid));
            if (controllers.Count == 0)
            {
                var controller = gameObject.AddComponent<GridDragController2D>();
                controller.grid = Grid;
                controller.worldCamera = Camera.main;
                controller.overlay = Grid.GetComponent<GridOverlay2D>();
                controllers.Add(controller);
            }
            foreach (var controller in controllers)
            {
                controller.CanDrag = CanDrag;
                controller.PlacementRequest = RequestPlacement;
            }
        }
        foreach (var controller in controllers)
            if (controller != null && controller.BuildMode != (Manager.Phase == RoundPhase.Build))
                controller.SetBuildMode(Manager.Phase == RoundPhase.Build);
    }

    public void Synchronize()
    {
        if (cleared) return;
        Initialize();
        foreach (int id in items.Keys.Where(id => !Manager.Obstacles.ContainsKey(id)).ToArray()) RemoveItem(id);
        var changes = Manager.Obstacles.Where(pair => !applied.TryGetValue(pair.Key, out var old) ||
            old.Anchor != pair.Value.Anchor || old.Rotation != pair.Value.Rotation || (bool)old.Committed != (bool)pair.Value.Committed).ToArray();
        // Release every changed reservation first, then apply the complete new snapshot.
        foreach (var pair in changes)
            if (items.TryGetValue(pair.Key, out var existing))
            {
                CancelDragFor(existing);
                Grid.Remove(existing);
            }
        foreach (var pair in changes)
        {
            var data = pair.Value;
            if (!items.TryGetValue(pair.Key, out var item))
            {
                item = CreateItem(data.Definition, data.Owner.RawEncoded);
                if (item == null) continue;
                items.Add(pair.Key, item);
            }
            if (!Grid.TryPlace(item, data.Anchor, data.Rotation)) continue;
            item.movable = !data.Committed;
            applied[pair.Key] = data;
        }
    }

    GridPlaceable2D CreateItem(int definitionIndex, int colorIndex)
    {
        if (definitionIndex < 0 || definitionIndex >= Manager.placeableDefinitions.Length) return null;
        var definition = Manager.placeableDefinitions[definitionIndex];
        if (definition == null) return null;
        var go = new GameObject($"Round piece - {definition.name}");
        go.transform.SetParent(transform);
        var item = go.AddComponent<GridPlaceable2D>();
        item.grid = Grid;
        item.definition = definition;
        foreach (var cell in definition.Cells())
        {
            var visual = new GameObject("Cell");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = ((Vector2)cell + Vector2.one * .5f - definition.pivot) * Grid.cellSize;
            visual.transform.localScale = Vector3.one * Grid.cellSize;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.sortingOrder = 10;
            renderer.color = Color.HSVToRGB(Mathf.Repeat(colorIndex * .17f, 1), .65f, 1);
            visual.AddComponent<BoxCollider2D>();
        }
        return item;
    }

    public bool TryCreateGrant(int id, int definitionIndex, int colorIndex, out Vector2Int anchor)
    {
        anchor = Vector2Int.zero;
        var item = CreateItem(definitionIndex, colorIndex);
        if (item == null) return false;
        for (int y = Grid.dimensions.y - 1; y >= 0; y--)
            for (int x = 0; x < Grid.dimensions.x; x++)
            {
                anchor = new Vector2Int(x, y);
                if (!Grid.TryPlace(item, anchor)) continue;
                items.Add(id, item);
                return true;
            }
        Destroy(item.gameObject);
        return false;
    }

    public bool TryCommit(int id, Vector2Int anchor, int rotation)
    {
        if (!Ready || anchor.x < 0 || anchor.y < 0 || anchor.x >= Grid.dimensions.x || anchor.y >= Grid.dimensions.y) return false;
        return items.TryGetValue(id, out var item) && Grid.TryPlace(item, anchor, rotation);
    }
    bool CanDrag(GridPlaceable2D item)
    {
        int id = IdFor(item);
        return Manager.Phase == RoundPhase.Build && Manager.Obstacles.TryGet(id, out var data) &&
            data.Owner == Manager.Runner.LocalPlayer && data.Round == Manager.RoundNumber && !data.Committed;
    }
    bool RequestPlacement(GridPlaceable2D item, Vector2Int anchor, int rotation)
    {
        if (CanDrag(item) && Grid.CanPlace(item, anchor, rotation))
            Manager.RPC_Place(IdFor(item), Manager.RoundNumber, anchor, rotation);
        // Keep the old reservation/pose until the host validates the request on its next tick.
        return false;
    }
    int IdFor(GridPlaceable2D item)
    {
        foreach (var pair in items) if (pair.Value == item) return pair.Key;
        return -1;
    }
    void RemoveItem(int id)
    {
        var item = items[id];
        if (item != null)
        {
            CancelDragFor(item);
            if (Grid != null) Grid.Remove(item);
            Destroy(item.gameObject);
        }
        items.Remove(id);
        applied.Remove(id);
    }
    void CancelDragFor(GridPlaceable2D item)
    {
        foreach (var controller in controllers)
            if (controller != null) controller.CancelDragFor(item);
    }
    public void Clear()
    {
        if (cleared) return;
        cleared = true;
        if (buildCamera != null)
        {
            buildCamera.Unregister(boardMinimum);
            buildCamera.Unregister(boardMaximum);
        }
        foreach (var controller in controllers)
            if (controller != null)
            {
                controller.CancelDrag();
                controller.CanDrag = null;
                controller.PlacementRequest = null;
                controller.SetBuildMode(false);
            }
        foreach (int id in items.Keys.ToArray()) RemoveItem(id);
        if (square != null) Destroy(square);
        if (texture != null) Destroy(texture);
    }
    void OnDestroy() => Clear();
}
