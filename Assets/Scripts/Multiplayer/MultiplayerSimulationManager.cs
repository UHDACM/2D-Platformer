using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private NetworkPrefabRef _GameManager_Prefab;

    
    // Stores all players
    [SerializeField] private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new();
    
    // readOnly version of this dictionary (for outside scripts to use)
    public IReadOnlyDictionary<PlayerRef, NetworkObject> spawnedCharacters => _spawnedCharacters;
    
    private NetworkRunner _hostRunner;
    private NetworkObject _globalGameSettings;
    private NetworkObject _GameManager; // only one instance allowed in host game
    private NetworkObject sceneController;
    private SceneRouter subscribedRouter;


    // Unity events
    public event Action onConnectionFailed;
    public event Action onCreatedRoom;

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
    
    async void StartGame(GameMode mode, string code)
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
            SessionName = code,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        
    }

    // temporary testing functions
    public void HostGame(string code) => StartGame(GameMode.Host, code);
    public void JoinGame(string code) => StartGame(GameMode.Client, code);
    
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
                 SpawnMenuController();
                 
                 onCreatedRoom?.Invoke();
            }

            
            NetworkObject networkPlayerObject = runner.Spawn(_LocalPlayerProfile_Prefab, Vector3.zero, Quaternion.identity, player);
            DontDestroyOnLoad(networkPlayerObject.gameObject); // only destroy on despawn
            
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
            
            SubscribeSceneCallbacks();

        }
        else
            Debug.Log("[Photon Multiplayer] Your Client has joined!");
    }
    
    void SubscribeSceneCallbacks()
    {
        if (subscribedRouter != null) return;
        subscribedRouter = SceneRouter.Instance;
        subscribedRouter.RPC_OnStartMenuScene += SpawnMenuController;
        subscribedRouter.RPC_OnStartMapScene += SpawnMapController;
        subscribedRouter.RPC_OnStartGameScene += SpawnGameController;
        subscribedRouter.OnStartGameScene += SpawnGameManager;
        subscribedRouter.OnExitGameScene += DespawnGameManager;
        subscribedRouter.OnExitMenuScene += DespawnSceneController;
        subscribedRouter.OnExitMapScene += DespawnSceneController;
        subscribedRouter.OnExitGameScene += DespawnSceneController;
    }
    void UnsubscribeSceneCallbacks()
    {
        if (subscribedRouter == null) return;
        subscribedRouter.RPC_OnStartMenuScene -= SpawnMenuController;
        subscribedRouter.RPC_OnStartMapScene -= SpawnMapController;
        subscribedRouter.RPC_OnStartGameScene -= SpawnGameController;
        subscribedRouter.OnStartGameScene -= SpawnGameManager;
        subscribedRouter.OnExitGameScene -= DespawnGameManager;
        subscribedRouter.OnExitMenuScene -= DespawnSceneController;
        subscribedRouter.OnExitMapScene -= DespawnSceneController;
        subscribedRouter.OnExitGameScene -= DespawnSceneController;
        subscribedRouter = null;
    }
    void SpawnMenuController() => SpawnController(_MenuSceneRPCController_Prefab);
    void SpawnMapController() => SpawnController(_MapSceneRPCController_Prefab);
    void SpawnGameController() => SpawnController(_GameSceneRPCController_Prefab);
    void SpawnController(NetworkPrefabRef prefab)
    {
        if (_hostRunner == null || !_hostRunner.IsRunning || !_hostRunner.IsServer) return;
        DespawnSceneController();
        sceneController = _hostRunner.Spawn(prefab, Vector3.zero, Quaternion.identity);
    }
    void DespawnSceneController()
    {
        if (sceneController != null && sceneController.IsValid) _hostRunner.Despawn(sceneController);
        sceneController = null;
    }
    void SpawnGameManager()
    {
        if (_hostRunner != null && _hostRunner.IsRunning && _hostRunner.IsServer && _GameManager == null)
            _GameManager = _hostRunner.Spawn(_GameManager_Prefab, Vector3.zero, Quaternion.identity);
    }
    void DespawnGameManager()
    {
        if (_GameManager != null && _GameManager.IsValid)
        {
            _GameManager.GetComponent<GameManager>().ShutdownRound();
            _hostRunner.Despawn(_GameManager);
        }
        _GameManager = null;
    }
    void OnDestroy()
    {
        UnsubscribeSceneCallbacks();
        if (Instance == this) Instance = null;
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (GameManager.Instance != null) GameManager.Instance.RemovePlayer(player);
        // Remove player from lists & destroy object
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
        
        Debug.Log("[Photon Multiplayer] Player Left!");
    }

    private readonly PlatformerInputSampler playerInput = new PlatformerInputSampler();

    private void Update() => playerInput.Capture(Keyboard.current);
    private void OnApplicationFocus(bool focused) { if (!focused) playerInput.Clear(); }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        playerInput.Read(Keyboard.current, out Vector2 moveDirection, out float climbDirection);
        input.Set(new NetworkInputData { moveDirection = moveDirection, climbDirection = climbDirection });
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        UnsubscribeSceneCallbacks();
        _spawnedCharacters.Clear();
        _GameManager = null;
        sceneController = null;
        _globalGameSettings = null;
        playerInput.Clear();
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress,
        NetConnectFailedReason reason)
    {
        onConnectionFailed?.Invoke();
    }
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
