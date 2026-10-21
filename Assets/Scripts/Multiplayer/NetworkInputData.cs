using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction;
    public NetworkButtons Buttons;
    
    // like a enum
    public const byte MouseButton0 = 1;
}