using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : MonoBehaviourPlus
{
    public override string prettyName => "Scene Manager";
    public override string description => "Controlls the 3 Scenes in the game. Lets us go from Menu->Map->Game and back";

    // Unity Events
    public event Action OnStartMenuScene;
    public event Action OnStartMapScene;
    public event Action OnStartGameScene;
    
    public event Action OnExitMenuScene;
    public event Action OnExitMapScene;
    public event Action OnExitGameScene;
    
    // static instance, only 1 per level.
    public static SceneManager Instance  { get; private set; }
    
    // Once the Game starts
    private void Awake()
    {
        // this script will never unload
        DontDestroyOnLoad(this.gameObject);
        if (Instance == null)
            Instance = this;
        else
            Debug.Log(" Uh oh. Something went really wrong for this to not be null on awake()");
    }
}
