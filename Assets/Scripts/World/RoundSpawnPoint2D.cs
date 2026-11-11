using UnityEngine;

// Optional per-map spawn markers, ordered by slot. Without markers, the manager spaces players around its origin.
public sealed class RoundSpawnPoint2D : MonoBehaviour
{
    public int slot;
    void OnDrawGizmos() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, .4f); }
}
