using System;
using Fusion;
using UnityEngine;

public class PlayerMovementMultiplayer : NetworkBehaviour
{
    private Rigidbody2D rb;
    private GroupFollowCamera2D followCamera;

    // Example: Synced network variables
    [Networked] public bool changedFlag { get; set; }

    // required to detect networked variable changes
    private ChangeDetector changeDetector;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void RegisterCameraTarget()
    {
        if (followCamera == null) followCamera = GroupFollowCamera2D.ForMainCamera();
        if (followCamera != null) followCamera.Register(transform);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (followCamera != null) followCamera.Unregister(transform);
    }

    private void OnDisable()
    {
        if (followCamera != null) followCamera.Unregister(transform);
    }

    // Once this player is spawned in
    public override void Spawned()
    {
        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState); // init
    }

    // After update() function, invoke any needed network changes
    public override void Render()
    {
        // Every visible player joins the local group; input authority does not gate framing.
        RegisterCameraTarget();
        
        // example on how to listen to changes in variables
        foreach (string change in changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(changedFlag):
                    Debug.Log($"changed flag: {changedFlag}");
                    break;
            }
        }
    }

    // PLAYER MOVEMENT GOES HERE
    // Called every multiplayer simulation "tick"
    public override void FixedUpdateNetwork()
    {
        if (GameManager.Instance.currentGameMode == Enums.Gamemode.EditMode)
            return;
        
        // Server -> sends us our local player input to update!
        if (GetInput(out NetworkInputData data))
        {

            rb.AddForce(data.moveDirection * 50f * Runner.DeltaTime, ForceMode2D.Impulse);
            
        }
    }
}
