using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(AfflictionData), true)]
	public class AfflictionEditor : ChainEvaluatorEditor {
		readonly string[] propertyGroup1 = {"displayName", "description", "baseDuration", "canAfflictDefeatedActors"};
		readonly string[] propertyGroup2 = {"obtainAnimation", "obtainInstantiation", "obtainAudio", "triggerAnimation", "triggerInstantiation", "triggerAudio",
											"stageIncreaseAnimation", "stageIncreaseInstantiation", "stageIncreaseAudio",
											"stageDecreaseAnimation", "stageDecreaseInstantiation", "stageDecreaseAudio",
											"endAnimation", "endInstantiation", "endAudio"};
		readonly string[] hiddenProperties = {"m_Script", "actions", "powerScalingMode", "power", "powerMultiplier", "powerIncrement", "powerFormula",
											  "minStage", "maxStage", "cureCondition", "duration1", "duration2", "cureChance", "effectProcessingMoment",
											  "durationProcessingMoment", "doubleSetStageBehaviour", "doubleSetDurationBehaviour", "cureOnAfflictedDeath"};


		protected override void DrawPreChain(){
			AfflictionData afflictionData = target as AfflictionData;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Power", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Stages", GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 10));
			EditorGUIUtility.labelWidth = 30;
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minStage"), new GUIContent("Min"), GUILayout.MinWidth(30f), GUILayout.ExpandWidth(false));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStage"), new GUIContent("Max"), GUILayout.MinWidth(30f), GUILayout.ExpandWidth(false));
			EditorGUIUtility.labelWidth = 0;
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.PropertyField(serializedObject.FindProperty("powerScalingMode"));

			if(afflictionData.PowerScalingMode == PowerScaling.Linear || afflictionData.PowerScalingMode == PowerScaling.ExponentialSimple ||
			   afflictionData.PowerScalingMode == PowerScaling.ExponentialSimpleSymmetric){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("power"), new GUIContent("Base Power"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerIncrement"));
			}
			else if(afflictionData.PowerScalingMode == PowerScaling.ExponentialComplex || afflictionData.PowerScalingMode == PowerScaling.ExponentialComplexSymmetric){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("power"), new GUIContent("Base Power"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerIncrement"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerMultiplier"));
			}
			else{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("powerFormula"));

				EditorGUILayout.HelpBox("Default formula tokens:\tFunctions:\nSTAGE\t\t\tABS()\n" +
					"\t\t\tMIN()\n\t\t\tMAX()\n\n" +
					"Macros:\n#D# (dice, i.e. 1D6)",
					MessageType.Info);
			}

			DrawPowerHelpBox(afflictionData.Power, (PowerData)afflictionData);

			EditorGUILayout.PropertyField(serializedObject.FindProperty("cureCondition"));

			switch(afflictionData.CureCondition){
				case AfflictionData.Cure.ConstantNumberOfTurns:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("duration1"), new GUIContent("Duration"));
					break;
				case AfflictionData.Cure.RandomNumberOfTurns:
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Duration", GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 10));
					EditorGUIUtility.labelWidth = 30;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("duration1"), new GUIContent("Min"), GUILayout.ExpandWidth(false));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("duration2"), new GUIContent("Max"), GUILayout.ExpandWidth(false));
					EditorGUIUtility.labelWidth = 0;
					EditorGUILayout.EndHorizontal();
					break;
				case AfflictionData.Cure.RandomChance:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("cureChance"), new GUIContent("Chance"));
					break;
			}

			EditorGUILayout.PropertyField(serializedObject.FindProperty("cureOnAfflictedDeath"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("cureOnAfflicterDeath"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Processing Moments", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("effectProcessingMoment"), new GUIContent("Effect"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("durationProcessingMoment"), new GUIContent("Duration"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Double Set Behaviour", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("doubleSetStageBehaviour"), new GUIContent("Stage"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("doubleSetDurationBehaviour"), new GUIContent("Duration"));
		}

		protected override void DrawHelpBoxIfNeeded(){
			if(((AfflictionData)target).Actions.Any(a => a.Action == ChainEvaluator.ActionType.Cure)){
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Note that, for curing afflictions, setting the number of stages to -1 will make it cure and remove the affliction completely.",
					MessageType.Info);
			}

			if(((AfflictionData)target).Actions.Any(a => a.PowerMode != ChainableAction.PowerType.Formula)){
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Note that, for afflictions, constant and random power values act as multipliers to the power value calculated by the " +
					"base power and current stage.",
					MessageType.Info);
			}

			if(((AfflictionData)target).Actions.Any(a => a.PowerMode == ChainableAction.PowerType.Formula)){
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Default formula tokens:\t\t\t\tFunctions:\n\nAFFLICTED_HP\tAFFLICTER_HP\tBASE_POWER\tABS()\n" +
					"AFFLICTED_MAX_HP\tAFFLICTER_MAX_HP\tCURRENT_POWER\tMIN()\nAFFLICTED_ATTACK\tAFFLICTER_ATTACK\t\t\tMAX()\n" +
					"AFFLICTED_DEFENSE\tAFFLICTER_DEFENSE\nAFFLICTED_SPEED\tAFFLICTER_SPEED\n\nMacros:\n#D# (dice, i.e. 1D6)",
					MessageType.Info);
			}
		}

		protected override string[] GetPropertyGroup1(){
			return propertyGroup1;
		}

		protected override string[] GetPropertyGroup2(){
			return propertyGroup2;
		}

		protected override string[] GetExcludedProperties(){
			return propertyGroup1.Concat(GetPropertyGroup2().Concat(hiddenProperties)).ToArray();
		}
	}
}