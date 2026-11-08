using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;   // x: walk, y: jump held/requested (0 or 1)
    public float climbDirection;   // -1: down, 0: stop, +1: up
}
