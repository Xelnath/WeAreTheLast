using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(EnvironmentVariableData), true)]
	public class EnvironmentVariableDataEditor : AresEditor {
		readonly string[] effectProperties = {"setInstantiation", "setAudio", "processInstantiation", "processAudio", "filterInstantiation", "filterAudio",
											  "unsetInstantiation", "unsetAudio"};
		readonly string[] hiddenProperties = {"m_Script", "displayName", "doubleSetStageBehaviour", "doubleSetDurationBehaviour",
											  "durationProcessingMoment", "effectProcessingMoment", "minStage", "maxStage", "durationMode", "minDuration",
											  "maxDuration", "filters"};

		float EditorWidth {get{return EditorGUIUtility.currentViewWidth - 45;}}

		ReorderableList filterList;
		ChainEvaluator.ActionType[] filterTypes;

		bool ShouldSplitFilterTypeAndTarget(float width){
			return width < 320;
		}

		bool ShouldSplitAffectors(float width){
			return width < 340;
		}

		void OnEnable(){
			filterTypes = (ChainEvaluator.ActionType[])System.Enum.GetValues(typeof(ChainEvaluator.ActionType));
			filterList = new ReorderableList(serializedObject, serializedObject.FindProperty("filters"), true, true, true, true);

			filterList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Filters");
			};

			filterList.elementHeightCallback = (index) => {
				int numLines = 4;

				if(ShouldSplitAffectors(EditorWidth)){
					numLines++;
				}

				if(ShouldSplitFilterTypeAndTarget(EditorWidth)){
					numLines++;
				}

				return ((EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * numLines + 5);
			};

			filterList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				int xOffset = 0;

				SerializedProperty element = filterList.serializedProperty.GetArrayElementAtIndex(index);

				rect.y += 2;

				if(ShouldSplitFilterTypeAndTarget(EditorWidth)){
					DrawField(rect, ref xOffset, rect.width, element.FindPropertyRelative("type"), "Filter Actions Of Type");
					
					xOffset = 0;
					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					DrawField(rect, ref xOffset, rect.width, element.FindPropertyRelative("target"), "Filter Actions For");
				}
				else{
					float fieldsWidth = (rect.width - EditorGUIUtility.labelWidth - EditorStyles.label.CalcSize(new GUIContent(" for ")).x) / 2;
					DrawLabel(rect, ref xOffset, "Filter Actions Of Type");
					xOffset = Mathf.RoundToInt(EditorGUIUtility.labelWidth);
					DrawField(rect, ref xOffset, fieldsWidth, element.FindPropertyRelative("type"));
					xOffset -= propertySpacingHorizontal;
					DrawLabel(rect, ref xOffset, " for ");
					xOffset -= propertySpacingHorizontal;
					DrawField(rect, ref xOffset, fieldsWidth, element.FindPropertyRelative("target"));
				}

				xOffset = 0;
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				DrawField(rect, ref xOffset, rect.width, element.FindPropertyRelative("blockCondition"), "Block Action Use");

				xOffset = 0;
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				DrawField(rect, ref xOffset, rect.width, element.FindPropertyRelative("modificationFormula"), "Modification Formula");

				xOffset = 0;
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				DrawField(rect, ref xOffset, 12, element.FindPropertyRelative("affectsAbilities"));
				xOffset -= 3;
				DrawLabel(rect, ref xOffset, "Affects Abilities");
				xOffset += 3;

				DrawField(rect, ref xOffset, 12, element.FindPropertyRelative("affectsItems"));
				xOffset -= 3;
				DrawLabel(rect, ref xOffset, "Affects Items");
				xOffset += 3;

				if(ShouldSplitAffectors(EditorWidth)){
					xOffset = 0;
					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}

				DrawField(rect, ref xOffset, 12, element.FindPropertyRelative("affectsAfflictions"));
				xOffset -= 3;
				DrawLabel(rect, ref xOffset, "Affects Afflictions");
				xOffset += 3;
			};

			filterList.onAddCallback = (ReorderableList list) =>{
				GenericMenu menu = new GenericMenu();

				foreach(ChainEvaluator.ActionType filterType in filterTypes){
					menu.AddItem(new GUIContent(filterType.ToString()), false, i => {FilterAddHandler(filterType);}, null);
				}

				menu.ShowAsContext();
			};

			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		void FilterAddHandler(ChainEvaluator.ActionType filterType){
			filterList.serializedProperty.arraySize++;

			SerializedProperty element = filterList.serializedProperty.GetArrayElementAtIndex(filterList.serializedProperty.arraySize - 1);
			element.FindPropertyRelative("type").enumValueIndex = ArrayUtility.IndexOf(filterTypes, filterType);

			if(filterList.serializedProperty.arraySize == 1){
				element.FindPropertyRelative("modificationFormula").stringValue = "POWER * 0.5";
			}

			serializedObject.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			SerializedProperty spDurationType = serializedObject.FindProperty("durationMode");

			EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("effectProcessingMoment"));
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Double Set Behaviour", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("doubleSetStageBehaviour"), new GUIContent("Stage"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("doubleSetDurationBehaviour"), new GUIContent("Duration"));
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("Duration", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(spDurationType, new GUIContent("Mode"));

			switch((EnvironmentVariableData.DurationType)spDurationType.enumValueIndex){
				case EnvironmentVariableData.DurationType.ConstantNumberOfTurns:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("durationProcessingMoment"), new GUIContent("Turn Decrease Moment"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minDuration"), new GUIContent("Duration"));
					break;
				case EnvironmentVariableData.DurationType.RandomNumberOfTurns:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("durationProcessingMoment"), new GUIContent("Turn Decrease Moment"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minDuration"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDuration"));
					break;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Stages", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("minStage"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStage"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
			filterList.DoLayoutList();

			if(filterList.count > 0){
				EditorGUILayout.HelpBox("Default formula tokens:\tFunctions:\nPOWER\t\t\tABS()\n" +
					"\t\t\tMIN()\nMacros:\t\t\tMAX()\n" +
					"#D# (dice, i.e. 1D6)",
					MessageType.Info);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

			foreach(string spEffect in effectProperties){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(spEffect));
				EditorGUILayout.Space();
			}

			DrawMiscProperties(effectProperties.Concat(hiddenProperties).ToArray());

			serializedObject.ApplyModifiedProperties();
		}
	}
}