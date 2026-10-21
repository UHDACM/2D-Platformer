using System;
using Fusion;
using UnityEngine;

public class PlayerMovementMultiplayer : NetworkBehaviour
{
    private Rigidbody2D rb;
    public GameObject circle_prefab;        // just for testing

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;
        
        // Jittery because the local synced-position != camera position
        Vector3 v = transform.position;
        v.z = -10f;

        Camera.main.transform.position = v;
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
            }
        }
    }
    
    
}
