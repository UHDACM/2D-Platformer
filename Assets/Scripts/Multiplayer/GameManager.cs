using System.Collections.Generic;
using System.Linq;
using Fusion;
using UltimateGrid2D;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] NetworkPrefabRef _Player_Prefab;
    [Min(1)] public float buildSeconds = 20;
    [Min(1)] public float playSeconds = 60;
    [Min(1)] public float resultsSeconds = 4;
    [Range(1, SessionRules.MaxRounds)] public int roundsPerMatch = 5;
    public float deathHeight = -20;
    [Min(1)] public float spawnSpacing = 1.5f;
    public PlaceableDefinition2D[] placeableDefinitions = new PlaceableDefinition2D[0];
    public bool showRoundStatus = true;
    public static GameManager Instance { get; private set; }

    [Networked] public Enums.Gamemode currentGameMode { get; private set; }
    [Networked] public RoundPhase Phase { get; private set; }
    [Networked] public int RoundNumber { get; private set; }
    [Networked] public int AliveCount { get; private set; }
    [Networked] public TickTimer PhaseTimer { get; private set; }
    [Networked] int NextObstacleId { get; set; }
    [Networked, Capacity(SessionRules.MaxPlayers)] public NetworkDictionary<PlayerRef, RoundPlayerData> Players => default;
    [Networked, Capacity(SessionRules.MaxPlayers * SessionRules.MaxRounds)] public NetworkDictionary<int, RoundObstacleData> Obstacles => default;
    public float? PlayMode_TimerSeconds => Phase == RoundPhase.Play ? PhaseTimer.RemainingTime(Runner) : 0;
    public float? EditMode_TimerSeconds => Phase == RoundPhase.Build ? PhaseTimer.RemainingTime(Runner) : 0;

    readonly Dictionary<PlayerRef, NetworkObject> avatars = new Dictionary<PlayerRef, NetworkObject>();
    readonly Dictionary<PlayerRef, PlacementRequest> pendingPlacements = new Dictionary<PlayerRef, PlacementRequest>();
    RoundPlacementWorld world;
    bool shuttingDown;
    struct PlacementRequest { public int Id, Round, Rotation; public Vector2Int Anchor; }

    public override void Spawned()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        world = gameObject.AddComponent<RoundPlacementWorld>();
        world.Manager = this;
        if (HasStateAuthority) currentGameMode = Enums.Gamemode.EditMode;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || shuttingDown) return;
        world.Synchronize();
        SyncMembers();
        if (RoundNumber == 0) { BeginBuild(); return; }
        if (Phase == RoundPhase.Build)
        {
            if (PhaseTimer.Expired(Runner)) { BeginPlay(); return; }
            foreach (var pair in Players.ToArray())
                if (pair.Value.GrantId < 0) GrantPlaceable(pair.Key);
            foreach (var request in pendingPlacements.ToArray()) CommitPlacement(request.Key, request.Value);
            pendingPlacements.Clear();
        }
        else if (Phase == RoundPhase.Play)
        {
            foreach (var pair in avatars.ToArray())
                if (pair.Value == null || !pair.Value.IsValid || pair.Value.transform.position.y < deathHeight)
                    ResolvePlayer(pair.Key, false);
            AliveCount = Players.Count(pair => pair.Value.Status == RoundPlayerStatus.Alive);
            if (SessionRules.RoundIsOver(Phase, AliveCount, PhaseTimer.Expired(Runner))) FinishRound();
        }
        else if (PhaseTimer.Expired(Runner))
        {
            if (Phase == RoundPhase.Results) BeginBuild();
            else if (GlobalGameSettings.Instance != null && GlobalGameSettings.Instance.HasStateAuthority)
            {
                PhaseTimer = TickTimer.None;
                GlobalGameSettings.Instance.sceneIndex = (int)Enums.Scenes.Map;
            }
        }
    }

    void SyncMembers()
    {
        var active = new HashSet<PlayerRef>(Runner.ActivePlayers);
        foreach (var pair in Players.ToArray())
            if (!active.Contains(pair.Key)) RemovePlayer(pair.Key);
        foreach (var player in active.OrderBy(player => player.RawEncoded))
        {
            if (Players.ContainsKey(player) || Players.Count >= SessionRules.MaxPlayers) continue;
            int slot = Enumerable.Range(0, SessionRules.MaxPlayers).First(candidate => Players.All(pair => pair.Value.Slot != candidate));
            Players.Add(player, new RoundPlayerData { Slot = slot, GrantId = -1, Status = RoundPlayerStatus.Waiting });
            if (RoundNumber > 0 && Phase == RoundPhase.Build) SpawnPlayer(player);
        }
    }

    void BeginBuild()
    {
        RoundNumber++;
        Phase = RoundPhase.Build;
        currentGameMode = Enums.Gamemode.EditMode;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(1, buildSeconds));
        AliveCount = 0;
        pendingPlacements.Clear();
        foreach (var pair in Players.ToArray())
        {
            var data = pair.Value;
            data.GrantId = -1;
            Players.Set(pair.Key, data);
            SpawnPlayer(pair.Key);
            GrantPlaceable(pair.Key);
        }
    }

    void SpawnPlayer(PlayerRef player)
    {
        DespawnAvatar(player);
        var data = Players.Get(player);
        var points = FindObjectsOfType<RoundSpawnPoint2D>().OrderBy(point => point.slot).ToArray();
        Vector3 position = points.Length > 0 ? points[data.Slot % points.Length].transform.position :
            world.DefaultSpawnPosition + Vector3.right * (data.Slot * spawnSpacing);
        var avatar = Runner.Spawn(_Player_Prefab, position, Quaternion.identity, player);
        avatars[player] = avatar;
        Runner.SetPlayerObject(player, avatar);
        data.Status = RoundPlayerStatus.Building;
        Players.Set(player, data);
    }

    void GrantPlaceable(PlayerRef player)
    {
        if (!world.Ready || placeableDefinitions.Length == 0 || Obstacles.Count >= SessionRules.MaxPlayers * SessionRules.MaxRounds) return;
        var data = Players.Get(player);
        if (data.GrantId >= 0 || data.Status != RoundPlayerStatus.Building) return;
        int definition = (RoundNumber - 1 + data.Slot) % placeableDefinitions.Length;
        int id = NextObstacleId;
        if (!world.TryCreateGrant(id, definition, player.RawEncoded, out Vector2Int anchor)) return;
        NextObstacleId++;
        Obstacles.Add(id, new RoundObstacleData { Owner = player, Round = RoundNumber, Definition = definition, Anchor = anchor });
        data.GrantId = id;
        Players.Set(player, data);
    }

    void BeginPlay()
    {
        pendingPlacements.Clear();
        foreach (var obstacle in Obstacles.ToArray())
            if (!obstacle.Value.Committed) Obstacles.Remove(obstacle.Key);
        world.Synchronize();
        Phase = RoundPhase.Play;
        currentGameMode = Enums.Gamemode.PlayMode;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(1, playSeconds));
        foreach (var pair in Players.ToArray())
        {
            var data = pair.Value;
            if (data.Status == RoundPlayerStatus.Building) data.Status = RoundPlayerStatus.Alive;
            Players.Set(pair.Key, data);
        }
        AliveCount = Players.Count(pair => pair.Value.Status == RoundPlayerStatus.Alive);
    }

    public void ResolvePlayer(PlayerMovementMultiplayer player, bool finished)
    {
        if (player == null || player.Object == null || !player.Object.IsValid) return;
        PlayerRef owner = player.Object.InputAuthority;
        if (!avatars.TryGetValue(owner, out var avatar) || avatar != player.Object) return;
        ResolvePlayer(owner, finished);
    }
    void ResolvePlayer(PlayerRef owner, bool finished)
    {
        if (!HasStateAuthority || !Players.TryGet(owner, out var data) || !SessionRules.CanResolvePlayer(Phase, data.Status)) return;
        data.Status = finished ? RoundPlayerStatus.Finished : RoundPlayerStatus.Eliminated;
        if (finished) data.Score++;
        Players.Set(owner, data);
        DespawnAvatar(owner);
        AliveCount = Players.Count(pair => pair.Value.Status == RoundPlayerStatus.Alive);
    }
    void FinishRound()
    {
        foreach (var pair in Players.ToArray())
            if (pair.Value.Status == RoundPlayerStatus.Alive) ResolvePlayer(pair.Key, false);
        Phase = RoundNumber >= Mathf.Clamp(roundsPerMatch, 1, SessionRules.MaxRounds) ? RoundPhase.MatchComplete : RoundPhase.Results;
        currentGameMode = Enums.Gamemode.EditMode;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(1, resultsSeconds));
    }
    public void RemovePlayer(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        DespawnAvatar(player);
        Players.Remove(player);
        pendingPlacements.Remove(player);
        foreach (var pair in Obstacles.ToArray())
            if (pair.Value.Owner == player && !pair.Value.Committed) Obstacles.Remove(pair.Key);
    }
    void DespawnAvatar(PlayerRef player)
    {
        if (avatars.TryGetValue(player, out var avatar) && avatar != null && avatar.IsValid) Runner.Despawn(avatar);
        avatars.Remove(player);
        if (Runner.IsRunning && Runner.ActivePlayers.Contains(player)) Runner.SetPlayerObject(player, null);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_Place(int id, int round, Vector2Int anchor, int rotation, RpcInfo info = default)
    {
        if (!HasStateAuthority || info.Source == PlayerRef.None || !Runner.ActivePlayers.Contains(info.Source) ||
            Phase != RoundPhase.Build || PhaseTimer.Expired(Runner) || !Players.TryGet(info.Source, out var player) || player.GrantId != id) return;
        pendingPlacements[info.Source] = new PlacementRequest { Id = id, Round = round, Anchor = anchor, Rotation = rotation };
    }
    void CommitPlacement(PlayerRef sender, PlacementRequest request)
    {
        if (!Players.TryGet(sender, out var player) || player.GrantId != request.Id || !Obstacles.TryGet(request.Id, out var item)) return;
        if (!SessionRules.CanPlaceGrant(Phase, RoundNumber, request.Round, item.Owner.RawEncoded, sender.RawEncoded, item.Committed, request.Rotation)) return;
        if (!world.TryCommit(request.Id, request.Anchor, request.Rotation)) return;
        item.Anchor = request.Anchor;
        item.Rotation = request.Rotation;
        item.Committed = true;
        Obstacles.Set(request.Id, item);
    }

    public void ShutdownRound()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        if (HasStateAuthority)
            foreach (var player in avatars.Keys.ToArray()) DespawnAvatar(player);
        if (world != null) world.Clear();
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Scene callbacks normally clean up before despawning the manager.
        if (world != null) world.Clear();
        if (Instance == this) Instance = null;
    }
    void OnGUI()
    {
        if (!showRoundStatus || Object == null || !Object.IsValid) return;
        GUILayout.BeginArea(new Rect(20, 20, 330, 260), GUI.skin.box);
        GUILayout.Label($"Round {RoundNumber}/{roundsPerMatch} - {Phase} - {Mathf.CeilToInt(PhaseTimer.RemainingTime(Runner) ?? 0)}s");
        GUILayout.Label($"Players alive: {AliveCount}");
        if (Phase == RoundPhase.Play) GUILayout.Label("Reach the green finish line to score a point.");
        foreach (var pair in Players.OrderBy(pair => pair.Value.Slot))
            GUILayout.Label($"Player {pair.Value.Slot + 1}: {pair.Value.Status} | {pair.Value.Score} points{(pair.Key == Runner.LocalPlayer ? " (you)" : "")}");
        if (Phase == RoundPhase.Build && Players.TryGet(Runner.LocalPlayer, out var mine))
            GUILayout.Label(mine.GrantId < 0 ? "Waiting for a free grid cell for your piece." :
                Obstacles.TryGet(mine.GrantId, out var item) && item.Committed ? "Piece placed. Waiting for play." : "Drag your colored piece. R rotates. One placement per round.");
        GUILayout.EndArea();
    }
}
