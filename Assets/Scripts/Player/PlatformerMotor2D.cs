using System.Collections.Generic;
using UnityEngine;

/// <summary>Rigidbody motor stepped only by the network state authority, before physics.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
public sealed class PlatformerMotor2D : MonoBehaviour
{
    [Min(0)] public float walkSpeed = 6;
    [Min(0)] public float acceleration = 40;
    [Min(0)] public float deceleration = 50;
    [Min(0)] public float airAcceleration = 25;
    [Min(0)] public float jumpSpeed = 11;
    [Min(0)] public float climbSpeed = 4;
    [Min(.01f)] public float maximumFallSpeed = 20;
    [Min(.001f)] public float groundCheckDistance = .05f;
    [Range(0, 1)] public float minimumGroundNormalY = .65f;
    public LayerMask groundLayers = ~0;

    public bool IsGrounded { get; private set; }
    public bool IsClimbing { get; private set; }
    public bool JumpHeld { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public Vector2 Velocity
    {
#if UNITY_6000_0_OR_NEWER
        get => body.linearVelocity;
        private set => body.linearVelocity = value;
#else
        get => body.velocity;
        private set => body.velocity = value;
#endif
    }

    Rigidbody2D body;
    Collider2D[] shapes;
    float normalGravity;
    bool climbDetached;
    readonly List<RaycastHit2D> groundHits = new List<RaycastHit2D>();
    readonly List<Collider2D> overlaps = new List<Collider2D>();

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        shapes = GetComponentsInChildren<Collider2D>(true);
        normalGravity = body.gravityScale;
    }

    public void Step(float walk, float climb, bool jump, bool movementEnabled, float deltaTime)
    {
        if (body == null) Awake();
        if (deltaTime <= 0 || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) return;
        bool jumpPressed = jump && !JumpHeld;
        JumpHeld = jump;
        body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        body.angularVelocity = 0;

        if (!movementEnabled)
        {
            Velocity = Vector2.zero;
            body.gravityScale = 0;
            IsClimbing = IsGrounded = false;
            climbDetached = false;
            return;
        }

        walk = ClampInput(walk);
        climb = ClampInput(climb);
        Vector2 velocity = Velocity;
        IsGrounded = velocity.y <= .05f && CheckGround();
        bool inClimbVolume = CheckClimbVolume();
        if (!inClimbVolume || Mathf.Abs(climb) <= .01f) climbDetached = false;
        IsClimbing = !climbDetached && inClimbVolume && (IsClimbing || Mathf.Abs(climb) > .01f);

        if (jumpPressed && (IsGrounded || IsClimbing))
        {
            velocity.y = jumpSpeed;
            IsGrounded = IsClimbing = false;
            climbDetached = inClimbVolume;
        }
        else if (IsClimbing)
        {
            // Releasing W/S holds position on a ladder; walking out restores gravity.
            velocity.y = climb * climbSpeed;
        }

        float rate = Mathf.Abs(walk) < .01f ? deceleration : IsGrounded ? acceleration : airAcceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, walk * walkSpeed, rate * deltaTime);
        velocity.y = Mathf.Max(velocity.y, -maximumFallSpeed);
        body.gravityScale = IsClimbing ? 0 : normalGravity;
        Velocity = velocity;
        if (Mathf.Abs(walk) > .01f) FacingDirection = walk > 0 ? 1 : -1;
    }

    static float ClampInput(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp(value, -1, 1);

    bool CheckGround()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLayers);
        groundHits.Clear();
        body.Cast(Vector2.down, filter, groundHits, groundCheckDistance);
        foreach (var hit in groundHits)
            if (hit.collider != null && hit.normal.y >= minimumGroundNormalY) return true;
        return false;
    }

    bool CheckClimbVolume()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        foreach (var shape in shapes)
        {
            if (shape == null || !shape.enabled || !shape.gameObject.activeInHierarchy || shape.attachedRigidbody != body) continue;
            overlaps.Clear();
            shape.Overlap(filter, overlaps);
            foreach (var overlap in overlaps)
            {
                if (!overlap.isTrigger || overlap.attachedRigidbody == body) continue;
                var climbable = overlap.GetComponentInParent<Climbable2D>();
                if (climbable != null && climbable.isActiveAndEnabled) return true;
            }
        }
        return false;
    }

    void OnDisable()
    {
        if (body != null) body.gravityScale = normalGravity;
        IsClimbing = IsGrounded = JumpHeld = false;
    }
}
