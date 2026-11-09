using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;
    public byte climbDirection;
    
    public const byte climbingUp = 1;
    public const byte climbingDown = 2;
    
    // For button  clicks
    //public NetworkButtons Buttons;

}