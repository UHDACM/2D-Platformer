using System;
using Fusion;
using UnityEngine;

public class PlayerMovementMultiplayer : NetworkBehaviour
{
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called every multiplayer simulation "tick"
    public override void FixedUpdateNetwork()
    {
        // Server -> sends us our local player input to update!
        if (GetInput(out NetworkInputData data))
        {
            rb.AddForce(data.Direction
                        * 50f * Runner.DeltaTime,  ForceMode2D.Impulse);
            
        }
        
    }
    
    
}
