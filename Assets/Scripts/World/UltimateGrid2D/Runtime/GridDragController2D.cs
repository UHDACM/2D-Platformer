using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateGrid2D
{
    public class GridDragController2D : MonoBehaviour
    {
        public PlacementGrid2D grid;
        public GridOverlay2D overlay;
        public Camera worldCamera;
        public LayerMask pickLayers = ~0;
        [SerializeField] bool buildMode = true;
        public bool BuildMode => buildMode;
        GridPlaceable2D dragging;
        Vector3 originalPosition, grabOffset;
        Quaternion originalRotation;
        int candidateRotation;
        Vector2Int candidate;
        Rigidbody2D[] bodies;
        bool[] simulationStates;
        void Start()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (grid == null || worldCamera == null)
            {
                Debug.LogError("GridDragController2D requires a grid and camera.", this); enabled = false; return;
            }
            if (overlay != null) overlay.SetVisible(buildMode);
        }
        public void SetBuildMode(bool value)
        {
            CancelDrag(); buildMode = value;
            if (overlay != null) overlay.SetVisible(value);
        }
        bool ReadPointer(out Vector2 screen, out bool pressed, out bool released, out bool cancel)
        {
            screen = Vector2.zero; pressed = released = cancel = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            screen = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            released = Mouse.current.leftButton.wasReleasedThisFrame;
            cancel = Mouse.current.rightButton.wasPressedThisFrame || (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame);
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            screen = Input.mousePosition; pressed = Input.GetMouseButtonDown(0); released = Input.GetMouseButtonUp(0);
            cancel = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape); return true;
#else
            return false;
#endif
        }
        GridPlaceable2D Pick(Vector3 point)
        {
            Physics2D.SyncTransforms();
            GridPlaceable2D best = null; int order = int.MinValue;
            foreach (var hit in Physics2D.OverlapPointAll(point, pickLayers))
            {
                var item = hit.GetComponentInParent<GridPlaceable2D>();
                if (item == null || !item.enabled || item.grid != grid || item.definition == null) continue;
                var sr = item.GetComponentInChildren<SpriteRenderer>();
                int current = sr == null ? 0 : sr.sortingOrder;
                if (best == null || current > order) { best = item; order = current; }
            }
            return best;
        }
        bool RotatePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
        void Update()
        {
            if (!buildMode) return;
            if (!ReadPointer(out var screen, out bool pressed, out bool released, out bool cancel)) { CancelDrag(); return; }
            if (cancel) { CancelDrag(); return; }
            Ray ray = worldCamera.ScreenPointToRay(screen);
            Plane plane = new Plane(Vector3.forward, grid.transform.position);
            if (!plane.Raycast(ray, out float distance)) { CancelDrag(); return; }
            Vector3 world = ray.GetPoint(distance);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (dragging == null)
            {
                var hovered = overUI ? null : Pick(world);
                if (overlay != null) overlay.Show(hovered, hovered == null ? Vector2Int.zero : hovered.Anchor);
                if (!pressed || hovered == null || !hovered.movable) return;
                dragging = hovered; originalPosition = dragging.transform.position; grabOffset = originalPosition - world;
                originalRotation = dragging.transform.rotation;
                candidateRotation = dragging.QuarterTurns;
                bodies = dragging.GetComponentsInChildren<Rigidbody2D>(); simulationStates = new bool[bodies.Length];
                for (int i = 0; i < bodies.Length; i++) { simulationStates[i] = bodies[i].simulated; bodies[i].simulated = false; }
            }
            if (!overUI && RotatePressed())
            {
                candidateRotation = (candidateRotation + 1) % 4;
                grabOffset = new Vector3(-grabOffset.y, grabOffset.x, grabOffset.z);
            }
            candidate = grid.SnapAnchor(world + grabOffset, dragging.definition.RotatedPivot(candidateRotation));
            dragging.ApplyPose(candidate, candidateRotation);
            if (overlay != null) overlay.Show(dragging, candidate, candidateRotation);
            if (released)
            {
                if (overUI || !grid.TryPlace(dragging, candidate, candidateRotation)) RestorePose();
                EndDrag();
            }
        }
        public void CancelDrag()
        {
            RestorePose();
            EndDrag();
        }
        void RestorePose()
        {
            if (dragging != null) dragging.transform.SetPositionAndRotation(originalPosition, originalRotation);
        }
        void EndDrag()
        {
            if (bodies != null)
                for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null) bodies[i].simulated = simulationStates[i];
            bodies = null; simulationStates = null; dragging = null;
            if (overlay != null) overlay.HideHighlights();
            Physics2D.SyncTransforms();
        }
        void OnDisable() { CancelDrag(); }
        void OnApplicationFocus(bool focus) { if (!focus) CancelDrag(); }
    }
}
