# 2D-Platformer
Open with **Unity 6000.6.4f1**, matching `ProjectSettings/ProjectVersion.txt`.

## Player camera

`PlayerMovementMultiplayer` automatically registers every spawned player with a
single `GroupFollowCamera2D` on the orthographic Main Camera. It follows one player
or frames the whole group, using the rendered sprite bounds after Fusion's
interpolation in `LateUpdate`. The offline `SpriteFollowCamera` test component
uses the same controller.

Add `GroupFollowCamera2D` to a scene's Main Camera in the Inspector to tune padding,
minimum orthographic size, position smoothing, and zoom smoothing. It is added
automatically at runtime if absent. Keep the camera aligned to world XY and behind
the players; its existing Z position is preserved. Expansion is immediate to keep
all players visible; contraction is smoothed. There is no maximum zoom-out cap.
Inactive, destroyed, or despawned players stop contributing to the frame. This is
a local view controller for the current single-runner setup, with no camera RPCs.

## Local placeable rotation

During build mode, **hold the left mouse button to drag a movable piece and press
R** to rotate it 90 degrees counterclockwise. Release to place. Rotation is local
only, just like the existing placement system; it is not synchronized by Fusion.

The preview, collider/visual root, pivot, occupied cells, boundary checks, and
green/red highlights rotate together. Rectangles and custom cell maps are
supported. Right-click, Escape, loss of focus, disabling the controller, leaving
build mode, or dropping onto an invalid location/UI restore the previous position
and rotation. The old cells remain reserved until a valid placement commits.

Set an item's initial **Quarter Turns** (0–3) on `GridPlaceable2D`; keep the root
and parent scales at one and the board/parents unrotated. Author the visual and
colliders in their zero-turn pose; the component applies the root's rotation.
The existing `PlaceableDefinition2D.GameMap` selection remains supported.

## Verification

Run **Tools > Ultimate Grid 2D > Run Camera And Rotation Checks** in Edit Mode, or:

```text
Unity -batchmode -nographics -projectPath <project> -executeMethod CameraAndGridChecks.Run -quit -logFile <log>
```

The checks cover irregular and rectangular rotation, asymmetric pivots, four-turn
wraparound, atomic occupancy changes, overlap/boundary rejection, cancellation,
physics-state restoration, camera framing at different aspect ratios, and target
removal. They use temporary objects and clean them up afterward. In an empty test
scene, enter Play Mode and run the same menu command to also simulate mouse pickup,
R-key rotation, release-to-commit, and fixed-piece rejection with the new Input System.

Before merging, also play-test in the target Unity version:

1. Start a host and a separate client. Move the non-host player while watching
   its camera, then move players apart horizontally and vertically. All spawned
   players should remain visible; compare movement at different frame rates and
   with simulated network latency. The camera timing fix does not change Fusion's
   underlying physics prediction or guarantee removal of network corrections.
2. Disconnect/rejoin and switch gameplay scenes. Confirm the new Main Camera
   follows the active players and a missing camera causes no null-reference error.
3. Use the grid demo or a board in build mode. Drag the 3x1 platform and press R
   through all four orientations; repeat with an L-shaped map and a noncentral pivot.
4. Try rotated overlaps and board edges, then valid drops, cancel actions, and
   drops over UI. Check the original cells remain reserved after a rejected drop
   and are released after a successful one. Fixed pieces and play mode must ignore R.

Camera timing follows [Photon's Fusion update loop](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/fusion-update-loop):
Fusion renders interpolated state before Unity's `LateUpdate` callbacks.
