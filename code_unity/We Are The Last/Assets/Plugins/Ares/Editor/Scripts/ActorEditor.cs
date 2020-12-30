using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomEditor(typeof(Actor), true)]
	public class ActorEditor : AresEditor {
		readonly string[] propertyGroup1 = {"displayName", "hp", "maxHP"};
		readonly string[] propertyGroup2 = {};
		readonly string[] hiddenProperties = {"m_Script", "stats", "abilities", "fallbackAbility"};
//		readonly Color fallbackFieldBackground = new Color(.894f, .894f, .894f, 1f);
//		readonly Color fallbackFieldBackgroundPro = new Color(.3f, .3f, .3f, 1f);

		ReorderableList abilityList;
		int lastSelectedIndex;

		ReorderableList afflictionsList;

		private void OnEnable(){
			PropertyInfo displayNamePropertyAbilityData = typeof(AbilityData).GetProperty("DisplayName");

			abilityList = new ReorderableList(serializedObject, serializedObject.FindProperty("abilities"), true, true, true, true);

			abilityList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Actions");
			};

			abilityList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 5);
			};

			abilityList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				int xOffset = 0;

				SerializedProperty element = abilityList.serializedProperty.GetArrayElementAtIndex(index);
				SerializedProperty spEnabled = element.FindPropertyRelative("enabled");
				SerializedProperty spAbility = element.FindPropertyRelative("data");
				SerializedProperty spOverrideDuration = element.FindPropertyRelative("overrideBaseDuration");
				SerializedProperty spDuration = element.FindPropertyRelative("baseDuration");

				rect.y += 2;
				DrawField(rect, ref xOffset, 12, spEnabled);
				DrawField(rect, ref xOffset, (int)Mathf.Min(Mathf.Max(100, rect.width - 298), 250), spAbility);
				DrawLabel(rect, ref xOffset, 142, "Use custom base duration");
				DrawField(rect, ref xOffset, 22, spOverrideDuration);

				GUI.enabled = spOverrideDuration.boolValue;
				DrawLabel(rect, ref xOffset, 50, "Duration");
				DrawField(rect, ref xOffset, 40, spDuration);
				GUI.enabled = true;
			};

			abilityList.onSelectCallback = (ReorderableList list) => {
				lastSelectedIndex = list.index;
			};

			abilityList.onReorderCallback = (ReorderableList list) => {
				ActorComponent[] callbackComponents = ((Actor)target).GetComponents<ActorComponent>();

				foreach(ActorComponent callbackComponent in callbackComponents){
					callbackComponent.OnAbilityMoved(lastSelectedIndex, list.index);
				}
			};

			abilityList.onAddCallback = (ReorderableList list) => {
				if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ABILITY_OPTIONS, true)){
					GenericMenu menu = new GenericMenu();

					PopulateGenericMenu<AbilityData>(menu, AbilityAddHandler, o => ((AbilityData)o).FullCategoryPath + "/" + displayNamePropertyAbilityData.GetValue(o, null));
					menu.ShowAsContext();
				}
				else{
					AbilityAddHandler(null);
				}
			};

			abilityList.onRemoveCallback = (ReorderableList list) => {
				list.serializedProperty.DeleteArrayElementAtIndex(lastSelectedIndex);

				ActorComponent[] callbackComponents = ((Actor)target).GetComponents<ActorComponent>();

				foreach(ActorComponent callbackComponent in callbackComponents){
					callbackComponent.OnAbilityRemoved(lastSelectedIndex);
				}
			};

			Actor actor = (Actor)target;

			afflictionsList = new ReorderableList(null, typeof(Affliction), false, true, false, false);

			afflictionsList.drawHeaderCallback = (Rect rect) => {  
				int oneFourth = (int)(rect.width * .25f);

				rect.y += 2;

				EditorGUI.LabelField(rect, "Affliction:");
				rect.x = oneFourth * 1.45f;
				EditorGUI.LabelField(rect, "Stage:");
				rect.x = oneFourth * 2.2f;
				EditorGUI.LabelField(rect, rect.width > 410 ? "Rounds Remaining:" : "Remaining:");
				rect.x = oneFourth * 3.3f;
				EditorGUI.LabelField(rect, "Afflicted by:");
			};

			afflictionsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			};

			afflictionsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>{
				Affliction affliction = (Affliction)afflictionsList.list[index];
				int oneFourth = (int)(rect.width * .25f);

				rect.y += 2;
				rect.width = oneFourth * 1.1f;

				EditorGUI.LabelField(rect, affliction.Data.DisplayName);
				rect.x = oneFourth * 1.45f;
				EditorGUI.LabelField(rect, affliction.Stage.ToString());
				rect.x = oneFourth * 2.2f;

				if(affliction.Data.CureCondition == AfflictionData.Cure.RandomChance){
					EditorGUI.LabelField(rect, string.Format("0 ({0}%)", affliction.Data.CureChance * 100f));
				}
				else{
					EditorGUI.LabelField(rect, affliction.RoundsRemaining.ToString());
				}

				rect.x = oneFourth * 3.3f;
				rect.width = oneFourth;
				EditorGUI.LabelField(rect, affliction.Afflicter.DisplayName);
			};

			actor.SetupStats(true);

			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		void AbilityAddHandler(object abilityData){
			abilityList.serializedProperty.arraySize++;

			if(abilityData != null){
				SerializedProperty element = abilityList.serializedProperty.GetArrayElementAtIndex(abilityList.serializedProperty.arraySize - 1);
				element.FindPropertyRelative("data").objectReferenceValue = (AbilityData)abilityData;
				element.FindPropertyRelative("enabled").boolValue = true;
				element.FindPropertyRelative("overrideBaseDuration").boolValue = false;
				element.FindPropertyRelative("baseDuration").floatValue = 0f;
			}

			ActorComponent[] callbackComponents = ((Actor)target).GetComponents<ActorComponent>();

			foreach(ActorComponent callbackComponent in callbackComponents){
				callbackComponent.OnAbilityAdded();
			}

			serializedObject.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			Rect rect;
			int xOffset;

			EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
			foreach(string prop in propertyGroup1){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
			SerializedProperty spStats = serializedObject.FindProperty("stats");

			for(int i = 0; i < spStats.arraySize; i++){
				SerializedProperty spStat = spStats.GetArrayElementAtIndex(i);
				SerializedProperty spBaseValue = spStat.FindPropertyRelative("baseValue");

				StatData statData = (StatData)spStat.FindPropertyRelative("data").objectReferenceValue;
				string statLabel = statData.DisplayName;

				if(EditorApplication.isPlaying){
					int stage = spStat.FindPropertyRelative("stage").intValue;
					statLabel += string.Format(" ({0}{1}; {2})", stage > -1 ? "+" : ".", stage, Mathf.RoundToInt(statData.GetValue(spBaseValue.intValue, stage)));
				}

				EditorGUILayout.PropertyField(spBaseValue, new GUIContent(statLabel));
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
			abilityList.DoLayoutList();

			EditorGUILayout.LabelField("Fallback Ability", EditorStyles.boldLabel);
			SerializedProperty element = serializedObject.FindProperty("fallbackAbility");
			SerializedProperty spAbility = element.FindPropertyRelative("data");
			SerializedProperty spOverrideDuration = element.FindPropertyRelative("overrideBaseDuration");
			SerializedProperty spDuration = element.FindPropertyRelative("baseDuration");

			rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 5, GUI.skin.box, GUILayout.ExpandWidth(true));
			EditorGUI.DrawRect(rect, AresEditor.FallbackFieldBackgroundColor);

			rect.width -= 64;

			xOffset = 39;
			rect.y += 2;

			DrawField(rect, ref xOffset, (int)Mathf.Min(Mathf.Max(100, rect.width - 259), 250), spAbility);
			DrawLabel(rect, ref xOffset, 142, "Use custom base duration");
			DrawField(rect, ref xOffset, 22, spOverrideDuration);

			rect.width += 64;
			GUI.enabled = spOverrideDuration.boolValue;
			DrawLabel(rect, ref xOffset, 50, "Duration");
			rect.width -= 64;
			DrawField(rect, ref xOffset, 40, spDuration);
			GUI.enabled = true;

			foreach(string prop in propertyGroup2){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}

			DrawMiscProperties(propertyGroup1.Concat(propertyGroup2.Concat(hiddenProperties)).ToArray());
			
			serializedObject.ApplyModifiedProperties();

			Actor actor = (Actor)target;
			ActorAnimation actorAnimation = actor.GetComponent<ActorAnimation>();
			ActorInstantiation actorInstantiation = actor.GetComponent<ActorInstantiation>();
			ActorAudio actorAudio = actor.GetComponent<ActorAudio>();

			if(EditorApplication.isPlaying){
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Afflictions", EditorStyles.boldLabel);

				afflictionsList.list = actor.Afflictions.ToList();
				afflictionsList.DoLayoutList();
			}

			if(actorAnimation == null || actorAudio == null || actorInstantiation == null){
				if(!EditorApplication.isPlaying){
					EditorGUILayout.Space();
				}

				EditorGUILayout.LabelField("Notice", EditorStyles.boldLabel);

				if(actorAnimation == null){
					ShowActorComponentWarning<ActorAnimation>(actor.gameObject, "(Default animations will still play.)");
				}

				if(actorInstantiation == null){
					ShowActorComponentWarning<ActorInstantiation>(actor.gameObject, "(Default instantiation effects will still spawn.)");
				}

				if(actorAudio == null){
					ShowActorComponentWarning<ActorAudio>(actor.gameObject, "(Default audio effects will still play.)");
				}
			}
		}

		void ShowActorComponentWarning<T>(GameObject gameObject, string warning) where T : ActorComponent{
			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2, GUI.skin.box, GUILayout.ExpandWidth(true));

			EditorGUI.HelpBox(rect, string.Format("No {0} component found.\n{1}", ObjectNames.NicifyVariableName(typeof(T).Name), warning), MessageType.Info);

			rect.x = rect.width - 40;
			rect.y += rect.height * .125f;
			rect.width = 50;
			rect.height *= .75f;

			if(GUI.Button(rect, "Add")){
				Undo.AddComponent<T>(gameObject);
			}
		}
	}
}