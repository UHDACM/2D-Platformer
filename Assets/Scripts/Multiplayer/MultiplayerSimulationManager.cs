using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Object = System.Object;

public class MultiplayerSimulationManager : MonoBehaviourPlus, INetworkRunnerCallbacks
{
    public override string prettyName => "MultiplayerSimulationManager";
    public override string description => "Runs the Network Simulation of the game";
    
    
    [Header("Persistent Networking")]
    [SerializeField] private NetworkPrefabRef _LocalPlayerProfile_Prefab;
    [SerializeField] private NetworkPrefabRef _GlobalGameSettings_Prefab;

    [Header("Remote Procedure Calls (RPC)")]
    [SerializeField] private NetworkPrefabRef _MenuSceneRPCController_Prefab;
    [SerializeField] private NetworkPrefabRef _MapSceneRPCController_Prefab;
    [SerializeField] private NetworkPrefabRef _GameSceneRPCController_Prefab;

    
    // Hashmap of all players
    [ReadOnly]
    [SerializeField] Dictionary<PlayerRef, NetworkObject> 
        _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    
    private NetworkRunner _hostRunner;
    private NetworkObject _globalGameSettings;

    
    public static MultiplayerSimulationManager Instance { get; private set; }
    
    // Create singelton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    async void StartGame(GameMode mode)
    {
        
        // Create the Fusion runner and let it know that we will be providing user input
        _hostRunner = gameObject.AddComponent<NetworkRunner>();
        _hostRunner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        //var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        //var sceneInfo = new NetworkSceneInfo();
        //if (scene.IsValid)
        //{
        //    sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        //}

        // Start or join (depends on gamemode) a session with a specific name
        await _hostRunner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        
    }

    // temporary testing functions
    public void HostGame() => StartGame(GameMode.Host);
    public void JoinGame() => StartGame(GameMode.Client);
    
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
             
            // Host owns the game settings for everyone
            if (_globalGameSettings == null)
            {
                _globalGameSettings = _hostRunner.Spawn(_GlobalGameSettings_Prefab, Vector3.zero, Quaternion.identity);
                DontDestroyOnLoad(_globalGameSettings.gameObject);                
                
                // RPC
                 _hostRunner.Spawn(_MenuSceneRPCController_Prefab, Vector3.zero, Quaternion.identity);
            }

            
            NetworkObject networkPlayerObject = runner.Spawn(_LocalPlayerProfile_Prefab, Vector3.zero, Quaternion.identity, player);
            DontDestroyOnLoad(networkPlayerObject.gameObject); // only destroy on despawn
            
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
            
            // Idempotent, but oh well
            SceneRouter.Instance.RPC_OnStartMenuScene = () => _hostRunner.Spawn(_MenuSceneRPCController_Prefab, Vector3.zero, Quaternion.identity);
            SceneRouter.Instance.RPC_OnStartMapScene = () => _hostRunner.Spawn(_MapSceneRPCController_Prefab, Vector3.zero, Quaternion.identity);
            SceneRouter.Instance.RPC_OnStartGameScene = () => _hostRunner.Spawn(_GameSceneRPCController_Prefab, Vector3.zero, Quaternion.identity, player);

        }
        else
            Debug.Log("[Photon Multiplayer] Your Client has joined!");

    }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Remove player from lists & destroy object
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
        
        Debug.Log("[Photon Multiplayer] Player Left!");
    }

    // sample mouse click
    private bool _mouseButton0;
    private void Update()
    {
        // capture via new input system
        _mouseButton0 = _mouseButton0 | Mouse.current.leftButton.wasPressedThisFrame;
    }


    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = new  NetworkInputData();
        
        // Read Keyboard
        // Change later to other script
        if (Keyboard.current.aKey.isPressed)
            data.Direction += Vector2.left;
        if (Keyboard.current.dKey.isPressed)
            data.Direction += Vector2.right;
        if (Keyboard.current.wKey.isPressed)
            data.Direction += Vector2.up;
        if (Keyboard.current.dKey.isPressed)
            data.Direction += Vector2.down;

        // read sampled mouse click
        data.Buttons.Set(NetworkInputData.MouseButton0, _mouseButton0);
        _mouseButton0 = false;
        
        // give input to the host
        input.Set(data);
    }
    
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    
}