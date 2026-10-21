using System;
using Fusion;
using UnityEngine;

public class SpriteFollowCamera : MonoBehaviourPlus
{
    
    public override string prettyName => "SpriteFollowCamera";
    public override string description => "you can ignore this, just to test some stuff..";
    

    private void Update()
    {
        Vector3 pos = transform.position;
        pos.z = -10f; // stupid anoying camera z thingy 

        Camera.main.transform.position = pos;

    }
    
}
