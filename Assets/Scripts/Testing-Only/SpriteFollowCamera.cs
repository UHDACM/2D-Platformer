using System;
using UnityEngine;

public class SpriteFollowCamera : MonoBehaviourPlus
{
    
    public override string prettyName => "SpriteFollowCamera";
    public override string description => "you can ignore this, just to test some stuff..";
    
    public GameObject sprite;

    public Enums.Maps map;

    private void Update()
    {
        Vector3 pos = sprite.transform.position;
        pos.z = -10f; // stupid anoying camera z thingy 
        
        transform.position = pos;
    }
    
}
