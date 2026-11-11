using Fusion;
using UnityEngine;

public struct RoundPlayerData : INetworkStruct
{
    public RoundPlayerStatus Status;
    public int Slot;
    public int Score;
    public int GrantId;
}

public struct RoundObstacleData : INetworkStruct
{
    public PlayerRef Owner;
    public int Round;
    public int Definition;
    public Vector2Int Anchor;
    public int Rotation;
    public NetworkBool Committed;
}
