using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapSceneRPCController : NetworkBehaviour
{
    [Min(1)] public float votingSeconds = 20;
    public bool showVotingControls = true;
    [Networked] public TickTimer VoteTimer { get; private set; }
    [Networked] public NetworkBool VotingClosed { get; private set; }
    [Networked] public int EligiblePlayers { get; private set; }
    [Networked] public Enums.Maps SelectedMap { get; private set; }
    [Networked, Capacity(SessionRules.MaxPlayers)] public NetworkDictionary<PlayerRef, int> Votes => default;
    readonly MapBallot ballot = new MapBallot();

    public override void Spawned()
    {
        DontDestroyOnLoad(gameObject);
        if (!HasStateAuthority) return;
        VoteTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(1, votingSeconds));
        RefreshMembers();
    }
    void RefreshMembers()
    {
        ballot.SetMembers(Runner.ActivePlayers.Select(player => player.RawEncoded));
        EligiblePlayers = ballot.EligibleCount;
        foreach (var pair in Votes.ToArray())
            if (!ballot.Votes.ContainsKey(pair.Key.RawEncoded)) Votes.Remove(pair.Key);
    }
    public void VoteForMap(int map)
    {
        if (Object != null && Object.IsValid && !VotingClosed && SessionRules.ValidMap((Enums.Maps)map)) RPC_Vote(map);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_Vote(int map, RpcInfo info = default)
    {
        if (!HasStateAuthority || VotingClosed || VoteTimer.Expired(Runner) || info.Source == PlayerRef.None) return;
        RefreshMembers();
        if (ballot.Vote(info.Source.RawEncoded, (Enums.Maps)map)) Votes.Set(info.Source, map);
    }
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || VotingClosed) return;
        RefreshMembers();
        if (!ballot.AllVoted && !VoteTimer.Expired(Runner)) return;
        var settings = GlobalGameSettings.Instance;
        if (settings == null || !settings.HasStateAuthority) return;
        SelectedMap = ballot.Close();
        VotingClosed = true;
        VoteTimer = TickTimer.None;
        settings.sceneIndex = (int)SessionRules.SceneForMap(SelectedMap);
    }
    void Update()
    {
        if (Object == null || !Object.IsValid || VotingClosed || Keyboard.current == null ||
            (SceneRouter.Instance != null && SceneRouter.Instance.IsLoading)) return;
        var keys = Keyboard.current;
        if (keys.digit1Key.wasPressedThisFrame) VoteForMap((int)Enums.Maps.Grass);
        if (keys.digit2Key.wasPressedThisFrame) VoteForMap((int)Enums.Maps.Ice);
        if (keys.digit3Key.wasPressedThisFrame) VoteForMap((int)Enums.Maps.Lava);
        if (keys.digit4Key.wasPressedThisFrame) VoteForMap((int)Enums.Maps.Space);
        if (keys.digit5Key.wasPressedThisFrame) VoteForMap((int)Enums.Maps.Undersea);
    }
    void OnGUI()
    {
        if (!showVotingControls || Object == null || !Object.IsValid) return;
        GUILayout.BeginArea(new Rect(20, 20, 300, 290), GUI.skin.box);
        GUILayout.Label($"Choose a map - {Votes.Count}/{EligiblePlayers} votes");
        GUILayout.Label(VotingClosed ? $"Loading {SelectedMap}" : $"{Mathf.CeilToInt(VoteTimer.RemainingTime(Runner) ?? 0)} seconds remaining");
        bool wasEnabled = GUI.enabled;
        GUI.enabled = !VotingClosed;
        int index = 1;
        foreach (var map in SessionRules.MapOrder)
        {
            int count = Votes.Count(pair => pair.Value == (int)map);
            bool mine = Votes.TryGet(Runner.LocalPlayer, out int choice) && choice == (int)map;
            if (GUILayout.Button($"{index++}. {map} ({count}){(mine ? " - your vote" : "")}")) VoteForMap((int)map);
        }
        GUI.enabled = wasEnabled;
        GUILayout.Label("You can change your vote until voting closes.\nTies use map order; no votes selects Grass.");
        GUILayout.EndArea();
    }
}
