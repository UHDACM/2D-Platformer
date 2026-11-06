using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;   // x and y
    public Vector2 climbDirection;  // y only
    
    // For button  clicks
    //public NetworkButtons Buttons;
    
    // like a enum
    //public const byte MouseButton0 = 1;
}