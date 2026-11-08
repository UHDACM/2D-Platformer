using System;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : NetworkBehaviour
{
        
    [SerializeField] private NetworkPrefabRef _Player_Prefab;
    public static GameManager Instance {  get; private set; }
        
    [Networked] public Enums.Gamemode currentGameMode { get; set; }
    [Networked] private TickTimer PlayMode_Timer { get; set; }
    [Networked] private TickTimer EditMode_Timer { get; set; }

    public float? PlayMode_TimerSeconds = 0f;
    public float? EditMode_TimerSeconds = 0f;
    
    public override void Spawned()
    { 
        base.Spawned();
        Instance = this;
        // Only the host creates characters and initializes the networked phase/timers.
        if (HasStateAuthority) onGameStarted();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        Instance = null;
    }
    
    private void onGameStarted()
    {
        spawnAllPlayers();
        
        const float EDITMODE_SECONDS = 10f;
        
        EditMode_Timer = TickTimer.CreateFromSeconds(Runner, EDITMODE_SECONDS);
        PlayMode_Timer = TickTimer.None;
        currentGameMode = Enums.Gamemode.EditMode;
    }
    
    private void spawnAllPlayers()
    {
        foreach (var player in
                 MultiplayerSimulationManager.Instance.spawnedCharacters.Values)
        {
            NetworkObject networkObject = player;
            
            // spawn each player in
            Runner.Spawn(_Player_Prefab, Vector3.zero, Quaternion.identity, networkObject.InputAuthority);
    
        }
        
    }
    
    
    private void Update()
    {
        // Debug.Log($"Gamemode: {currentGameMode.ToString()}, seconds: {PlayMode_TimerSeconds}");

        //  host only
        //if (HasStateAuthority &&
        //    Keyboard.current.spaceKey.wasPressedThisFrame)
        //    
        
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
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
            }
        }
    }
    
}
