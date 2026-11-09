using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PlayerMovementChecks
{
    static readonly List<GameObject> objects = new List<GameObject>();
    static Scene scene;
    static int checks;

    [MenuItem("Tools/Player/Run Movement Checks (Play Mode)")]
    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run these checks in Play Mode.");
        checks = 0;
        scene = SceneManager.CreateScene("Movement checks", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        try
        {
            WalkingAndJumping();
            ClimbingAndPhases();
            InputSampling();
            Debug.Log($"Player movement checks passed: {checks}");
        }
        finally
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
            objects.Clear();
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    [MenuItem("Tools/Player/Run Movement Checks (Play Mode)", true)]
    static bool CanRun() => Application.isPlaying;

    static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }

    static GameObject Make(string name, Vector2 position)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position;
        objects.Add(go);
        return go;
    }

    static PlatformerMotor2D Player(Vector2 position)
    {
        var go = Make("Player", position);
        go.AddComponent<Rigidbody2D>();
        // Match the team's prefab: collider on a visual child, body/motor on root.
        var visual = Make("Body", position);
        visual.transform.SetParent(go.transform, true);
        visual.AddComponent<BoxCollider2D>().size = new Vector2(1, 2);
        return go.AddComponent<PlatformerMotor2D>();
    }

    static void Tick(PlatformerMotor2D motor, float walk = 0, float climb = 0, bool jump = false, bool enabled = true)
    {
        motor.Step(walk, climb, jump, enabled, .02f);
        scene.GetPhysicsScene2D().Simulate(.02f);
    }

    static void WalkingAndJumping()
    {
        var ground = Make("Ground", new Vector2(0, -.5f));
        ground.AddComponent<BoxCollider2D>().size = new Vector2(100, 1);
        var motor = Player(new Vector2(0, 1.02f));
        for (int i = 0; i < 15; i++) Tick(motor);
        Check(motor.IsGrounded, "Standing on a child collider must be grounded");
        for (int i = 0; i < 30; i++) Tick(motor, walk: 1);
        Check(motor.transform.position.x > 1, "Walk input must move player");
        Check(Mathf.Abs(motor.Velocity.x - motor.walkSpeed) < .1f, "Walking must reach bounded speed");
        for (int i = 0; i < 20; i++) Tick(motor);
        Check(Mathf.Abs(motor.Velocity.x) < .01f, "Releasing movement must brake");
        Tick(motor, walk: -1);
        Check(motor.FacingDirection == -1, "Facing direction must follow walking");
        Tick(motor, jump: true);
        Check(motor.Velocity.y > 9, "Grounded Space must jump");
        Check(!motor.IsGrounded, "Jump must clear grounded state");
        Tick(motor, jump: true);
        Check(motor.Velocity.y < motor.jumpSpeed, "Holding Space must not reset upward velocity");
        Tick(motor);
        Tick(motor, jump: true);
        Check(motor.Velocity.y < motor.jumpSpeed - .3f, "Pressing Space in the air must not double-jump");
        for (int i = 0; i < 150; i++) Tick(motor, jump: true);
        Check(motor.IsGrounded && Mathf.Abs(motor.Velocity.y) < .1f, "Held jump must not auto-hop after landing");
        Tick(motor);
        Tick(motor, jump: true);
        Check(motor.Velocity.y > 9, "Releasing and pressing Space after landing must jump again");

        // Wall contact and ordinary trigger volumes cannot grant a grounded jump or flight.
        var airborne = Player(new Vector2(60, 4));
        var wall = Make("Wall", new Vector2(61, 4));
        wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 10);
        var trigger = Make("Ordinary trigger", new Vector2(60, 4)).AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(3, 3);
        trigger.isTrigger = true;
        Tick(airborne, climb: 1, jump: true);
        Check(!airborne.IsGrounded && airborne.Velocity.y <= 0, "Wall or trigger must not enable a jump");
        Check(!airborne.IsClimbing, "W outside marked climbables must not fly");
        for (int i = 0; i < 160; i++) Tick(airborne);
        Check(airborne.Velocity.y >= -airborne.maximumFallSpeed - .3f, "Falling speed must be capped at each motor step");
        Tick(airborne, walk: float.NaN, climb: float.PositiveInfinity);
        Check(!float.IsNaN(airborne.Velocity.x) && !float.IsInfinity(airborne.Velocity.y), "Invalid input must not poison physics");
    }

    static void ClimbingAndPhases()
    {
        var motor = Player(new Vector2(100, 5));
        var ladder = Make("Climbable", new Vector2(100, 5));
        ladder.AddComponent<BoxCollider2D>().size = new Vector2(3, 20);
        var marker = ladder.AddComponent<Climbable2D>();
        Tick(motor, climb: 1);
        Check(motor.IsClimbing && Mathf.Approximately(motor.Velocity.y, motor.climbSpeed), "Positive float must climb up");
        Tick(motor, climb: -1);
        Check(motor.Velocity.y < 0, "Negative float must climb down");
        Tick(motor);
        Check(motor.IsClimbing && motor.Velocity.y == 0, "Zero input must hold on a ladder");
        Tick(motor, climb: 1, jump: true);
        Check(!motor.IsClimbing && motor.Velocity.y > 9, "Space must detach and jump off a ladder");
        Tick(motor, climb: 1, jump: true);
        Check(!motor.IsClimbing && motor.Velocity.y > motor.climbSpeed, "Held W must not immediately cancel a ladder jump");
        Tick(motor);
        Tick(motor, climb: 1);
        Check(motor.IsClimbing, "Releasing and pressing W must allow re-grabbing");
        marker.enabled = false;
        Tick(motor, climb: 1);
        Check(!motor.IsClimbing && motor.GetComponent<Rigidbody2D>().gravityScale == 1, "Disabled ladder must release player and restore gravity");
        marker.enabled = true;
        Tick(motor, climb: 1);
        for (int i = 0; i < 50; i++) Tick(motor, walk: 1, climb: 1);
        Check(!motor.IsClimbing, "Walking outside the trigger must release the ladder");
        Tick(motor, walk: 1, jump: true, enabled: false);
        Vector3 frozen = motor.transform.position;
        for (int i = 0; i < 10; i++) Tick(motor, walk: 1, jump: true, enabled: false);
        Check(motor.Velocity == Vector2.zero && motor.transform.position == frozen, "Build mode must stop velocity and gravity");
        Tick(motor, jump: true);
        Check(motor.Velocity.y <= 0, "Holding Space through a phase switch must not cause a jump");
        Check(motor.GetComponent<Rigidbody2D>().gravityScale == 1, "Play mode must restore prefab gravity");
        Check((motor.GetComponent<Rigidbody2D>().constraints & RigidbodyConstraints2D.FreezeRotation) != 0, "Player must remain upright");
    }

    static void InputSampling()
    {
        var savedSettings = InputSystem.settings;
        var settings = Object.Instantiate(savedSettings);
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = settings;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            var sampler = new PlatformerInputSampler();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            sampler.Capture(keyboard);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            sampler.Read(keyboard, out var move, out float climb);
            Check(move.y == 1, "A short jump tap between ticks must survive sampling");
            sampler.Read(keyboard, out move, out climb);
            Check(move.y == 0, "Queued jump must be consumed only once");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.S));
            InputSystem.Update();
            sampler.Read(keyboard, out move, out climb);
            Check(move.x == -1 && climb == -1, "A/S must move left and climb down");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.W));
            InputSystem.Update();
            sampler.Read(keyboard, out move, out climb);
            Check(move.x == 1 && climb == 1, "D/W must move right and climb up");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.D, Key.W, Key.S));
            InputSystem.Update();
            sampler.Read(keyboard, out move, out climb);
            Check(move == Vector2.zero && climb == 0, "Opposing keys must cancel");
            sampler.Read(null, out move, out climb);
            Check(move == Vector2.zero && climb == 0, "Missing keyboard must return neutral input");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            sampler.Capture(keyboard);
            sampler.Clear();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            sampler.Read(keyboard, out move, out climb);
            Check(move.y == 0, "Focus loss must clear queued jump");
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = savedSettings;
            Object.DestroyImmediate(settings);
        }
    }
}
