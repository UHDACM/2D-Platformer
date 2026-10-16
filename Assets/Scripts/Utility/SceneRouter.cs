using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneRouter : MonoBehaviourPlus
{
    public override string prettyName => "Scene Router";
    public override string description => "Controlls the many Scenes in the game. Lets us go from Menu->Map->Game and back";

    ////////////////////////////////
    //
    ////////////////////////////////
    
    // Unity Events
    public event Action OnStartMenuScene;
    public event Action OnStartMapScene;
    public event Action OnStartGameScene;
    
    public event Action OnExitMenuScene;
    public event Action OnExitMapScene;
    public event Action OnExitGameScene;
    
    // Scene names
    [Header("System Scenes")]
    public string menu_scene;
    public string map_scene;
    public string loading_scene;

    [Header("Map Scenes")]
    public string undersea_scene;
    public string ice_scene;
    public string lava_scene;
    public string space_scene;
    public string grass_scene;

    
    // static instance, only 1 per build.
    public static SceneRouter Instance  { get; private set; }
    
    
    ////////////////////////////////
    //
    ////////////////////////////////
    
    // Once the Game starts
    private void Awake()
    {
        // this script will never unload
        if (Instance == null)
        {
            DontDestroyOnLoad(this.gameObject);
            Instance = this;
        }
        else
            Debug.Log(" Uh oh. Something went really wrong for this to not be null on awake()");
    }

    /* Only to test the Scene Router
    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
            HostStarts();
        if (Keyboard.current.bKey.wasPressedThisFrame)
            MapChosen(Enums.Maps.Grass);
        if (Keyboard.current.cKey.wasPressedThisFrame)
            GameEnds();
        if (Keyboard.current.dKey.wasPressedThisFrame)
            QuitMapScene();
    } */
    
    ////////////////////////////////
    //
    ////////////////////////////////

    public void HostStarts() => StartCoroutine(HostStartsLoad());
    public void QuitMapScene() => StartCoroutine(QuitMapSceneLoad());
    public void MapChosen(Enums.Maps mapChosen) => StartCoroutine(MapChosenLoad(mapChosen));
    public void GameEnds() => StartCoroutine(GameEndsLoad());

    // Coroutines
    private IEnumerator HostStartsLoad()
    {
        OnExitMenuScene?.Invoke();

        SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(map_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Map Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.UnloadSceneAsync(loading_scene);
        OnStartMapScene?.Invoke();
    }
    
    ////////////////////////////////
    //
    ////////////////////////////////
    
    private IEnumerator QuitMapSceneLoad()
    {
        OnExitMapScene?.Invoke();

        SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(menu_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.UnloadSceneAsync(loading_scene);
        OnStartMenuScene?.Invoke();
    }
    
    
    ////////////////////////////////
    //
    ////////////////////////////////

    private IEnumerator MapChosenLoad(Enums.Maps mapChosen)
    {
        OnExitMapScene?.Invoke();

        SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = new AsyncOperation();
        
        // Multiplex between possible maps
        switch (mapChosen)
        {
            case Enums.Maps.Undersea:
                asyncLoad = SceneManager.LoadSceneAsync(undersea_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Ice:
                asyncLoad = SceneManager.LoadSceneAsync(ice_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Lava:
                asyncLoad = SceneManager.LoadSceneAsync(lava_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Space:
                asyncLoad = SceneManager.LoadSceneAsync(space_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Grass:
                asyncLoad = SceneManager.LoadSceneAsync(grass_scene, LoadSceneMode.Additive);
                break;
            default:
                Debug.LogError("Uh oh. Something seriously went wrong with the Scene Router rip. ");
                break;
        }
        
        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.UnloadSceneAsync(loading_scene);
        OnStartGameScene?.Invoke();
    }

    ////////////////////////////////
    //
    ////////////////////////////////
    
    private IEnumerator GameEndsLoad()
    {
        OnExitGameScene?.Invoke();

        SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(map_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.UnloadSceneAsync(loading_scene);
        OnStartMapScene?.Invoke();
    }
    
}
