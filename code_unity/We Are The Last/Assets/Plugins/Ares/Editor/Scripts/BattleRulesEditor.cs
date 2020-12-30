using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace Ares.Editor {
	[CustomEditor(typeof(BattleRules))]
	public class BattleRulesEditor : AresEditor {
		ReorderableList timedProcessesOrderList;

		void OnEnable(){
			serializedObject.Update();

			//Make sure all values are present and valid
			SerializedProperty spTimedProcessseOrder = serializedObject.FindProperty("timedProcessesOrder");

			int numMomentProcessingElements = System.Enum.GetValues(typeof(BattleRules.TimedProcess)).Length;

			if(spTimedProcessseOrder.arraySize > numMomentProcessingElements){
				for(int i = spTimedProcessseOrder.arraySize - 1; i >= 0; i--){
					if(spTimedProcessseOrder.GetArrayElementAtIndex(i).enumValueIndex > numMomentProcessingElements - 1){
						for(int j = i; j < spTimedProcessseOrder.arraySize - 1; j++){
							spTimedProcessseOrder.GetArrayElementAtIndex(j).enumValueIndex = spTimedProcessseOrder.GetArrayElementAtIndex(j + 1).enumValueIndex;
						}

						spTimedProcessseOrder.arraySize--;
					}
				}
			}
			else if(spTimedProcessseOrder.arraySize < numMomentProcessingElements){
				List<int> enumIndicesToAdd = new List<int>(numMomentProcessingElements);

				for(int i = 0; i < numMomentProcessingElements; i++){
					enumIndicesToAdd.Add(i);
				}

				for(int i = 0; i < spTimedProcessseOrder.arraySize; i++){
					enumIndicesToAdd.Remove(spTimedProcessseOrder.GetArrayElementAtIndex(i).enumValueIndex);
				}

				foreach(int index in enumIndicesToAdd){
					spTimedProcessseOrder.arraySize++;
					spTimedProcessseOrder.GetArrayElementAtIndex(spTimedProcessseOrder.arraySize - 1).enumValueIndex = index;
				}
			}

			//Set up the list
			timedProcessesOrderList = new ReorderableList(serializedObject, spTimedProcessseOrder, true, true, false, false);

			timedProcessesOrderList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Processing order on timing conflict");
			};

			timedProcessesOrderList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 4);
			};

			timedProcessesOrderList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = timedProcessesOrderList.serializedProperty.GetArrayElementAtIndex(index);

				int xOffset = 0;
				rect.y += 2;

				DrawLabel(rect, ref xOffset, ObjectNames.NicifyVariableName(element.enumNames[element.enumValueIndex]) + "s");
			};

			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		public override void OnInspectorGUI(){
			bool showDefaultAbility = false;

			serializedObject.Update();

			SerializedProperty spProgressAutomatically = serializedObject.FindProperty("progressAutomatically");
			SerializedProperty spCanCastWithNoValidTargets = serializedObject.FindProperty("canCastAbilitiesWithNoValidTargets");
			SerializedProperty spCanUseItemsWithNoValidTargets = serializedObject.FindProperty("canUseItemsWithNoValidTargets");
			SerializedProperty spNoValidActionsAction = serializedObject.FindProperty("noValidActionsAction");
			SerializedProperty spSelectActorAction = serializedObject.FindProperty("selectActorAction");
			SerializedProperty spActorCanFaceTarget = serializedObject.FindProperty("actorCanFaceTarget");

			EditorGUIUtility.labelWidth = Mathf.Min(260, EditorGUIUtility.currentViewWidth - 130);

			EditorGUILayout.LabelField("Progression", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxRounds"));
			EditorGUILayout.PropertyField(spProgressAutomatically);

			if(spProgressAutomatically.boolValue){
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("initialBattleProgressDelay"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenMoves"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenRounds"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenAfflictionEffects"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenAfflictionDurationAdjustments"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenEnvironmentVariableCallbacks"), new GUIContent("Time Between Env. Var. Callbacks"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("timeBetweenEnvironmentVariableDurationAdjustments"), new GUIContent("Time Between Env. Var. Duration Adjustments"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("turnTimeout"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("waitForUIEvents"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("waitForAnimationEvents"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("waitForAbilityEvents"));
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Targeting", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSelectSingleOptionTargetForAbility"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSelectNoChoiceMultiTargetsForAbility"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSelectSingleOptionTargetForItem"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSelectNoChoiceMultiTargetsForItem"));
			
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(spCanCastWithNoValidTargets, new GUIContent("Can Select Ability Without Valid Target"));
			EditorGUILayout.PropertyField(spCanUseItemsWithNoValidTargets, new GUIContent("Can Select Item Without Valid Target"));
			
			EditorGUILayout.PropertyField(spNoValidActionsAction);
			
			if((BattleRules.AbilityFallbackType)spNoValidActionsAction.enumValueIndex == BattleRules.AbilityFallbackType.CastDefaultAbility){
				showDefaultAbility = true;
			}
			
			EditorGUILayout.PropertyField(spSelectActorAction, new GUIContent("Action Select Moment"));

			if((BattleRules.SelectActionMoment)spSelectActorAction.enumValueIndex == BattleRules.SelectActionMoment.OnRoundStart){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("itemComsumptionMoment"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("invalidAbilityTargetAction"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("invalidItemTargetAction"));
			}
			
			if(serializedObject.FindProperty("turnTimeout").floatValue > 0f){
				SerializedProperty spTurnTimeoutAction = serializedObject.FindProperty("turnTimeoutAction");
				EditorGUILayout.PropertyField(spTurnTimeoutAction);

				if((BattleRules.ActionFallbackType)spTurnTimeoutAction.enumValueIndex == BattleRules.ActionFallbackType.CastDefaultAbility){
					showDefaultAbility = true;
				}
			}

			if(showDefaultAbility){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultAbility"));
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Actors", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("actorSort"));
			EditorGUILayout.PropertyField(spActorCanFaceTarget);

			if(spActorCanFaceTarget.enumValueIndex != (int)BattleRules.ActorFacingMoment.Never){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("faceTargetSpeed"));
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Post-battle Cleanup", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("cleanupProperties"), new GUIContent("Destroy"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("cleanupDelay"), new GUIContent("Destruction Delay"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Effects Processing", EditorStyles.boldLabel);

			EditorGUILayout.Space();
			timedProcessesOrderList.DoLayoutList();

			serializedObject.ApplyModifiedProperties();
		}
	}
}