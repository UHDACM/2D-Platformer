using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(ScriptableObjectPlus), true), CanEditMultipleObjects]
public class ScriptableObjectPlusEditor : MonoBehaviourPlusEditorBase { }
[CustomEditor(typeof(MonoBehaviourPlus), true), CanEditMultipleObjects]
public class MonoBehaviourPlusEditor : MonoBehaviourPlusEditorBase { }
public abstract class MonoBehaviourPlusEditorBase : UnityEditor.Editor
{
    private bool _initialized;
    //used to hide the script field
    private static readonly string[] _excludedProperties = new string[] { "m_Script" };
    private Texture2D _backgroundTexture;
    private Texture2D _subBackgroundTexture;
    private Texture2D _buttonTexture;
    private Texture2D _iconTexture;
    private Texture2D _caretUpTexture;
    private Texture2D _caretDownTexture;

    protected GUIStyle _warningStyle;
    private GUIStyle _areaStyle;
    private GUIStyle _subAreaStyle;
    private GUIStyle _logoStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _subTitleStyle;
    private GUIStyle _helpButtonStyle;
    private GUIStyle _caretStyle;
    private GUIStyle _hiddenToggleButtonStyle;

    private string _prettyName;
    private string _tooltip;

    private void InitializeIfNecessary(UnityEngine.Object target)
    {
        if (_initialized)
        {
            return;
        }

        if (target is MonoBehaviourPlus component)
        {
            _prettyName = component.prettyName;
            _tooltip = component.description;
        }
        else if (target is ScriptableObjectPlus scriptableObject)
        {
            _prettyName = scriptableObject.prettyName;
            _tooltip = scriptableObject.description;
        }
        else
        {
            _prettyName = target.GetType().Name;
            _tooltip = "";
        }

        GUIContent c = EditorGUIUtility.ObjectContent(target, target.GetType());
        
        if (c.image == null)
        {
            _iconTexture = SpatialGUIUtility.LoadGUITexture("GUI/SpatialLogo.png");
        }
        else
        {
            _iconTexture = (Texture2D)c.image;
        }

        _backgroundTexture = SpatialGUIUtility.LoadGUITexture("GUI/TooltipBackground.png");
        _subBackgroundTexture = SpatialGUIUtility.LoadGUITexture("GUI/TooltipSubBackground.png");
        _buttonTexture = SpatialGUIUtility.LoadGUITexture("GUI/ButtonBackground.png");
        _caretUpTexture = SpatialGUIUtility.LoadGUITexture("GUI/CaretUp.png");
        _caretDownTexture = SpatialGUIUtility.LoadGUITexture("GUI/CaretDown.png");

        _warningStyle = new GUIStyle() {
            padding = new RectOffset(0, 0, 5, 0),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
        };
        _warningStyle.normal.textColor = new Color(1, .66f, 0f);
        _warningStyle.active.textColor = new Color(1, .66f, 0f);

        _areaStyle = new GUIStyle() {
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(8, 8, 8, 8),
            alignment = TextAnchor.MiddleLeft,
        };
        _areaStyle.normal.background = _backgroundTexture;
        _areaStyle.normal.textColor = Color.white;

        _subAreaStyle = new GUIStyle() {
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(16, 8, 8, 8),
            alignment = TextAnchor.MiddleLeft,
        };
        _subAreaStyle.normal.background = _subBackgroundTexture;
        _subAreaStyle.normal.textColor = Color.white;

        _logoStyle = new GUIStyle() {
            fixedHeight = 28,
            fixedWidth = 28,
            padding = new RectOffset(0, 0, 0, 0),
            border = new RectOffset(0, 0, 0, 0),
        };

        _caretStyle = new GUIStyle() {
            contentOffset = new Vector2(0, 4),
        };

        _titleStyle = new GUIStyle() {
            fontStyle = FontStyle.Bold,
            fontSize = 20,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
        };
        _titleStyle.normal.textColor = Color.white;

        _subTitleStyle = new GUIStyle() {
            fontSize = 12,
            wordWrap = true,
            richText = true,
        };
        _subTitleStyle.normal.textColor = new Color(1, 1, 1, .75f);

        _helpButtonStyle = new GUIStyle() {
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(4, 4, 4, 4),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            fixedWidth = 48,
        };
        _helpButtonStyle.active.background = _buttonTexture;
        _helpButtonStyle.normal.background = _buttonTexture;

        // an invisible button style
        _hiddenToggleButtonStyle = new GUIStyle() {
        };
    }

    public override void OnInspectorGUI()
    {
        var editorTarget = target as UnityEngine.Object;
        InitializeIfNecessary(editorTarget);
        serializedObject.Update();

        bool showSubMenu = EditorPrefs.GetBool("_InspExpand_" + target.GetType().Name, true);

        GUILayout.Space(-4);// Top margin hack
        GUILayout.BeginHorizontal();
        GUILayout.Space(-18);// Left margin hack

        //! START
        GUILayout.BeginVertical();
        GUILayout.BeginVertical(_areaStyle);
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Box(_iconTexture, _logoStyle);
                GUILayout.Space(4);

                // Align to the center of the icon
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(_prettyName, _titleStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                GUILayout.Box(showSubMenu ? _caretUpTexture : _caretDownTexture, _caretStyle);
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label(showSubMenu ? "Less" : "More");
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

        }
        GUILayout.EndVertical();

        GUILayout.Space(-44);
        if (GUILayout.Button("", _hiddenToggleButtonStyle, GUILayout.Height(44)))
        {
            EditorPrefs.SetBool("_InspExpand_" + target.GetType().Name, !showSubMenu);
        }

        if (showSubMenu)
        {
            GUILayout.Space(-5);// account for texture corners
            // * Subtitle
            GUILayout.BeginVertical(_subAreaStyle);
            GUILayout.Space(4);

            GUILayout.Label(_tooltip, _subTitleStyle);
            GUILayout.EndVertical();
        }
        //! END
        GUILayout.EndVertical();

        GUILayout.Space(-4);// Right Margin hack
        GUILayout.EndHorizontal();

        GUILayout.Space(6);// Margin between tooltip and inspector

        DrawFields();

        serializedObject.ApplyModifiedProperties();
    }

    public virtual void DrawFields()
    {
        DrawPropertiesExcluding(serializedObject, _excludedProperties);
    }
}
