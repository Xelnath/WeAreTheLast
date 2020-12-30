using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Editor {
	public abstract class ChainEvaluatorEditor : AresEditor {
		ReorderableList actionList;
		ReorderableList tokenList;
		Dictionary<ChainEvaluator.ActionType, int> numActionTypes;

		string[] afflictionGuids;
		AfflictionData[] afflictionDatas;
		string[] afflictionNames;
		string[] afflictionNamesWithNone;

		string[] statGuids;
		StatData[] statDatas;
		string[] statNames;
		string[] statNamesWithNone;

		string[] environmentVariableGuids;
		EnvironmentVariableData[] environmentVariableDatas;
		string[] environmentVariableNames;
		string[] environmentVariableNamesWithNone;

		string[] actionTypeNames;

		void DrawActionChainValueEvaluator(Rect rect, int xStart, ref int xOffset, string powerLabelText, SerializedProperty spValueType, SerializedProperty spValue1, SerializedProperty spValue2, SerializedProperty spValueFormula){
			DrawField(rect, ref xOffset, 70, spValueType);

			if((ActionChainValueEvaluator.PowerType)spValueType.enumValueIndex == ActionChainValueEvaluator.PowerType.Random){
				powerLabelText += " [";
			}

			DrawLabel(rect, ref xOffset, 37, powerLabelText);

			if((ActionChainValueEvaluator.PowerType)spValueType.enumValueIndex == ActionChainValueEvaluator.PowerType.Formula){
				DrawField(rect, ref xOffset, Mathf.Max((int)rect.width - xOffset - 70, 160 - xStart), spValueFormula);
			}
			else{
				DrawField(rect, ref xOffset, 40, spValue1);
			}

			if((ActionChainValueEvaluator.PowerType)spValueType.enumValueIndex == ActionChainValueEvaluator.PowerType.Random){
				xOffset -= 3;
				DrawLabel(rect, ref xOffset, 7, "-");
				DrawField(rect, ref xOffset, 40, spValue2);
				xOffset -= 6;
				DrawLabel(rect, ref xOffset, 3, "]");
				xOffset += 2;
			}

			if(target.GetType() == typeof(AfflictionData) && (ActionChainValueEvaluator.PowerType)spValueType.enumValueIndex != ActionChainValueEvaluator.PowerType.Formula){
				xOffset -= 4;
				DrawLabel(rect, ref xOffset, 50, "* power");
			}
		}

		void OnEnable() {
			string[] missingPrependArray = new string[]{"[Missing!]"};

			afflictionGuids = AssetDatabase.FindAssets("t:AfflictionData");
			afflictionDatas = afflictionGuids.Select(g => AssetDatabase.LoadAssetAtPath<AfflictionData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
			afflictionNames = afflictionDatas.Select(d => d.DisplayName).OrderBy(n => n).ToArray();
			afflictionNamesWithNone = missingPrependArray.Concat(afflictionNames).ToArray();

			statGuids = AssetDatabase.FindAssets("t:StatData");
			statDatas = statGuids.Select(g => AssetDatabase.LoadAssetAtPath<StatData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
			statNames = statDatas.Select(d => d.DisplayName).OrderBy(n => n).ToArray();
			statNamesWithNone = missingPrependArray.Concat(statNames).ToArray();

			environmentVariableGuids = AssetDatabase.FindAssets("t:EnvironmentVariableData");
			environmentVariableDatas = environmentVariableGuids.Select(g => AssetDatabase.LoadAssetAtPath<EnvironmentVariableData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
			environmentVariableNames = environmentVariableDatas.Select(d => d.DisplayName).OrderBy(n => n).ToArray();
			environmentVariableNamesWithNone = missingPrependArray.Concat(environmentVariableNames).ToArray();

			actionTypeNames = System.Enum.GetNames(typeof(ChainEvaluator.ActionType));

			for(int i = 0; i < actionTypeNames.Length; i++){
				if(actionTypeNames[i] == "Buff"){
						actionTypeNames[i] = "Buff \u2044 Debuff";
					break;
				}
			}

			actionList = new ReorderableList(serializedObject, serializedObject.FindProperty("actions"), true, true, true, true);
			tokenList = new ReorderableList(serializedObject, serializedObject.FindProperty("actionTokens"), true, true, true, true);

			tokenList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Tokens");
			};

			tokenList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 2) + 6;
			};

			tokenList.onAddCallback = (list) => {
				list.serializedProperty.arraySize++;
				SerializedProperty newToken = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
				newToken.FindPropertyRelative("id").stringValue = "TOKEN" + list.serializedProperty.arraySize.ToString();
				newToken.FindPropertyRelative("valueType").enumValueIndex = 0;
				newToken.FindPropertyRelative("value1").floatValue = 0f;
				newToken.FindPropertyRelative("value2").floatValue = 0f;
				newToken.FindPropertyRelative("valueFormula").stringValue = "";
			};

			tokenList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = tokenList.serializedProperty.GetArrayElementAtIndex(index);

				int xStart = 0;
				int xOffset = xStart;

				rect.y += 4;

				DrawField(rect, ref xOffset, rect.width * .2f, element.FindPropertyRelative ("id"));

				DrawActionChainValueEvaluator(rect, xStart, ref xOffset, "Value", element.FindPropertyRelative ("valueType"), element.FindPropertyRelative ("value1"),
											  element.FindPropertyRelative ("value2"), element.FindPropertyRelative ("valueFormula"));
			};



			actionList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Actions");
			};

			actionList.elementHeightCallback = (index) => {
				SerializedProperty element = actionList.serializedProperty.GetArrayElementAtIndex(index);
				ChainEvaluator.ActionType type = (ChainEvaluator.ActionType)element.FindPropertyRelative("action").enumValueIndex;
				ChainableAction.EnvironmentVariableSetType spSetType = (ChainableAction.EnvironmentVariableSetType)element.FindPropertyRelative("environmentVariableSetType").enumValueIndex;

				bool isChildAction = element.FindPropertyRelative("isChildEffect").boolValue;
				int numExtraLines = 0;

				if(!isChildAction || index == 0){
					numExtraLines += 2;
				}

				if((!isChildAction || index == 0)){
					for(int i = index + 1; i < actionList.serializedProperty.arraySize; i++){
						if(!actionList.serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("isChildEffect").boolValue){
							numExtraLines++;
							break;
						}
					}
				}

				if(type == ChainEvaluator.ActionType.Environment && spSetType == ChainableAction.EnvironmentVariableSetType.Unset){
					numExtraLines--;
				}
				else if(type == ChainEvaluator.ActionType.Buff){
					numExtraLines++;
				}

				return (EditorGUIUtility.singleLineHeight + 2) * (2 + numExtraLines) + 6;
			};

			actionList.onAddCallback = (list) => {
				list.serializedProperty.arraySize++;
				SerializedProperty newAction = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
				newAction.FindPropertyRelative("action").enumValueIndex = 0;
				newAction.FindPropertyRelative("powerType").enumValueIndex = 0;
				newAction.FindPropertyRelative("power1").floatValue = 0f;
				newAction.FindPropertyRelative("power2").floatValue = 0f;
				newAction.FindPropertyRelative("powerFormula").stringValue = "";
				newAction.FindPropertyRelative("specialType").enumValueIndex = 0;
				newAction.FindPropertyRelative("special1").floatValue = 0f;
				newAction.FindPropertyRelative("special2").floatValue = 0f;
				newAction.FindPropertyRelative("specialFormula").stringValue = "";
				newAction.FindPropertyRelative("chance").floatValue = 1f;
				newAction.FindPropertyRelative("duration").floatValue = 1f;
				newAction.FindPropertyRelative("normalizedProcessTime").floatValue = 1f;
				newAction.FindPropertyRelative("affliction").objectReferenceValue = afflictionDatas.FirstOrDefault();
				newAction.FindPropertyRelative("stat").objectReferenceValue = statDatas[0];
				newAction.FindPropertyRelative("environmentVariable").objectReferenceValue = environmentVariableDatas.FirstOrDefault();
			};

			actionList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = actionList.serializedProperty.GetArrayElementAtIndex(index);
				SerializedProperty spAction = element.FindPropertyRelative("action");
				SerializedProperty spPowerType = element.FindPropertyRelative("powerType");
				SerializedProperty spSpecialType = element.FindPropertyRelative("specialType");
				SerializedProperty spTargetType = element.FindPropertyRelative("targetType");
				SerializedProperty spSetType = element.FindPropertyRelative("environmentVariableSetType");
				SerializedProperty spChance = element.FindPropertyRelative("chance");
				SerializedProperty spIsChildEffect = element.FindPropertyRelative("isChildEffect");
				SerializedProperty spNPT = element.FindPropertyRelative("normalizedProcessTime");

				int xStart = (spIsChildEffect.boolValue && index > 0) ? 20 : 0;
				int xOffset = xStart;

				if(!spIsChildEffect.boolValue && index > 0){
					Rect r = new Rect(rect.x + xStart - 18, rect.y - 1, rect.width - xStart + 22, 1);
					EditorGUI.DrawRect(r, Color.gray);
				}

				rect.y += 4;

				spAction.enumValueIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, 80, EditorGUIUtility.singleLineHeight), spAction.enumValueIndex, actionTypeNames);
				
				xOffset += (int)80 + propertySpacingHorizontal;

				ChainEvaluator.ActionType type = (ChainEvaluator.ActionType)spAction.enumValueIndex;
				int enumFieldWidth = 66;
				
				if(type == ChainEvaluator.ActionType.Environment){
					DrawField(rect, ref xOffset, 94, spSetType);
				}
				else{
					DrawField(rect, ref xOffset, 94, spTargetType);
				}

				if(type == ChainEvaluator.ActionType.Buff){
					SerializedProperty spStat = element.FindPropertyRelative("stat");
					int enumIndex = 0;

					if(spStat.objectReferenceValue == null){
						GUI.backgroundColor = InvalidEnumColor;
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, statNamesWithNone);
						GUI.backgroundColor = Color.white;

						if(enumIndex > 0){
							spStat.objectReferenceValue = statDatas.First(d => d.DisplayName == statNames[enumIndex - 1]);
						}
					}
					else{
						enumIndex = System.Array.IndexOf(statNames, ((StatData)spStat.objectReferenceValue).DisplayName);
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, statNames);
						spStat.objectReferenceValue = statDatas.First(d => d.DisplayName == statNames[enumIndex]);
					}


					xOffset += enumFieldWidth;
				}
				else if(type == ChainEvaluator.ActionType.Afflict || type == ChainEvaluator.ActionType.Cure){
					SerializedProperty spAffliction = element.FindPropertyRelative("affliction");
					int enumIndex = 0;

					if(spAffliction.objectReferenceValue == null){
						GUI.backgroundColor = InvalidEnumColor;
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, afflictionNamesWithNone);
						GUI.backgroundColor = Color.white;

						if(enumIndex > 0){
							spAffliction.objectReferenceValue = afflictionDatas.First(d => d.DisplayName == afflictionNames[enumIndex - 1]);
						}
					}
					else{
						enumIndex = System.Array.IndexOf(afflictionNames, ((AfflictionData)spAffliction.objectReferenceValue).DisplayName);
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, afflictionNames);
						spAffliction.objectReferenceValue = afflictionDatas.First(d => d.DisplayName == afflictionNames[enumIndex]);
					}
						
					xOffset += enumFieldWidth;
				}
				else if(type == ChainEvaluator.ActionType.Environment){
					SerializedProperty spEnvironmentVariable = element.FindPropertyRelative("environmentVariable");
					int enumIndex = 0;

					if(spEnvironmentVariable.objectReferenceValue == null){
						GUI.backgroundColor = InvalidEnumColor;
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, environmentVariableNamesWithNone);
						GUI.backgroundColor = Color.white;

						if(enumIndex > 0){
							spEnvironmentVariable.objectReferenceValue = environmentVariableDatas.First(d => d.DisplayName == environmentVariableNames[enumIndex - 1]);
						}
					}
					else{
						enumIndex = System.Array.IndexOf(environmentVariableNames, ((EnvironmentVariableData)spEnvironmentVariable.objectReferenceValue).DisplayName);
						enumIndex = EditorGUI.Popup(new Rect(rect.x + xOffset, rect.y, enumFieldWidth, EditorGUIUtility.singleLineHeight), enumIndex, environmentVariableNames);
						spEnvironmentVariable.objectReferenceValue = environmentVariableDatas.First(d => d.DisplayName == environmentVariableNames[enumIndex]);
					}


					xOffset += enumFieldWidth;
				}

				int refLabelX = xOffset + 94;

				// Is Child
				if(index > 0){
					xOffset = Mathf.Max(xOffset + 1, (int)rect.width - 60 - 72 - 65);

					DrawLabel(rect, ref xOffset, 72, "Is child effect");
					DrawField(rect, ref xOffset, 30, spIsChildEffect);
				}

				// Reference Label
				numActionTypes[type]++;
				string refLabel = string.Format("({0}{1})", type.ToString().ToUpper(), numActionTypes[type]);
				Vector2 refLabelSize = GUI.skin.label.CalcSize(new GUIContent(refLabel));

				xOffset = Mathf.Max(refLabelX, (int)rect.width - (int)refLabelSize.x - 6);

				DrawLabel(rect, ref xOffset, 60, refLabel, EditorStyles.boldLabel);

				// Line 2
				if(!(type == ChainEvaluator.ActionType.Environment && (ChainableAction.EnvironmentVariableSetType)spSetType.enumValueIndex == ChainableAction.EnvironmentVariableSetType.Unset)){
					rect.y += EditorGUIUtility.singleLineHeight + 2;
					xOffset = xStart;

					DrawField(rect, ref xOffset, 70, spPowerType);

					string powerLabelText;

					if(type == ChainEvaluator.ActionType.Buff || type == ChainEvaluator.ActionType.Cure){
						powerLabelText = "Stages";
					}
					else if(type == ChainEvaluator.ActionType.Afflict || type == ChainEvaluator.ActionType.Environment){
						powerLabelText = "Stage";
					}
					else{
						powerLabelText = "Power";
					}

					DrawActionChainValueEvaluator(rect, xStart, ref xOffset, powerLabelText, spPowerType, element.FindPropertyRelative ("power1"),
												  element.FindPropertyRelative ("power2"), element.FindPropertyRelative ("powerFormula"));
				}

				if(type == ChainEvaluator.ActionType.Buff){
					rect.y += EditorGUIUtility.singleLineHeight + 2;
					xOffset = xStart;

					DrawField(rect, ref xOffset, 70, spSpecialType);

					int xType = xOffset;
					float ySpecial = rect.y;

					DrawActionChainValueEvaluator(rect, xStart, ref xOffset, "Turns", spSpecialType, element.FindPropertyRelative ("special1"),
												  element.FindPropertyRelative ("special2"), element.FindPropertyRelative ("specialFormula"));

					if((ActionChainValueEvaluator.PowerType)spSpecialType.enumValueIndex == ActionChainValueEvaluator.PowerType.Formula){
						rect.y += EditorGUIUtility.singleLineHeight + 2;
						xOffset = xType + 120;
					}

					Color labelColor = EditorStyles.miniLabel.normal.textColor;
					labelColor.a = .5f;
					EditorStyles.miniLabel.normal.textColor = labelColor;
					DrawLabel(rect, ref xOffset, "(0 or less means the buff is permanent)", EditorStyles.miniLabel);
					labelColor.a = 1f;
					EditorStyles.miniLabel.normal.textColor = labelColor;

					rect.y = ySpecial;
				}

				if(!spIsChildEffect.boolValue || index == 0){
					// Line 3
					rect.y += EditorGUIUtility.singleLineHeight + 2;
					xOffset = xStart;


					// Chance % Slider
					int sliderWidth = Mathf.Min(Mathf.Max((int)rect.width - xOffset - 70 - 46, 35), 60);
					DrawLabel(rect, ref xOffset, 26, "Hit %");

					spChance.floatValue = GUI.HorizontalSlider(new Rect(rect.x + xOffset, rect.y, sliderWidth, EditorGUIUtility.singleLineHeight), spChance.floatValue, 0f, 1f);
					xOffset += sliderWidth + 6;

					DrawField(rect, ref xOffset, 40, spChance);

					// Line 4
					rect.y += EditorGUIUtility.singleLineHeight + 2;
					xOffset = 0;

					EditorGUIUtility.labelWidth = 50 + propertySpacingHorizontal;
					EditorGUIUtility.fieldWidth = 10;
					DrawField(rect, ref xOffset, 100, element.FindPropertyRelative("duration"), "Duration");

					EditorGUIUtility.fieldWidth = 0;
					EditorGUIUtility.labelWidth = 0;

					xOffset += propertySpacingHorizontal + 8;
					DrawLabel(rect, ref xOffset, 72, "Process Time");

					sliderWidth = Mathf.Min(Mathf.Max((int)rect.width - xOffset - 70 - 46, 35), 60);
					spNPT.floatValue = GUI.HorizontalSlider(new Rect(rect.x + xOffset, rect.y, sliderWidth, EditorGUIUtility.singleLineHeight), spNPT.floatValue, 0f, 1f);
					xOffset += sliderWidth + 6;

					DrawField(rect, ref xOffset, 40, spNPT);

					// Line 5
//					if(actionList.serializedProperty.arraySize > index + 1){
						for(int i = index + 1; i < actionList.serializedProperty.arraySize; i++){
							if(!actionList.serializedProperty.GetArrayElementAtIndex(i).FindPropertyRelative("isChildEffect").boolValue){
								rect.y += EditorGUIUtility.singleLineHeight + 2;
								xOffset = xStart;
								
								DrawLabel(rect, ref xOffset, 112, "Breaks chain on miss");
								DrawField(rect, ref xOffset, 24, element.FindPropertyRelative("breaksChainOnMiss"));
								break;
							}
						}
//					}

					// Line 6+ //V2
//					rect.y += EditorGUIUtility.singleLineHeight + 2;
//					xOffset = 0;
//					DrawLabel(rect, ref xOffset, "Effects:", EditorStyles.boldLabel);
//					rect.y += EditorGUIUtility.singleLineHeight + 2;
//					xOffset = 0;
//					EditorGUIUtility.labelWidth = 70;
//
//					var spAnimEffect = element.FindPropertyRelative("animationEffect");
//					float animEffectHeight = EditorGUI.GetPropertyHeight(spAnimEffect);
//
//					EditorGUI.PropertyField(new Rect(rect.x + xOffset, rect.y, rect.width, animEffectHeight), spAnimEffect, GUIContent.none);
//
//					xOffset = 30;
//					DrawLabel(rect, ref xOffset, "Animation");
//
//					rect.y += animEffectHeight;
//					xOffset = 0;
//
//					var spInstEffect = element.FindPropertyRelative("instantiationEffect");
//					float instEffectHeight = EditorGUI.GetPropertyHeight(spInstEffect) + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
//
//					EditorGUI.PropertyField(new Rect(rect.x + xOffset, rect.y, rect.width, instEffectHeight), spInstEffect, GUIContent.none);
//
//					xOffset = 30;
//					DrawLabel(rect, ref xOffset, "Instantiation");
//
//					rect.y += instEffectHeight;
//					xOffset = 0;
//
//					var spAudioEffect = element.FindPropertyRelative("audioEffect");
//					float audioEffectHeight = EditorGUI.GetPropertyHeight(spAnimEffect);
//
//					EditorGUI.PropertyField(new Rect(rect.x + xOffset, rect.y, rect.width, audioEffectHeight), spAudioEffect, GUIContent.none);
//
//					xOffset = 30;
//					DrawLabel(rect, ref xOffset, "Audio");
				}

			};
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			numActionTypes = new Dictionary<ChainEvaluator.ActionType, int>();

			foreach(ChainEvaluator.ActionType type in System.Enum.GetValues(typeof(ChainEvaluator.ActionType))){
				numActionTypes.Add(type, 0);
			}

			DrawPropertyGroup1();

			DrawPreChain();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
			tokenList.DoLayoutList();
			actionList.DoLayoutList();

			DrawHelpBoxIfNeeded();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

			foreach(string prop in GetPropertyGroup2()){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
				EditorGUILayout.Space();
			}

			DrawFinalProperties();
			DrawMiscProperties(GetExcludedProperties().Distinct().ToArray());

			serializedObject.ApplyModifiedProperties();
		}

		protected abstract void DrawHelpBoxIfNeeded();
		protected abstract string[] GetPropertyGroup1();
		protected abstract string[] GetPropertyGroup2();
		protected abstract string[] GetExcludedProperties();

		protected virtual void DrawPreChain(){}
		protected virtual void DrawFinalProperties(){}

		protected virtual void DrawPropertyGroup1(){
			foreach(string prop in GetPropertyGroup1()){
				EditorGUILayout.PropertyField(serializedObject.FindProperty(prop));
			}
		}
	}
}