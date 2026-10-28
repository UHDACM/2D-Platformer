using System.Collections.Generic;
using UnityEngine;

namespace UltimateGrid2D
{
    public class GridOverlay2D : MonoBehaviour
    {
        public PlacementGrid2D grid;
        public Color lineColor = new Color(1, 1, 1, .15f);
        public Color validColor = new Color(.2f, 1, .45f, .35f);
        public Color invalidColor = new Color(1, .2f, .2f, .45f);
        public int sortingOrder = 100;
        readonly List<SpriteRenderer> highlights = new List<SpriteRenderer>();
        GameObject lines;
        Sprite square;
        Texture2D texture;
        void Awake()
        {
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            square = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            lines = new GameObject("Grid lines"); lines.transform.SetParent(transform, false);
            for (int x = 0; x <= grid.dimensions.x; x++)
                MakeLine(new Vector2(x * grid.cellSize, grid.dimensions.y * grid.cellSize / 2), new Vector2(.025f, grid.dimensions.y * grid.cellSize));
            for (int y = 0; y <= grid.dimensions.y; y++)
                MakeLine(new Vector2(grid.dimensions.x * grid.cellSize / 2, y * grid.cellSize), new Vector2(grid.dimensions.x * grid.cellSize, .025f));
        }
        SpriteRenderer MakeSprite(string label, Transform parent)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = square; sr.sortingOrder = sortingOrder;
            return sr;
        }
        void MakeLine(Vector2 offset, Vector2 size)
        {
            var sr = MakeSprite("Line", lines.transform); sr.color = lineColor;
            sr.transform.position = grid.transform.position + (Vector3)offset;
            sr.transform.localScale = new Vector3(size.x, size.y, 1);
        }
        public void Show(GridPlaceable2D item, Vector2Int anchor)
        {
            HideHighlights();
            if (item == null || item.definition == null) return;
            Color color = grid.CanPlace(item, anchor) ? validColor : invalidColor;
            int i = 0;
            foreach (var offset in item.definition.Cells())
            {
                if (i == highlights.Count) highlights.Add(MakeSprite("Cell highlight", transform));
                var sr = highlights[i++]; sr.gameObject.SetActive(true); sr.color = color;
                sr.transform.position = grid.PivotPosition(anchor + offset, new Vector2(.5f, .5f));
                sr.transform.localScale = Vector3.one * grid.cellSize * .94f;
            }
        }
        public void HideHighlights() { foreach (var sr in highlights) sr.gameObject.SetActive(false); }
        public void SetVisible(bool visible) { if (lines != null) lines.SetActive(visible); if (!visible) HideHighlights(); }
        void OnDestroy() { if (square != null) Destroy(square); if (texture != null) Destroy(texture); }
    }
}
