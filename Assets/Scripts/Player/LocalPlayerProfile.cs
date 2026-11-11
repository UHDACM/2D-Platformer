using UnityEngine;
using Fusion;

public class LocalPlayerProfile : NetworkBehaviour
{
    public override void Spawned() => DontDestroyOnLoad(gameObject);
    [Networked] public string name { get; set; }
        
}
