using UnityEngine;

/// <summary>Add to a ladder/rope trigger volume to allow W/S climbing.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class Climbable2D : MonoBehaviour
{
    void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnValidate() => GetComponent<BoxCollider2D>().isTrigger = true;
}
