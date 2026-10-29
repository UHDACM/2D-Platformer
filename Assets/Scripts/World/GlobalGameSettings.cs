using Fusion;
using UnityEngine;

public class GlobalGameSettings : NetworkBehaviour
{
    [Networked] public bool score_to_win { get; set; }
    [Networked] public int sceneIndex { get; set; }
    
    public ChangeDetector changeDetector;
    
    // Singelton
    public static GlobalGameSettings Instance { get; private set; }
    
    public override void Spawned()
    {
        changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        Instance = this;
        
        
    }
}
