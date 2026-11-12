using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SessionChecks
{
    static int checks;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    [MenuItem("Tools/Multiplayer/Run Session Rule Checks")]
    public static void Run()
    {
        checks = 0;
        var ballot = new MapBallot();
        Check(!ballot.AllVoted, "An empty session must not finish voting early");
        Check(!ballot.Vote(1, Enums.Maps.Grass), "An unregistered sender voted");
        ballot.SetMembers(new[] { 1, 2, 3 });
        Check(ballot.EligibleCount == 3, "Membership count differs");
        Check(!ballot.Vote(1, Enums.Maps.Default), "Non-map choice accepted");
        Check(!ballot.Vote(1, (Enums.Maps)999), "Out-of-range choice accepted");
        Check(ballot.Vote(1, Enums.Maps.Grass), "Valid vote rejected");
        Check(ballot.Vote(1, Enums.Maps.Ice), "Vote replacement rejected");
        Check(ballot.Votes.Count == 1 && ballot.Votes[1] == Enums.Maps.Ice, "Replacement added a second vote");
        ballot.Vote(2, Enums.Maps.Ice);
        Check(!ballot.AllVoted, "Vote closed before the last player voted");
        ballot.SetMembers(new[] { 1, 3, 4 });
        Check(!ballot.Votes.ContainsKey(2) && ballot.EligibleCount == 3, "Disconnected player's vote remained");
        Check(!ballot.Vote(2, Enums.Maps.Lava), "Departed player voted again");
        ballot.Vote(3, Enums.Maps.Lava);
        ballot.Vote(4, Enums.Maps.Lava);
        Check(ballot.AllVoted && ballot.Close() == Enums.Maps.Lava, "Plurality result differs");
        Check(!ballot.Vote(1, Enums.Maps.Space), "Closed ballot accepted a late RPC");
        ballot.SetMembers(new[] { 99 });
        Check(ballot.EligibleCount == 3 && ballot.Close() == Enums.Maps.Lava, "Closed result changed on membership update");

        ballot = new MapBallot();
        ballot.SetMembers(new[] { 1, 2, 3 });
        ballot.Vote(1, Enums.Maps.Space);
        ballot.Vote(2, Enums.Maps.Ice);
        Check(ballot.Close() == Enums.Maps.Ice, "Timed-out partial tie did not use stable map order");
        Check(new MapBallot().Close() == Enums.Maps.Grass, "No-vote fallback differs");
        ballot = new MapBallot();
        ballot.SetMembers(Enumerable.Range(1, 100).Concat(new[] { 1, 2 }));
        Check(ballot.EligibleCount == SessionRules.MaxPlayers, "Capacity or duplicate membership handling differs");
        Check(!ballot.Vote(100, Enums.Maps.Grass), "Beyond-capacity member accepted");
        ballot = new MapBallot();
        ballot.SetMembers(new[] { 1 });
        ballot.Vote(1, Enums.Maps.Undersea);
        ballot.SetMembers(new[] { 1, 2 });
        Check(!ballot.AllVoted, "Late joiner was not included before close");
        ballot.SetMembers(new[] { 1 });
        Check(ballot.AllVoted, "Disconnect of an abstaining player blocked completion");
        foreach (var map in SessionRules.MapOrder)
            Check(SessionRules.SceneForMap(map).ToString() == map.ToString(), "Map enum was incorrectly cast to scene index");

        foreach (RoundPhase phase in Enum.GetValues(typeof(RoundPhase)))
        {
            Check(SessionRules.RoundIsOver(phase, 0, false) == (phase == RoundPhase.Play), "Zero-alive transition in wrong phase");
            Check(SessionRules.RoundIsOver(phase, 2, true) == (phase == RoundPhase.Play), "Timer transition in wrong phase");
            Check(!SessionRules.RoundIsOver(phase, 2, false), "Live round ended early");
            foreach (RoundPlayerStatus status in Enum.GetValues(typeof(RoundPlayerStatus)))
                Check(SessionRules.CanResolvePlayer(phase, status) == (phase == RoundPhase.Play && status == RoundPlayerStatus.Alive),
                    "Resolution could award duplicate points or resolve a spectator");
        }
        for (int rotation = 0; rotation < 4; rotation++)
            Check(SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 3, 11, 11, false, rotation), "Valid owned rotation rejected");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Play, 3, 3, 11, 11, false, 0), "Play-phase placement accepted");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 2, 11, 11, false, 0), "Stale-round placement accepted");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 3, 11, 12, false, 0), "Another player's piece accepted");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 3, 11, 11, true, 0), "Duplicate placement accepted");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 3, 11, 11, false, -1), "Negative RPC rotation accepted");
        Check(!SessionRules.CanPlaceGrant(RoundPhase.Build, 3, 3, 11, 11, false, 4), "Unnormalized RPC rotation accepted");
        Debug.Log($"Session rule checks passed: {checks}");
    }
}
