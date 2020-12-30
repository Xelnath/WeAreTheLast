using UnityEngine;
using UnityEditor;

public abstract class AresPropertyDrawer : PropertyDrawer {
	protected float GetDefaultPropertyHeight(int numLines){
		return EditorGUIUtility.singleLineHeight * numLines + EditorGUIUtility.standardVerticalSpacing * (numLines - 1);
	}

	protected void DrawLabelField(ref Rect rect, GUIContent label){
		EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
		rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	}

	protected void DrawPropertyField(ref Rect rect, SerializedProperty property){
		EditorGUI.PropertyField(rect, property);
		rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	}

	protected void DrawPropertyField(ref Rect rect, SerializedProperty property, string label){
		EditorGUI.PropertyField(rect, property, new GUIContent(label));
		rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	}
}
