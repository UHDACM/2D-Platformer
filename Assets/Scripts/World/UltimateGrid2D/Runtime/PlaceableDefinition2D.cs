using System.Collections.Generic;
using UnityEngine;

namespace UltimateGrid2D
{
    [CreateAssetMenu(menuName = "Ultimate Grid 2D/Placeable Definition")]
    public class PlaceableDefinition2D : ScriptableObject
    {
        public Enums.Maps GameMap = Enums.Maps.Default;
        
        public Vector2Int size = Vector2Int.one;
        [Tooltip("Optional occupied cells relative to bottom-left. Empty = full rectangle.")]
        public Vector2Int[] map = new Vector2Int[0];
        [Tooltip("Only bypasses board boundaries, never occupied cells.")]
        public bool ignoreBoundaries;
        [Tooltip("Pivot measured in cell units from bottom-left. (0.5,0.5) for a centered 1x1; (1.5,0.5) for a centered 3x1.")]
        public Vector2 pivot = new Vector2(.5f, .5f);
        public IEnumerable<Vector2Int> Cells()
        {
            if (map != null && map.Length > 0)
            {
                foreach (var cell in map) yield return cell;
            }
            else
                for (int x = 0; x < Mathf.Max(1, size.x); x++)
                    for (int y = 0; y < Mathf.Max(1, size.y); y++)
                        yield return new Vector2Int(x, y);
        }
        void OnValidate() { size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); }
    }
}
