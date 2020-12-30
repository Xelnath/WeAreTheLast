using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomEditor(typeof(ActorInstantiation)), CanEditMultipleObjects]
	public class ActorInstantiationEditor : ActorComponentEditor {
		float emptyElementHeight = 21;
		float fullElementHeight = EditorGUIUtility.singleLineHeight * 7 + ActorCallbackElementDrawerBase.lineSpacing * 6;

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

				return ((ActorInstantiation)target).AbilityCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};

			rlEventCallbacks.elementHeightCallback = (index) => {
				if(((ActorInstantiation)target).EventCallbacks[index].EditorShowExpanded){
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
				return ((ActorInstantiation)target).AfflictionCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};

			rlItemCallbacks.elementHeightCallback = (index) => {
				return ((ActorInstantiation)target).ItemCallbacks[index].EditorShowExpanded ? fullElementHeight : emptyElementHeight;
			};
		}

		protected override void OnDisable(){
			base.OnDisable();

			rlAbilityCallbacks.elementHeightCallback = null;
			rlAfflictionCallbacks.elementHeightCallback = null;
			rlItemCallbacks.elementHeightCallback = null;
		}

		protected override void OnAfflictionAdd(SerializedProperty property){
			property.FindPropertyRelative("effect.scale").vector3Value = Vector3.one;
		}


		protected override void AfflictionClickHandler(object menuTarget){
			base.AfflictionClickHandler(menuTarget);

			var callbackElements = ((ActorInstantiation)target).AfflictionCallbacks;
			ActorInstantiationAfflictionElement afflictionElement = callbackElements.Last();

			afflictionElement.Enabled = true;
			afflictionElement.EditorShowExpanded = true;

			if(callbackElements.Count > 1 && afflictionElement.Affliction == callbackElements[callbackElements.Count - 2].Affliction){
				afflictionElement = callbackElements[callbackElements.Count - 2];

				afflictionElement.Enabled = true;
				afflictionElement.EditorShowExpanded = true;
			}
		}

		protected override void ItemClickHandler(object menuTarget){
			base.ItemClickHandler(menuTarget);

			ActorInstantiationItemElement callbackElement = ((ActorInstantiation)target).ItemCallbacks.Last();
			callbackElement.Effect.SetTargetAsTransform(null, Vector3.zero, Vector3.zero, Vector3.one, false);
			callbackElement.Enabled = true;
			callbackElement.EditorShowExpanded = true;
		}
		
		public override void OnInspectorGUI(){
			List<string> excludedProperties = new List<string>();
			fullElementHeight = EditorGUIUtility.wideMode ?
				EditorGUIUtility.singleLineHeight * 7 + ActorCallbackElementDrawerBase.lineSpacing * 6 :
				EditorGUIUtility.singleLineHeight * 10 + ActorCallbackElementDrawerBase.lineSpacing * 9;

			if(!serializedObject.FindProperty("allowDefaultAbilityInstantiations").boolValue)
				excludedProperties.Add("abilityCallbackMode");
			if(!serializedObject.FindProperty("allowDefaultItemInstantiations").boolValue)
				excludedProperties.Add("itemCallbackMode");
			if(!serializedObject.FindProperty("allowDefaultAfflictionInstantiations").boolValue)
				excludedProperties.Add("afflictionCallbackMode");

			DrawPropertiesExcluding(serializedObject, hiddenProperties.Concat(excludedProperties).ToArray());
			DrawLists();

			serializedObject.ApplyModifiedProperties();

			ShowEventTargetWarningIfNecessary();
		}
	}

	public class ActorInstantiationElementDrawer : ActorCallbackElementDrawerBase {
		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			curPosition = new Rect(position.x + thirdWidth - dragHandleCorrection, position.y + 2, thirdWidth*2 + dragHandleCorrection, position.height);

			GUI.enabled = spElemEnabled.boolValue;

			curPosition.height = EditorGUIUtility.singleLineHeight;
			curPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			EditorGUI.PropertyField(curPosition, property.FindPropertyRelative("effect"), true);
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorInstantiationAbilityElement))]
	public class ActorInstantiationAbilityElementDrawer : ActorInstantiationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawAbility(position, property, label, "effect");
		}
	}

	[CustomPropertyDrawer(typeof(ActorInstantiationEventElement))]
	public class ActorInstantiationEventElementDrawer : ActorInstantiationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			DrawEvent(position, property, label, "effect");
		}

		protected override void DrawSpecifics(Rect position, SerializedProperty property){
			base.DrawSpecifics(position, property);

			DrawEventIgnoreFlags(property,
				new Rect(position.x + 36, position.y + (EditorGUIUtility.singleLineHeight + 2) * 8 + 2, position.width - 72, EditorGUIUtility.singleLineHeight),
				115);
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorInstantiationAfflictionElement))]
	public class ActorInstantiationAfflictionElementDrawer : ActorInstantiationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawAffliction(position, property, label, "effect");
		}
	}
	
	[CustomPropertyDrawer(typeof(ActorInstantiationItemElement))]
	public class ActorInstantiationItemElementDrawer : ActorInstantiationElementDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			position.y += 2f;
			DrawItem(position, property, label, "effect");
		}
	}
}