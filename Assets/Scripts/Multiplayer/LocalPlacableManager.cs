using System;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

public class LocalPlacableManager : NetworkBehaviour
{
    // used by client,
    // Set by the host
    [Networked] public int placableIndexGiven { get; set; }
    [Networked] public bool hasPlacedPlacable { get; set; }
    

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!HasInputAuthority) return;
        
        
        // mouse checks are done locally
        if (GameManager.Instance.currentGameMode != Enums.Gamemode.EditMode)
            return;

        if (!hasPlacedPlacable && Mouse.current.rightButton.wasPressedThisFrame)
        {
            // Get random placable
            

            // Convert the mouse screen position into World Units (X and Y)
            // Note: For 2D games, set the Z distance relative to the camera plain (e.g., 10f)
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = Camera.current.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
            Debug.Log(mouseWorldPos);
            
            if (Object.HasInputAuthority)   // double checking
                GameManager.Instance.RPC_requestToHostPlacePlacable(mouseWorldPos);
            
            
        }
    }
}
