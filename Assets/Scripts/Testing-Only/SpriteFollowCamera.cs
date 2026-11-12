using System;
using Fusion;
using UnityEngine;

public class SpriteFollowCamera : MonoBehaviourPlus
{
    
    public override string prettyName => "SpriteFollowCamera";
    public override string description => "you can ignore this, just to test some stuff..";
    

    private GroupFollowCamera2D followCamera;

    private void Update()
    {
        if (followCamera == null) followCamera = GroupFollowCamera2D.ForMainCamera();
        if (followCamera != null) followCamera.Register(transform);
    }

    private void OnDisable()
    {
        if (followCamera != null) followCamera.Unregister(transform);
    }
    
}
