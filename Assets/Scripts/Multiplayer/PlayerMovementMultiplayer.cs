using Fusion;
using UnityEngine;

[RequireComponent(typeof(PlatformerMotor2D))]
public class PlayerMovementMultiplayer : NetworkBehaviour
{
    PlatformerMotor2D motor;
    GroupFollowCamera2D followCamera;

    // Presentation hooks for the character/animation work, available on every peer.
    [Networked] public NetworkBool IsGrounded { get; private set; }
    [Networked] public NetworkBool IsClimbing { get; private set; }
    [Networked] public Vector2 MovementVelocity { get; private set; }
    [Networked] public int FacingDirection { get; private set; }

    void Awake() => motor = GetComponent<PlatformerMotor2D>();

    public override void Spawned()
    {
        if (HasStateAuthority) FacingDirection = 1;
    }

    public override void Render()
    {
        if (followCamera == null) followCamera = GroupFollowCamera2D.ForMainCamera();
        if (followCamera != null) followCamera.Register(transform);
    }

    public override void FixedUpdateNetwork()
    {
        // This project has Forecast disabled and no rollback physics addon.
        // The host consumes each owner's input; clients display NetworkTransform snapshots.
        if (!HasStateAuthority) return;
        if (motor == null) motor = GetComponent<PlatformerMotor2D>();

        bool canMove = GameManager.Instance != null && GameManager.Instance.Object != null &&
            GameManager.Instance.Object.IsValid && GameManager.Instance.currentGameMode == Enums.Gamemode.PlayMode;
        bool hasInput = GetInput(out NetworkInputData input);
        // Missing packets stop movement without manufacturing a release/re-press of a held jump.
        motor.Step(hasInput ? input.moveDirection.x : 0, hasInput ? input.climbDirection : 0,
            hasInput ? input.moveDirection.y > .5f : motor.JumpHeld, canMove, Runner.DeltaTime);
        IsGrounded = motor.IsGrounded;
        IsClimbing = motor.IsClimbing;
        MovementVelocity = motor.Velocity;
        FacingDirection = motor.FacingDirection;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) => ReleaseCamera();
    void OnDisable() => ReleaseCamera();
    void ReleaseCamera()
    {
        if (followCamera != null) followCamera.Unregister(transform);
    }
}
