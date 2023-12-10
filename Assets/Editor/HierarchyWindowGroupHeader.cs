using UnityEngine;
using UnityEditor;

/// <summary>
/// Hierarchy Window Group Header
/// http://diegogiacomelli.com.br/unitytips-hierarchy-window-group-header
/// </summary>
[InitializeOnLoad]
public static class HierarchyWindowGroupHeader
{
    static HierarchyWindowGroupHeader()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
    }

    static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (gameObject != null && gameObject.name.StartsWith("---", System.StringComparison.Ordinal))
        {
            EditorGUI.DrawRect(selectionRect, Color.gray);

            // Calculate the position for centering the text
            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;

            EditorGUI.DropShadowLabel(new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height), gameObject.name.Replace("-", "").ToUpperInvariant(), labelStyle);
        }
    }
}