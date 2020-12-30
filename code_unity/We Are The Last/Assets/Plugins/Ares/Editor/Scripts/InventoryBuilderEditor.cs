using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ares.Editor {
	[CustomEditor(typeof(InventoryBuilder))]
	public class InventoryBuilderEditor : AresEditor {
		public override void OnInspectorGUI(){
			serializedObject.Update();

			SerializedProperty spGenerator = serializedObject.FindProperty("generator");
			spGenerator.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent("Generator"), spGenerator.objectReferenceValue, typeof(InventoryGenerator), false);
			
//			EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("inventoryType"));

			if(((InventoryBuilder)target).GetComponent<Actor>() != null){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("assignToActorOnAwake"));
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}