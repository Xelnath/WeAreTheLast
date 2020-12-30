using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Ares.Editor {
	public class BattleInteractorEditor : ChainEvaluatorEditor {
		readonly string[] propertyGroup1 = {"displayName", "description", "category", "baseDuration", "preparation", "recovery"};
		readonly string[] propertyGroup1End = {"validTargetStates", "validTargetParticipants"};
		readonly string[] propertyGroup1EndWithTurnTowardsTarget = {"validTargetStates", "validTargetParticipants", "turnTowardsTarget"};
		readonly string[] propertyGroup2 = {"animation", "instantiation", "audio"};
		readonly string[] hiddenProperties = {"m_Script", "actionTokens", "actions", "targetType", "validTargets", "validTargetGroups", "validTargetStates", "isTargetable",
			"validTargetParticipants", "turnTowardsTarget", "numberOfTargets"};

		protected override void DrawHelpBoxIfNeeded(){
			//Ugly ifs here, but can't cast downwards to BattleInteractor<ChainableAction>
			if((target.GetType() == typeof(ItemData) && ((BattleInteractorData<ItemAction>)target).Actions.Any(a => a.Action == ChainEvaluator.ActionType.Cure)) ||
				(target.GetType() == typeof(AbilityData) && ((BattleInteractorData<AbilityAction>)target).Actions.Any(a => a.Action == ChainEvaluator.ActionType.Cure))){
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Note that, for curing afflictions, setting the number of stages to -1 will make it cure and remove the affliction completely.",
					MessageType.Info);
			}

			if((target.GetType() == typeof(ItemData) && ((BattleInteractorData<ItemAction>)target).Actions.Any(a => a.PowerMode == ChainableAction.PowerType.Formula)) ||
				(target.GetType() == typeof(AbilityData) && ((BattleInteractorData<AbilityAction>)target).Actions.Any(a => a.PowerMode == ChainableAction.PowerType.Formula))){
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Default formula tokens:\t\tFunctions:\n\nCASTER_HP\tTARGET_HP\tABS()\nCASTER_MAX_HP\tTARGET_MAX_HP\tMIN()\n" +
					"CASTER_ATTACK\tTARGET_ATTACK\tMAX()\nCASTER_DEFENSE\tTARGET_DEFENSE\nCASTER_SPEED\tTARGET_SPEED",
					MessageType.Info);
			}

			if(serializedObject.FindProperty("validTargetStates").enumValueIndex == (int)BattleInteractorData.TargetAliveState.Defeated){
				int errorsFound = 0;

				if(target.GetType() == typeof(AbilityData)){
					AbilityData abilityData = (AbilityData)target;

					foreach(var action in abilityData.Actions){
						ShowErrorIfNeeded(action.Action, action.Affliction, "ability", ref errorsFound);
					}
				}
				else if(target.GetType() == typeof(ItemData)){
					ItemData itemData = (ItemData)target;

					foreach(var action in itemData.Actions){
						ShowErrorIfNeeded(action.Action, action.Affliction, "item", ref errorsFound);
					}
				}

			}
		}

		void ShowErrorIfNeeded(ChainEvaluator.ActionType actionType, AfflictionData afflictionData, string interactorTypeString, ref int errorsFound){
			if(actionType == ChainEvaluator.ActionType.Afflict && afflictionData != null && !afflictionData.CanAfflictDefeatedActors){
				errorsFound++;

				if(errorsFound == 1){
					EditorGUILayout.Space();
				}

				EditorGUILayout.HelpBox(string.Format("Warning: This {0} is targeting defeated actors, but {1} can not afflict these.\n" +
					"To fix this, go to the {1} asset and make sure \"Can Afflict Defeated Actors\" is checked.", interactorTypeString, afflictionData.DisplayName),
					MessageType.Error);
			}
		}

		protected override string[] GetPropertyGroup1(){
			return propertyGroup1;//.Concat(GetTargetProps()).ToArray();
		}

		string[] GetTargetProps(){
			SerializedProperty spTargetType = serializedObject.FindProperty("targetType");

			string[] targetProps = null;

			switch((BattleInteractorData.TargetType)spTargetType.enumValueIndex){
				case BattleInteractorData.TargetType.SingleActor:		targetProps = new string[]{"targetType", "validTargets"};						break;
				case BattleInteractorData.TargetType.NumberOfActors:	targetProps = new string[]{"targetType", "numberOfTargets", "validTargets"};	break;
				case BattleInteractorData.TargetType.AllActorsInGroup:	targetProps = new string[]{"targetType", "validTargetGroups"};					break;
				case BattleInteractorData.TargetType.AllActors:			targetProps = new string[]{"targetType", "validTargetGroups"};					break;
			}

			BattleInteractorData.TargetType targetType = (BattleInteractorData.TargetType)spTargetType.enumValueIndex;
			bool validTargetsIsSelf = (BattleInteractorData.TargetGroupActors)serializedObject.FindProperty("validTargets").enumValueIndex == BattleInteractorData.TargetGroupActors.Self;

			if(((targetType == BattleInteractorData.TargetType.SingleActor || targetType == BattleInteractorData.TargetType.NumberOfActors) && !validTargetsIsSelf) ||
				(targetType == BattleInteractorData.TargetType.AllActorsInGroup &&
					(BattleInteractorData.TargetGroupGroups)serializedObject.FindProperty("validTargetGroups").enumValueIndex != BattleInteractorData.TargetGroupGroups.Allies)){
				System.Array.Resize(ref targetProps, targetProps.Length + 1);
				targetProps[targetProps.Length - 1] = "isTargetable";
			}

			if((targetType == BattleInteractorData.TargetType.SingleActor || targetType == BattleInteractorData.TargetType.NumberOfActors) && validTargetsIsSelf){
				return targetProps.Concat(propertyGroup1End).ToArray();
			}
			else{
				return targetProps.Concat(propertyGroup1EndWithTurnTowardsTarget).ToArray();
			}
		}

		protected override void DrawPropertyGroup1(){
			foreach(string prop in GetPropertyGroup1()) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}

			if(serializedObject.FindProperty("preparation.turns").intValue > 0 || serializedObject.FindProperty("recovery.turns").intValue > 0){
				EditorGUILayout.HelpBox("Supported preparation/recovery tokens:\n\nACTOR_NAME\n\nNote: Message fields do not support any math operations.", MessageType.Info);
			}

			foreach(string prop in GetTargetProps()){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}
		}

		protected override string[] GetPropertyGroup2(){
			return propertyGroup2;
		}

		protected override string[] GetExcludedProperties(){
			return GetPropertyGroup1().Concat(propertyGroup2.Concat(hiddenProperties)).ToArray();
		}

		protected override void DrawFinalProperties(){
			SerializedProperty spPreparation = serializedObject.FindProperty("preparation");
			SerializedProperty spAnimations = spPreparation.FindPropertyRelative("animations");
			SerializedProperty spAudios = spPreparation.FindPropertyRelative("audios");
			SerializedProperty spInstantiations = spPreparation.FindPropertyRelative("instantiations");

			int numTurns = spPreparation.FindPropertyRelative("turns").intValue;

			for(int i = 0; i < numTurns; i++){
				string numberSuffix = numTurns > 1 ? (" "+(i+1).ToString()) : "";

				EditorGUILayout.PropertyField(spAnimations.GetArrayElementAtIndex(i), new GUIContent("Prepation Animation" + numberSuffix));
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(spInstantiations.GetArrayElementAtIndex(i), new GUIContent("Prepation Instantiation" + numberSuffix));
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(spAudios.GetArrayElementAtIndex(i), new GUIContent("Prepation Audio" + numberSuffix));
				EditorGUILayout.Space();
			}
		}
	}
}