using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(StatData), true)]
	public class StatDataEditor : AresEditor {
		readonly string[] propertyGroup1 = {"minStage", "maxStage"};
		readonly string[] hiddenProperties = {"m_Script", "displayName", "powerIncrement", "powerMultiplier", "powerFormula", "powerScalingMode"};

		int debugBasePower = 50;
		
		public override void OnInspectorGUI(){
			serializedObject.Update();

			SerializedProperty spPowerScalingMode = serializedObject.FindProperty("powerScalingMode");
			PowerScaling powerScalingMode = (PowerScaling)spPowerScalingMode.enumValueIndex;

			EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));

			EditorGUILayout.LabelField("Scaling", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(spPowerScalingMode);

			if(powerScalingMode == PowerScaling.Linear || powerScalingMode == PowerScaling.ExponentialComplex || powerScalingMode == PowerScaling.ExponentialComplexSymmetric){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerMultiplier"));
			}

			if(powerScalingMode == PowerScaling.ExponentialSimple || powerScalingMode == PowerScaling.ExponentialSimpleSymmetric ||
			   powerScalingMode == PowerScaling.ExponentialComplex || powerScalingMode == PowerScaling.ExponentialComplexSymmetric){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerIncrement"));
			}
			else if(powerScalingMode == PowerScaling.Custom){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerFormula"));

					EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Default formula tokens:\tFunctions:\nSTAGE\t\t\tABS()\n" +
					"\t\t\tMIN()\n\t\t\tMAX()\n\nMacros:\n#D# (dice, i.e. 1D6)",
					MessageType.Info);
			}

			EditorGUILayout.Space();
			foreach(string prop in propertyGroup1){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}

			DrawMiscProperties(propertyGroup1.Concat(hiddenProperties).ToArray());

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			int xOffset = 0;

			DrawLabel(rect, ref xOffset, "Results for base power");

			xOffset -= 4;

			debugBasePower = EditorGUI.IntField(new Rect(rect.x + xOffset, rect.y, 30f, rect.height), debugBasePower);

			xOffset += 30;

			DrawLabel(rect, ref xOffset, " are as follows:");

			DrawPowerHelpBox(debugBasePower, (PowerData)target);

			serializedObject.ApplyModifiedProperties();
		}
	}
}