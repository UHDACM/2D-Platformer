using System;
using Fusion;
using UnityEngine;

public class PlayerMovementMultiplayer : NetworkBehaviour
{
    private Rigidbody2D rb;
    private GroupFollowCamera2D followCamera;
    public GameObject circle_prefab;        // just for testing

    // Synced network variables
    [Networked]
    public bool changedFlag { get; set; }
    [Networked]
    public int changedInt { get; set; }
    [Networked]
    public Enums.Characters changedCharacter { get; set; }

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
        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);  // init
        
        // init properties
        changedFlag = false;
        changedInt = 0;
        changedCharacter = 0;
    }

    // After update() function, invoke any needed network changes
    public override void Render()
    {
        // Every visible player joins the local group; input authority does not gate framing.
        RegisterCameraTarget();
        foreach (string change in  changeDetector.DetectChanges(this))
        {

            switch (change)
            {
                case nameof(changedFlag):
                    Debug.Log($"changed flag: {changedFlag}");

                    break;
                case nameof(changedInt):
                    Debug.Log($"changed flag: {changedInt}");

                    break;
                case nameof(changedCharacter):
                    Debug.Log($"changed flag: {changedCharacter.ToString()}");

                    break;
                
            }
        }
    }


    // Called every multiplayer simulation "tick"
    public override void FixedUpdateNetwork()
    {
        // Server -> sends us our local player input to update!
        if (GetInput(out NetworkInputData data))
        {
            rb.AddForce(data.Direction * 50f * Runner.DeltaTime,  ForceMode2D.Impulse);

            // if someone clicked. Only host gets to proceed in logic
            if (HasStateAuthority && data.Buttons.IsSet(NetworkInputData.MouseButton0))
            {
                //
                Runner.Spawn(
                    circle_prefab,
                    transform.position,
                    Quaternion.LookRotation(data.Direction),
                    Object.InputAuthority,
                    // Logic to run after the prefab spawns, but before synchronized
                    (runner, newCircle) =>
                    {
                        newCircle.GetComponent<scriptForBall>().Init();
                        
                    }
                );

                changedFlag = !changedFlag;
                changedInt++;
                changedCharacter = (Enums.Characters) (changedInt % 3);
            }
        }
    }
    
    
}
