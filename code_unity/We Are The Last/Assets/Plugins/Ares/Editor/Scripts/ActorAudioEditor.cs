using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomEditor(typeof(ActorAudio)), CanEditMultipleObjects]
	public class ActorAudioEditor : ActorComponentEditor {
		float emptyElementHeight = 21;
		float fullElementHeight = EditorGUIUtility.singleLineHeight * 4f + ActorCallbackElementDrawerBase.lineSpacing * 4 + 6;

		protected override void OnEnable(){
			base.OnEnable();

			rlAbilityCallbacks.elementHeight = rlAbilityCallbacks.count > 0 ? fullElementHeight : emptyElementHeight;
			rlAfflictionCallbacks.elementHeight = rlAfflictionCallbacks.count > 0 ? fullElementHeight : emptyElementHeight;
			rlItemCallbacks.elementHeight = rlItemCallbacks.count > 0 ? fullElementHeight : emptyElementHeight;

			actor = ((MonoBehaviour)target).GetComponent<Actor>();
			
			rlAbilityCallbacks.elementHeightCallback = (index) => {
				if(index == actor.Abilities.Length && (actor.FallbackAbility == null || actor.FallbackAbility.Data == null)){
					return 0f;
				}

				return ((ActorAudio)target).AbilityCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};


			rlEventCallbacks.elementHeightCallback = (index) => {
				if(((ActorAudio)target).EventCallbacks[index].EditorShowExpanded){
					EventCallbackType type = (EventCallbackType)rlEventCallbacks.serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("type").enumValueIndex;

					if(type == EventCallbackType.TakeDamage || type == EventCallbackType.Heal || type == EventCallbackType.BuffStat || type == EventCallbackType.DebuffStat){
						return fullElementHeight + EditorGUIUtility.singleLineHeight + 2;
					}
					else{
						return fullElementHeight;
					}
				}

				return emptyElementHeight;
			};

			rlAfflictionCallbacks.elementHeightCallback = (index) => {
				return ((ActorAudio)target).AfflictionCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};

			rlItemCallbacks.elementHeightCallback = (index) => {
				return ((ActorAudio)target).ItemCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};
		}

		protected override void OnDisable(){
			base.OnDisable();

			rlAbilityCallbacks.elementHeightCallback = null;
			rlAfflictionCallbacks.elementHeightCallback = null;
			rlItemCallbacks.elementHeightCallback = null;
		}

		protected override void OnAfflictionAdd(SerializedProperty property){
			property.FindPropertyRelative("editorShowExpanded").boolValue = true;
			property.FindPropertyRelative("effect.volume").floatValue = 1f;
			property.FindPropertyRelative("effect.playPosition").intValue = (int)AudioPlayPosition.Target;
			property.FindPropertyRelative("effect.enabled").boolValue = true;
		}

		protected override void ItemClickHandler(object menuTarget){
			base.ItemClickHandler(menuTarget);

			ActorAudioItemElement callbackElement = ((ActorAudio)target).ItemCallbacks.Last();
			callbackElement.Effect.Set(null, AudioPlayPosition.Target, 1f, 0f);
			callbackElement.Effect.enabled = true;
			callbackElement.EditorShowExpanded = true;
		}

		public override void OnInspectorGUI(){
			List<string> excludedProperties = new List<string>();

			if(!serializedObject.FindProperty("allowDefaultAbilityAudio").boolValue)
				excludedProperties.Add("abilityCallbackMode");
			if(!serializedObject.FindProperty("allowDefaultItemAudio").boolValue)
				excludedProperties.Add("itemCallbackMode");
			if(!serializedObject.FindProperty("allowDefaultAfflictionAudio").boolValue)
				excludedProperties.Add("afflictionCallbackMode");

			DrawPropertiesExcluding(serializedObject, hiddenProperties.Concat(excludedProperties).ToArray());
			DrawLists();

			serializedObject.ApplyModifiedProperties();

			ShowEventTargetWarningIfNecessary();
		}
	}

	public class ActorAudioElementDrawer : ActorCallbackElementDrawerBase {
		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			curPosition = new Rect(position.x + thirdWidth - dragHandleCorrection, position.y + 2, thirdWidth*2 + dragHandleCorrection, position.height);
			
			GUI.enabled = spElemEnabled.boolValue;

			curPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			curPosition.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField(curPosition, property.FindPropertyRelative("effect"), true);
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorAudioAbilityElement))]
	public class ActorAudioAbilityElementDrawer : ActorAudioElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawAbility(position, property, label, "effect");
		}
	}

	[CustomPropertyDrawer(typeof(ActorAudioEventElement))]
	public class ActorAudioEventElementDrawer : ActorAudioElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			DrawEvent(position, property, label, "effect");
		}

		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			base.DrawSpecifics(position, property);

			DrawEventIgnoreFlags(property,
				new Rect(position.x + 36, position.y + (EditorGUIUtility.singleLineHeight + 2) * 5 + 2, position.width - 72, EditorGUIUtility.singleLineHeight),
				115);
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorAudioAfflictionElement))]
	public class ActorAudioAfflictionElementDrawer : ActorAudioElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawAffliction(position, property, label, "effect");
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorAudioItemElement))]
	public class ActorAudioItemElementDrawer : ActorAudioElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawItem(position, property, label, "effect");
		}
	}
}