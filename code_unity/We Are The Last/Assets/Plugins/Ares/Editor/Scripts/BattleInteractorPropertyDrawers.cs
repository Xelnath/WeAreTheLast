using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomPropertyDrawer(typeof(BattleInteractorData.PreparationData))]
	public class ChargePropertyDrawer : ChargeRechargePropertyDrawer {
		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label){
			Draw(rect, property, "Preparation Turns", "Interrupt");
		}
	}

	[CustomPropertyDrawer(typeof(BattleInteractorData.RecoveryData))]
	public class RechargePropertyDrawer : ChargeRechargePropertyDrawer {
		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label){
			Draw(rect, property, "Recovery Turns", "Forego Recovery");
		}
	}

	public class ChargeRechargePropertyDrawer : AresPropertyDrawer {
		ReorderableList rlTexts;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
			int numTurns = property.FindPropertyRelative("turns").intValue;
			int numLines = 1 + (numTurns > 0 ? (numTurns + 2) : 0);

			return GetDefaultPropertyHeight(numLines) + numLines * 3 - (numLines > 1 ? 16f : 0f);
		}

		protected void Draw(Rect rect, SerializedProperty property, string turnsLabel, string interruptLabel){
			SerializedProperty spTurns = property.FindPropertyRelative("turns");
			SerializedProperty spAnimations = property.FindPropertyRelative("animations");
			SerializedProperty spAudios = property.FindPropertyRelative("audios");
			SerializedProperty spInstantiations = property.FindPropertyRelative("instantiations");

			float startX = rect.x;
			float startWidth = rect.width;

			rect.width = EditorGUIUtility.labelWidth + 50f;
			rect.height = EditorGUIUtility.singleLineHeight;

			EditorGUI.PropertyField(rect, spTurns, new GUIContent(turnsLabel));

			spTurns.intValue = Mathf.Max(0, spTurns.intValue);

			rect.x += rect.width + 10f;
			rect.width = startWidth - rect.width - 10f;

			GUI.enabled = spTurns.intValue > 0;
			EditorGUIUtility.labelWidth = 100f;
			EditorGUI.PropertyField(rect, property.FindPropertyRelative("interrupt"), new GUIContent(interruptLabel));
			GUI.enabled = true;

			if(spTurns.intValue > 0){
				SerializedProperty spTexts = property.FindPropertyRelative("texts");
				spTexts.arraySize = spAnimations.arraySize = spAudios.arraySize = spInstantiations.arraySize = spTurns.intValue;

				if(rlTexts == null){
					rlTexts = new ReorderableList(property.serializedObject, spTexts, true, true, false, false);
					rlTexts.drawHeaderCallback = _rect => GUI.Label(_rect, "Messages");
//					rlTexts.headerHeight = 4f;
					rlTexts.drawElementCallback = (_rect, index, active, focused) => {
						rlTexts.serializedProperty.GetArrayElementAtIndex(index).stringValue = EditorGUI.TextField(_rect, rlTexts.serializedProperty.GetArrayElementAtIndex(index).stringValue);
					};
				}

				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
//				rect.x = startX;
//				EditorGUI.LabelField(rect, "Preparation Messages");

				EditorGUIUtility.labelWidth = 0f;
				rect.x = startX + EditorGUIUtility.labelWidth;
				rect.width = startWidth - rect.x + 15f;

//				EditorGUI.PropertyField(rect, property.FindPropertyRelative("texts"), new GUIContent("Bla"));
				rlTexts.DoList(rect);

				property.serializedObject.ApplyModifiedProperties();
			}
		}
	}
}