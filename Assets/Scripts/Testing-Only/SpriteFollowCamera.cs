using System;
using UnityEngine;

public class SpriteFollowCamera : MonoBehaviour
{
    public GameObject sprite;

    private void Update()
    {
        Vector3 pos = sprite.transform.position;
        pos.z = -10f; // stupid anoying camera z thingy 
        
        transform.position = pos;
    }
}
