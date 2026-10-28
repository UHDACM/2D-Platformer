using System.Collections.Generic;
using UnityEngine;

namespace UltimateGrid2D
{
    public class PlacementGrid2D : MonoBehaviour
    {
        [Min(.01f)] public float cellSize = 1;
        public Vector2Int dimensions = new Vector2Int(20, 12);
        readonly Dictionary<Vector2Int, GridPlaceable2D> occupied = new Dictionary<Vector2Int, GridPlaceable2D>();
        // Board origin is this transform's position. World XY, unit scale, no rotation.
        public Vector3 PivotPosition(Vector2Int anchor, Vector2 pivot)
        {
            return transform.position + new Vector3((anchor.x + pivot.x) * cellSize, (anchor.y + pivot.y) * cellSize, 0);
        }
        public Vector2Int SnapAnchor(Vector3 worldPivot, Vector2 pivot)
        {
            Vector3 p = (worldPivot - transform.position) / cellSize;
            return new Vector2Int(Mathf.FloorToInt(p.x - pivot.x + .5f), Mathf.FloorToInt(p.y - pivot.y + .5f));
        }
        public bool CanPlace(GridPlaceable2D item, Vector2Int anchor)
        {
            if (item == null || item.definition == null) return false;
            foreach (var offset in item.definition.Cells())
            {
                var c = anchor + offset;
                if (!item.definition.ignoreBoundaries && (c.x < 0 || c.y < 0 || c.x >= dimensions.x || c.y >= dimensions.y)) return false;
                if (occupied.TryGetValue(c, out var other) && other != null && other != item) return false;
            }
            return true;
        }
        public bool TryPlace(GridPlaceable2D item, Vector2Int anchor)
        {
            if (!CanPlace(item, anchor)) return false;
            Remove(item);
            foreach (var offset in item.definition.Cells()) occupied[anchor + offset] = item;
            item.Anchor = anchor;
            item.transform.position = PivotPosition(anchor, item.definition.pivot);
            return true;
        }
        public void Remove(GridPlaceable2D item)
        {
            var keys = new List<Vector2Int>();
            foreach (var pair in occupied) if (pair.Value == item) keys.Add(pair.Key);
            foreach (var key in keys) occupied.Remove(key);
        }
        void OnValidate()
        {
            cellSize = Mathf.Max(.01f, cellSize);
            dimensions = new Vector2Int(Mathf.Max(1, dimensions.x), Mathf.Max(1, dimensions.y));
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 1, 1, .3f);
            for (int x = 0; x <= dimensions.x; x++)
                Gizmos.DrawLine(PivotPosition(new Vector2Int(x, 0), Vector2.zero), PivotPosition(new Vector2Int(x, dimensions.y), Vector2.zero));
            for (int y = 0; y <= dimensions.y; y++)
                Gizmos.DrawLine(PivotPosition(new Vector2Int(0, y), Vector2.zero), PivotPosition(new Vector2Int(dimensions.x, y), Vector2.zero));
        }
    }
}
