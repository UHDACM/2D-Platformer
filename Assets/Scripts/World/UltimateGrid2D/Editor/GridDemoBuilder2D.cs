using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimateGrid2D.Editor
{
    public static class GridDemoBuilder2D
    {
        [MenuItem("Tools/Ultimate Grid 2D/Create Demo In Current Scene")]
        static void CreateDemo()
        {
            const string folder = "Assets/Enviroment/World/UltimateGrid2D/";
            Directory.CreateDirectory(folder);
            string texturePath = AssetDatabase.GenerateUniqueAssetPath(folder + "/Square.png");
            var texture = new Texture2D(8, 8);
            var pixels = new Color[64]; for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 8;
            importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            var root = new GameObject("Ultimate Grid 2D Demo"); Undo.RegisterCreatedObjectUndo(root, "Create grid demo");
            var board = new GameObject("Board"); board.transform.SetParent(root.transform);
            var grid = board.AddComponent<PlacementGrid2D>(); grid.dimensions = new Vector2Int(20, 12);
            grid.transform.position = new Vector3(-10, -6, 0);
            var overlay = board.AddComponent<GridOverlay2D>(); overlay.grid = grid;
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera"); cameraObject.transform.SetParent(root.transform);
                camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.orthographic = true; camera.orthographicSize = 8; camera.transform.position = new Vector3(0, 0, -10);
                camera.backgroundColor = new Color(.06f, .08f, .12f);
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
            var controller = root.AddComponent<GridDragController2D>(); controller.grid = grid; controller.overlay = overlay; controller.worldCamera = camera;
            Make(root.transform, grid, sprite, folder, "Platform 3x1", new Vector2Int(3, 1), new Vector2Int(3, 4), new Color(.2f, .75f, 1), true);
            Make(root.transform, grid, sprite, folder, "Block 1x1", Vector2Int.one, new Vector2Int(10, 6), new Color(1, .65f, .2f), true);
            Make(root.transform, grid, sprite, folder, "Wall 1x3", new Vector2Int(1, 3), new Vector2Int(15, 2), new Color(.8f, .4f, 1), true);
            Make(root.transform, grid, sprite, folder, "Fixed Ground", new Vector2Int(20, 1), Vector2Int.zero, new Color(.35f, .4f, .45f), false);
            EditorSceneManager.MarkSceneDirty(root.scene); Selection.activeGameObject = root;
            AssetDatabase.SaveAssets();
        }
        static void Make(Transform parent, PlacementGrid2D grid, Sprite sprite, string folder, string label, Vector2Int size, Vector2Int anchor, Color color, bool movable)
        {
            var definition = ScriptableObject.CreateInstance<PlaceableDefinition2D>();
            definition.size = size; definition.pivot = (Vector2)size / 2;
            AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + label + ".asset"));
            var go = new GameObject(label); go.transform.SetParent(parent);
            go.transform.position = grid.PivotPosition(anchor, definition.pivot);
            var item = go.AddComponent<GridPlaceable2D>(); item.grid = grid; item.definition = definition; item.movable = movable;
            var visual = new GameObject("Visual"); visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size.x * grid.cellSize, size.y * grid.cellSize, 1);
            var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            visual.AddComponent<BoxCollider2D>();
        }
    }
}
