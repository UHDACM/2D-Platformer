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
        public static int NormalizeRotation(int quarterTurns) => (quarterTurns % 4 + 4) % 4;

        // Rotate cell areas and the root pivot in the same normalized bounding box.
        public Vector2 RotatedPivot(int quarterTurns)
        {
            Vector2 result = pivot;
            Vector2Int bounds = size;
            for (int i = 0; i < NormalizeRotation(quarterTurns); i++)
            {
                result = new Vector2(bounds.y - result.y, result.x);
                bounds = new Vector2Int(bounds.y, bounds.x);
            }
            return result;
        }

        Vector2Int RotateCell(Vector2Int cell, int quarterTurns)
        {
            Vector2Int bounds = size;
            for (int i = 0; i < NormalizeRotation(quarterTurns); i++)
            {
                cell = new Vector2Int(bounds.y - 1 - cell.y, cell.x);
                bounds = new Vector2Int(bounds.y, bounds.x);
            }
            return cell;
        }

        public IEnumerable<Vector2Int> Cells(int quarterTurns = 0)
        {
            if (map != null && map.Length > 0)
            {
                foreach (var cell in map) yield return RotateCell(cell, quarterTurns);
            }
            else
                for (int x = 0; x < Mathf.Max(1, size.x); x++)
                    for (int y = 0; y < Mathf.Max(1, size.y); y++)
                        yield return RotateCell(new Vector2Int(x, y), quarterTurns);
        }
        void OnValidate() { size = new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); }
    }
}
