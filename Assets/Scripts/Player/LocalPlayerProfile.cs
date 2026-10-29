using UnityEngine;
using Fusion;

public class LocalPlayerProfile : NetworkBehaviour
{
    [Networked] public string name { get; set; }

}
