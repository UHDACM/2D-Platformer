using System.Collections.Generic;
using System.Linq;

public enum RoundPhase { Build, Play, Results, MatchComplete }
public enum RoundPlayerStatus { Waiting, Building, Alive, Eliminated, Finished }

public static class SessionRules
{
    public const int MaxPlayers = 16;
    public const int MaxRounds = 8;
    public static readonly Enums.Maps[] MapOrder =
        { Enums.Maps.Grass, Enums.Maps.Ice, Enums.Maps.Lava, Enums.Maps.Space, Enums.Maps.Undersea };
    public static bool ValidMap(Enums.Maps map) => MapOrder.Contains(map);
    public static Enums.Scenes SceneForMap(Enums.Maps map)
    {
        switch (map)
        {
            case Enums.Maps.Ice: return Enums.Scenes.Ice;
            case Enums.Maps.Lava: return Enums.Scenes.Lava;
            case Enums.Maps.Space: return Enums.Scenes.Space;
            case Enums.Maps.Undersea: return Enums.Scenes.Undersea;
            default: return Enums.Scenes.Grass;
        }
    }
    public static bool CanPlaceGrant(RoundPhase phase, int round, int requestRound, int owner, int sender, bool committed, int rotation)
        => phase == RoundPhase.Build && round == requestRound && owner == sender && !committed && rotation >= 0 && rotation < 4;
    public static bool RoundIsOver(RoundPhase phase, int alive, bool expired)
        => phase == RoundPhase.Play && (alive == 0 || expired);
    public static bool CanResolvePlayer(RoundPhase phase, RoundPlayerStatus status)
        => phase == RoundPhase.Play && status == RoundPlayerStatus.Alive;
}

// Host-owned ballot. RPC callers supply a choice; sender identity comes from Fusion.
public sealed class MapBallot
{
    readonly HashSet<int> members = new HashSet<int>();
    readonly Dictionary<int, Enums.Maps> votes = new Dictionary<int, Enums.Maps>();
    public IReadOnlyDictionary<int, Enums.Maps> Votes => votes;
    public int EligibleCount => members.Count;
    public bool AllVoted => members.Count > 0 && votes.Count == members.Count;
    public bool Closed { get; private set; }
    public void SetMembers(IEnumerable<int> players)
    {
        if (Closed) return;
        members.Clear();
        foreach (int player in players.Distinct().OrderBy(x => x).Take(SessionRules.MaxPlayers)) members.Add(player);
        foreach (int player in votes.Keys.Where(x => !members.Contains(x)).ToArray()) votes.Remove(player);
    }
    public bool Vote(int sender, Enums.Maps map)
    {
        if (Closed || !members.Contains(sender) || !SessionRules.ValidMap(map)) return false;
        votes[sender] = map;
        return true;
    }
    public Enums.Maps Close()
    {
        Closed = true;
        return SessionRules.MapOrder.OrderByDescending(map => votes.Values.Count(vote => vote == map)).First();
    }
}
