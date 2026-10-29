using UnityEngine;

namespace UltimateGrid2D
{
    [DisallowMultipleComponent]
    public class GridPlaceable2D : MonoBehaviour
    {
        public PlacementGrid2D grid;
        public PlaceableDefinition2D definition;
        public bool movable = true;
        public Vector2Int Anchor { get; internal set; }
        void Start()
        {
            if (grid == null || definition == null)
            {
                Debug.LogError("Assign a grid and definition to " + name, this);
                enabled = false;
                return;
            }
            if (!grid.TryPlace(this, grid.SnapAnchor(transform.position, definition.pivot)))
            {
                Debug.LogError("Invalid initial placement: " + name + ". Fix the overlap or board bounds.", this);
                gameObject.SetActive(false);
            }
        }
        void OnDestroy() { if (grid != null) grid.Remove(this); }
        // Disabling a placed object keeps its cells reserved. Destroy it to release them.
    }
}
