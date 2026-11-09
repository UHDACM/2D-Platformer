using System;
using Fusion;
using UnityEngine;

namespace UltimateGrid2D
{
    [DisallowMultipleComponent]
    public class GridPlaceable2D : MonoBehaviourPlus
    {
        public override string prettyName => "GridPlaceable2D";
        public override string description => "change the settings for each placable";
        
        [ReadOnly]
        public PlacementGrid2D grid;
        public PlaceableDefinition2D definition;
        public bool movable = true;
        [SerializeField, Range(0, 3)] int quarterTurns;
        public int QuarterTurns { get => PlaceableDefinition2D.NormalizeRotation(quarterTurns); internal set => quarterTurns = PlaceableDefinition2D.NormalizeRotation(value); }
        public Vector2Int Anchor { get; internal set; }
        internal void ApplyPose(Vector2Int anchor, int rotation)
        {
            transform.SetPositionAndRotation(grid.PivotPosition(anchor, definition.RotatedPivot(rotation)),
                Quaternion.Euler(0, 0, 90 * PlaceableDefinition2D.NormalizeRotation(rotation)));
        }
        
        void Start()
        {
            grid = PlacementGrid2D.Instance;
            
            
            if (grid == null || definition == null)
            {
                Debug.LogError("Assign a grid and definition to " + name, this);
                enabled = false;
                return;
            }
            if (!grid.TryPlace(this, grid.SnapAnchor(transform.position, definition.RotatedPivot(QuarterTurns))))
            {
                Debug.LogError("Invalid initial placement: " + name + ". Fix the overlap or board bounds.", this);
                gameObject.SetActive(false);
            }
        }
        void OnDestroy() { if (grid != null) grid.Remove(this); }
        // Disabling a placed object keeps its cells reserved. Destroy it to release them.
    }
}
