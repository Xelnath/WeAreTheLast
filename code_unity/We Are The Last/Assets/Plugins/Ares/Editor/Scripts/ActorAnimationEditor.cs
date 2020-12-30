using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomEditor(typeof(ActorAnimation)), CanEditMultipleObjects]
	public class ActorAnimationEditor : ActorComponentEditor {
		public static Animator animator;

		List<string> excludedProperties;
		int missingParamsReenableCounter;
		bool hasAnimatorParams;
		GameObject targetGameObject;
		bool resetAnimatorEnabledState;

		protected override void OnAfflictionAdd(SerializedProperty property){
			property.FindPropertyRelative("effect.valueFloat").floatValue = 1f;
			property.FindPropertyRelative("effect.valueBool").boolValue = true;
			property.FindPropertyRelative("effect.enabled").boolValue = true;
		}
		
		protected override void ItemClickHandler(object menuTarget){
			base.ItemClickHandler(menuTarget);
			
			var callbackElement = ((ActorAnimation)target).ItemCallbacks.Last();
			callbackElement.Enabled = true;
		}

		protected override void OnEnable(){
			base.OnEnable();

			float elementHeight = EditorGUIUtility.singleLineHeight * 2 + 4;

			rlItemCallbacks.elementHeight = elementHeight;
			rlAfflictionCallbacks.elementHeight = elementHeight;

			actor = ((MonoBehaviour)target).GetComponent<Actor>();
			
			rlAbilityCallbacks.elementHeightCallback = index => {
				if(index == actor.Abilities.Length){
					return actor.FallbackAbility != null && actor.FallbackAbility.Data != null ? elementHeight : 0f;
				}

				return elementHeight;
			};

			rlEventCallbacks.elementHeightCallback = index => {
				EventCallbackType type = (EventCallbackType)rlEventCallbacks.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("type").enumValueIndex;

				if(type == EventCallbackType.TakeDamage || type == EventCallbackType.Heal || type == EventCallbackType.BuffStat || type == EventCallbackType.DebuffStat){
					return elementHeight + EditorGUIUtility.singleLineHeight + 2;
				}
				else{
					return elementHeight;
				}
			};

			excludedProperties = new List<string>();

			SerializedProperty spAnimator = serializedObject.FindProperty("animator");
			animator = (Animator)spAnimator.objectReferenceValue;

			targetGameObject = ((ActorAnimation)serializedObject.targetObject).gameObject;
			hasAnimatorParams = targetGameObject.activeSelf && animator != null && animator.runtimeAnimatorController != null && animator.parameters.Length > 0;
			missingParamsReenableCounter = 0;
		}
		
		public override void OnInspectorGUI(){
			if(resetAnimatorEnabledState){
				animator.enabled = !animator.enabled;
				resetAnimatorEnabledState = false;
				EditorUtility.SetDirty(target);
				return;
			}

			excludedProperties.Clear();

			if(serializedObject.FindProperty("resetParametersAfterSet").boolValue){
				if(serializedObject.FindProperty("intParameterResetType").enumValueIndex == (int)ActorAnimation.ParamaterResetType.PreviousValue){
					excludedProperties.Add("intParameterResetValue");
				}

				if(serializedObject.FindProperty("floatParameterResetType").enumValueIndex == (int)ActorAnimation.ParamaterResetType.PreviousValue){
					excludedProperties.Add("floatParameterResetValue");
				}

				if(serializedObject.FindProperty("boolParameterResetType").enumValueIndex == (int)ActorAnimation.ParamaterResetType.PreviousValue){
					excludedProperties.Add("boolParameterResetValue");
				}
			}
			else{
				excludedProperties.Add("intParameterResetType");
				excludedProperties.Add("intParameterResetValue");
				excludedProperties.Add("floatParameterResetType");
				excludedProperties.Add("floatParameterResetValue");
				excludedProperties.Add("boolParameterResetType");
				excludedProperties.Add("boolParameterResetValue");
			}

			DrawPropertiesExcluding(serializedObject, hiddenProperties.Concat(excludedProperties).ToArray());

			SerializedProperty spAnimator = serializedObject.FindProperty("animator");
			animator = (Animator)spAnimator.objectReferenceValue;

			if(animator == null || animator.runtimeAnimatorController == null){
				EditorGUILayout.HelpBox("Please create and attach an Animator Controller to the Animator component.", MessageType.Error, true);

				serializedObject.ApplyModifiedProperties();
				return;
			}

			if(!targetGameObject.activeSelf){
				EditorGUILayout.HelpBox("Animator Parameters can not be shown on a disabled gameobject.",
					MessageType.Warning, true);

				serializedObject.ApplyModifiedProperties();
				return;
			}
			else if(!animator.enabled){
				EditorGUILayout.HelpBox("Animator Parameters can not be shown when the Animator component is disabled.",
					MessageType.Warning, true);

				serializedObject.ApplyModifiedProperties();
				return;
			}

			if(animator.parameterCount == 0){
				// There is an internal Unity bug that makes animator params unreadable after a scene save.
				// Toggling the Gameobject to be non-active and back clears the animator cache and "fixes" it.

				missingParamsReenableCounter++;

				if(hasAnimatorParams || missingParamsReenableCounter == 1){ //allow for one-time retry as well in case params are freshly added
					animator.enabled = !animator.enabled;
					serializedObject.ApplyModifiedProperties();
					resetAnimatorEnabledState = true;
					return;
				}
				else{
					EditorGUILayout.HelpBox("No parameters found on the Animator Controller.\n\n" +
					"(If paramaters do exist, enter and exit Play mode to refresh the Animator Controller. This is an internal Unity bug.)",
						MessageType.Warning, true);

					serializedObject.ApplyModifiedProperties();
					return;
				}
			}

			DrawLists();

			serializedObject.ApplyModifiedProperties();
		}
	}

	public class ActorAnimationElementDrawer : ActorCallbackElementDrawerBase {
		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			// Handled here because from here we have access to the Animator, and can elimate the use of magic strings.
			SerializedProperty spElemParameterName = property.FindPropertyRelative("effect.parameterName");
			SerializedProperty spElemParameterType = property.FindPropertyRelative("effect.parameterType");
			SerializedProperty spElemValueBool = property.FindPropertyRelative("effect.valueBool");
			SerializedProperty spElemValueFloat = property.FindPropertyRelative("effect.valueFloat");

			position.width += 28;

			int parameterNameIndex = 0;

			for(int j=0; j<ActorAnimationEditor.animator.parameters.Length; j++){
				if(ActorAnimationEditor.animator.parameters[j].name == spElemParameterName.stringValue){
					parameterNameIndex = j;
					break;
				}
			}

			float paramEnumX = position.x + thirdWidth + 88 - paramWidthShift;
			curPosition = new Rect(paramEnumX, position.y, Mathf.Min(140, position.width - paramEnumX - 20), EditorGUIUtility.singleLineHeight);

			string[] parameterNames = ActorAnimationEditor.animator.parameters.Select(p => p.name).ToArray();
			
			GUI.enabled = spElemEnabled.boolValue;

			parameterNameIndex = EditorGUI.Popup(curPosition, "Param", parameterNameIndex, parameterNames);
			spElemParameterName.stringValue = ActorAnimationEditor.animator.parameters[parameterNameIndex].name;

			SerializedProperty spDelayType = property.FindPropertyRelative("effect.delayType");
			EditorGUI.PropertyField(new Rect(curPosition.x, curPosition.y + EditorGUIUtility.singleLineHeight + 2, curPosition.width, curPosition.height), spDelayType, new GUIContent("Delay"));

			bool showValueLabel = position.width > 440;

			curPosition.x = position.x + position.width - (showValueLabel ? 92 : 52);
			curPosition.width = showValueLabel ? 66 : 26;
			curPosition.height = EditorGUIUtility.singleLineHeight;

			Rect nextPosition;

			switch(spDelayType.enumValueIndex){
				case (int)DelayType.Animation:
					SerializedProperty spAnimLayer = property.FindPropertyRelative("effect.delayAnimationLayer");
					nextPosition = new Rect(curPosition.x, curPosition.y + EditorGUIUtility.singleLineHeight + 2, curPosition.width, curPosition.height);
					spAnimLayer.intValue = showValueLabel ? EditorGUI.IntField(nextPosition, "Layer", spAnimLayer.intValue) : EditorGUI.IntField(nextPosition, spAnimLayer.intValue);
					break;
				case (int)DelayType.Time:
					SerializedProperty spDelayTime = property.FindPropertyRelative("effect.delayTime");
					nextPosition = new Rect(curPosition.x, curPosition.y + EditorGUIUtility.singleLineHeight + 2, curPosition.width, curPosition.height);
					spDelayTime.floatValue = showValueLabel ? EditorGUI.FloatField(nextPosition, "Time", spDelayTime.floatValue) : EditorGUI.FloatField(nextPosition, spDelayTime.floatValue);
					break;
			}

			switch(ActorAnimationEditor.animator.parameters[parameterNameIndex].type){
				case UnityEngine.AnimatorControllerParameterType.Bool:
					curPosition.x = position.x + position.width - (showValueLabel ? 82 : 42);
					curPosition.width = 60f;
					spElemValueBool.boolValue = showValueLabel ? EditorGUI.Toggle(curPosition, "Value", spElemValueBool.boolValue) : EditorGUI.Toggle(curPosition, spElemValueBool.boolValue);
					spElemParameterType.enumValueIndex = 2;
					break;
				case UnityEngine.AnimatorControllerParameterType.Int:
					spElemValueFloat.floatValue = showValueLabel ? EditorGUI.IntField(curPosition, "Value", (int)spElemValueFloat.floatValue) : EditorGUI.IntField(curPosition, (int)spElemValueFloat.floatValue);
					spElemParameterType.enumValueIndex = 1;
					break;
				case UnityEngine.AnimatorControllerParameterType.Float:
					spElemValueFloat.floatValue = showValueLabel ? EditorGUI.FloatField(curPosition, "Value", spElemValueFloat.floatValue) : EditorGUI.FloatField(curPosition, spElemValueFloat.floatValue);
					spElemParameterType.enumValueIndex = 0;
					break;
				default:
					spElemParameterType.enumValueIndex = 3;
					break;
			}
		}
	}

	[CustomPropertyDrawer(typeof(ActorAnimationAbilityElement))]
	public class ActorAnimationAbilityElementDrawer : ActorAnimationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			DrawAbility(position, property, label, "effect");
		}
	}

	[CustomPropertyDrawer(typeof(ActorAnimationEventElement))]
	public class ActorAnimationEventElementDrawer : ActorAnimationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.height = EditorGUIUtility.singleLineHeight;
			DrawEvent(position, property, label, "effect");
		}

		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			base.DrawSpecifics(position, property);

			float paramX = position.x + thirdWidth + 88 - paramWidthShift;

			DrawEventIgnoreFlags(property,
				new Rect(paramX, position.y + EditorGUIUtility.singleLineHeight * 2 + 4,
					Mathf.Min(140, position.width - paramX + 8), EditorGUIUtility.singleLineHeight),
				EditorGUIUtility.labelWidth + 30);
		}
	}

	[CustomPropertyDrawer(typeof(ActorAnimationAfflictionElement))]
	public class ActorAnimationAfflictionElementDrawer : ActorAnimationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			DrawAffliction(position, property, label, "effect");
		}
	}

	[CustomPropertyDrawer(typeof(ActorAnimationItemElement))]
	public class ActorAnimationItemElementDrawer : ActorAnimationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			DrawItem(position, property, label, "effect");
		}
	}
}