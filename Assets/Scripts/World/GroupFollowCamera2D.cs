using System.Collections.Generic;
using UnityEngine;

/// <summary>One local camera frames the rendered bounds of all registered players.</summary>
[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class GroupFollowCamera2D : MonoBehaviour
{
    [Min(0)] public float padding = 2f;
    [Min(.01f)] public float minimumSize = 5f;
    [Min(0)] public float positionSmoothTime = .15f;
    [Min(0)] public float zoomSmoothTime = .2f;

    readonly Dictionary<Transform, SpriteRenderer[]> targets = new Dictionary<Transform, SpriteRenderer[]>();
    readonly List<Transform> removedTargets = new List<Transform>();
    Camera view;
    Vector3 positionVelocity;
    float zoomVelocity;
    bool hasFrame;

    public static GroupFollowCamera2D ForMainCamera()
    {
        Camera main = Camera.main;
        if (main == null || !main.orthographic) return null;
        var follow = main.GetComponent<GroupFollowCamera2D>();
        return follow != null ? follow : main.gameObject.AddComponent<GroupFollowCamera2D>();
    }

    void Awake() => view = GetComponent<Camera>();

    public void Register(Transform target)
    {
        if (target != null && !targets.ContainsKey(target))
            targets.Add(target, target.GetComponentsInChildren<SpriteRenderer>(true));
    }

    public void Unregister(Transform target)
    {
        // A destroyed Unity object still has a managed key that can be removed.
        if (!ReferenceEquals(target, null)) targets.Remove(target);
    }

    public static float RequiredSize(Bounds bounds, Vector3 center, float aspect, float padding, float minimumSize)
    {
        float halfWidth = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x));
        float halfHeight = Mathf.Max(Mathf.Abs(bounds.min.y - center.y), Mathf.Abs(bounds.max.y - center.y));
        return Mathf.Max(minimumSize, halfHeight + padding, (halfWidth + padding) / Mathf.Max(.01f, aspect));
    }

    void LateUpdate()
    {
        if (!view.orthographic) return;
        bool found = false;
        Bounds group = default;
        foreach (var pair in targets)
        {
            if (pair.Key == null) { removedTargets.Add(pair.Key); continue; }
            if (!pair.Key.gameObject.activeInHierarchy) continue;
            Bounds player = new Bounds(pair.Key.position, Vector3.zero);
            foreach (var sprite in pair.Value)
                if (sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy)
                    player.Encapsulate(sprite.bounds);
            if (!found) { group = player; found = true; }
            else group.Encapsulate(player);
        }
        foreach (var target in removedTargets) targets.Remove(target);
        removedTargets.Clear();
        if (!found) { ResetSmoothing(); return; }

        // Fusion Render has already applied interpolation. Never sample Rigidbody2D.position here.
        Vector3 destination = new Vector3(group.center.x, group.center.y, transform.position.z);
        transform.position = !hasFrame || positionSmoothTime <= 0 ? destination :
            Vector3.SmoothDamp(transform.position, destination, ref positionVelocity, positionSmoothTime);
        float required = RequiredSize(group, transform.position, view.aspect, Mathf.Max(0, padding), Mathf.Max(.01f, minimumSize));
        // Include the smoothed camera's lag in the fit. Expand immediately so no player is clipped;
        // contract smoothly when players regroup. No hard maximum can keep arbitrarily distant players visible.
        if (!hasFrame || required >= view.orthographicSize || zoomSmoothTime <= 0)
        {
            view.orthographicSize = required;
            zoomVelocity = 0;
        }
        else
            view.orthographicSize = Mathf.Max(required,
                Mathf.SmoothDamp(view.orthographicSize, required, ref zoomVelocity, zoomSmoothTime));
        hasFrame = true;
    }

    void ResetSmoothing()
    {
        hasFrame = false;
        positionVelocity = Vector3.zero;
        zoomVelocity = 0;
    }

    void OnDisable() => ResetSmoothing();
}
