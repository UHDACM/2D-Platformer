using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class RoundOutcomeZone2D : MonoBehaviour
{
    [Tooltip("Off = hazard/death zone. On = finish line (one point per round).")]
    public bool finishLine;
    void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) => Resolve(other);
    void OnTriggerStay2D(Collider2D other) => Resolve(other);
    void Resolve(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerMovementMultiplayer>();
        if (player != null && GameManager.Instance != null) GameManager.Instance.ResolvePlayer(player, finishLine);
    }
}
