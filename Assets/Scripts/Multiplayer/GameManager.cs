using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = System.Random;

public class GameManager : NetworkBehaviour
{
    [Header("Controls the game logic")]
    [SerializeField] private NetworkPrefabRef _Player_Prefab;
    [SerializeField] private NetworkPrefabRef _LocalPacableManager_Prefab;
    [SerializeField] private NetworkPrefabRef[] _PlacableList;
    
    public static GameManager Instance {  get; private set; }
    
    // Each player's placable manager
    private Dictionary<PlayerRef, LocalPlacableManager> _playerPlacables;

        
    [Networked] public Enums.Gamemode currentGameMode { get; set; }
    [Networked] private TickTimer PlayMode_Timer { get; set; }
    [Networked] private TickTimer EditMode_Timer { get; set; }

    public float? PlayMode_TimerSeconds = 0f;
    public float? EditMode_TimerSeconds = 0f;
    public int roundCount { get; private set; }
    
    
    public override void Spawned()
    { 
        base.Spawned();
        Instance = this;

        Debug.Log($"has state auth: {HasStateAuthority}");
        if (_playerPlacables == null && HasStateAuthority)
            initAllPlayers(); 
        
        onGameStarted();
        roundCount = 1;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        Instance = null;
    } 
    
    private void onGameStarted()
    {
        const float EDITMODE_SECONDS = 10f;
        
        EditMode_Timer = TickTimer.CreateFromSeconds(Runner, EDITMODE_SECONDS);
        PlayMode_Timer = TickTimer.None;
        currentGameMode = Enums.Gamemode.EditMode;
    }
        
    private void initAllPlayers()
    {
        foreach (var playerRef in
                 MultiplayerSimulationManager.Instance.spawnedCharacters.Keys)
        {
            // spawn each player in
            var networkObject =
                Runner.Spawn(_LocalPacableManager_Prefab, Vector3.zero, Quaternion.identity, playerRef);
            Runner.Spawn(_Player_Prefab, Vector3.zero, Quaternion.identity, playerRef);

            
            var placableManager =  networkObject.GetComponent<LocalPlacableManager>();
            
            // add to dictionary
            _playerPlacables = new();
            _playerPlacables.Add(playerRef, placableManager);
            
        }
    }
    
    
    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
            GameLoop();
    }

    // Main logic for the game
    private void GameLoop()
    {
        UpdateTimers();
    }
    

    public void UpdateTimers()
    {
        if (!HasStateAuthority) return;

        // update float
        PlayMode_TimerSeconds = PlayMode_Timer.RemainingTime(Runner);
        EditMode_TimerSeconds = EditMode_Timer.RemainingTime(Runner);
        
        
        const float PLAYMODE_SECONDS = 10f;
        const float EDITMODE_SECONDS = 10f;

        // Should we switch game modes?
        
        if (currentGameMode == Enums.Gamemode.EditMode)
        {
            // Edit mode time has ended
            if (EditMode_Timer.Expired(Runner))
            {
                
                PlayMode_Timer = TickTimer.CreateFromSeconds(Runner, PLAYMODE_SECONDS);
                currentGameMode = Enums.Gamemode.PlayMode;
            }
        }
        
        if (currentGameMode == Enums.Gamemode.PlayMode)
        {
            // Edit mode time has ended
            if (PlayMode_Timer.Expired(Runner))
            {
                
                EditMode_Timer = TickTimer.CreateFromSeconds(Runner, EDITMODE_SECONDS);
                currentGameMode = Enums.Gamemode.EditMode;
                roundCount++;
            }
        }
    }
    
    // This function can fail. If mouse click is invalid. Or if packets never arrive.
    // Client -> Host
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_requestToHostPlacePlacable(Vector2 requestedPos, RpcInfo info = default)
    {
        Debug.Log($"Recieved mouse click Place request from player: {info.Source.PlayerId}");

        PlayerRef callingPlayer = new();
        LocalPlacableManager callingPlacableManager = new();
        
        // check who's calling
        foreach (var player in _playerPlacables)
        {
            var playerRef = player.Key;
            var playerPlacableManager = player.Value;
            
            if (playerRef.PlayerId == info.Source.PlayerId)
            {
                callingPlayer = playerRef;
                callingPlacableManager = playerPlacableManager;
            }
        }

        if (!callingPlayer.IsValid)
        {
            Debug.LogError("Recieved a call from invalid Player! Ignoring RPC");
            return;
        }

        Random r = new();
        NetworkPrefabRef randomPrefab = _PlacableList[r.Next(_PlacableList.Length)];
        Runner.Spawn(randomPrefab, requestedPos, Quaternion.identity, callingPlayer);

        callingPlacableManager.hasPlacedPlacable = true;

    }
    
}
