using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace Ares.Editor {
	public class AresEditor : UnityEditor.Editor {
		public static Color InvalidEnumColor {get{return invalidEnumColor;}}
		public static Color FallbackFieldBackgroundColor {get{return EditorGUIUtility.isProSkin ? fallbackFieldBackgroundPro : fallbackFieldBackground;}}

		static Color invalidEnumColor = new Color(1f, .8f, .8f, 1f);
		static Color fallbackFieldBackground = new Color(.894f, .894f, .894f, 1f);
		static Color fallbackFieldBackgroundPro = new Color(.3f, .3f, .3f, 1f);

		protected readonly int propertySpacingHorizontal = 6;
		string lastPowerFormulaEvaluated;
		int lastMinStage, lastMaxStage;
		string cachedPowerFormulaMessage;

		protected void DrawField(Rect rect, ref int xOffset, float width, SerializedProperty property, string label=""){
			EditorGUI.PropertyField(new Rect(rect.x + xOffset, rect.y, width, EditorGUIUtility.singleLineHeight), property, label == "" ? GUIContent.none : new GUIContent(label));
			xOffset += (int)width + propertySpacingHorizontal;
		}

		protected void DrawAssetObjectField<T>(Rect rect, ref int xOffset, float width, SerializedProperty property){
			property.objectReferenceValue = EditorGUI.ObjectField(new Rect(rect.x + xOffset, rect.y, width, EditorGUIUtility.singleLineHeight), property.objectReferenceValue, typeof(T), false);
			xOffset += (int)width + propertySpacingHorizontal;
		}

		protected void DrawLabel(Rect rect, ref int xOffset, int width, string label, GUIStyle style = null){
			EditorGUI.LabelField(new Rect(rect.x + xOffset, rect.y, rect.width - xOffset, EditorGUIUtility.singleLineHeight), label, style == null ? EditorStyles.label : style);
			xOffset += width + propertySpacingHorizontal;
		}

		protected void DrawLabel(Rect rect, ref int xOffset, string label, GUIStyle style = null){
			GUIContent labelContent = new GUIContent(label);
			EditorGUI.LabelField(new Rect(rect.x + xOffset, rect.y, rect.width - xOffset, EditorGUIUtility.singleLineHeight), labelContent, style == null ? EditorStyles.label : style);
			xOffset += Mathf.RoundToInt(EditorStyles.label.CalcSize(labelContent).x) + propertySpacingHorizontal;
		}

		protected void DrawLabelMultiline(Rect rect, ref int xOffset, string label, GUIStyle style = null){
			GUIContent labelContent = new GUIContent(label);
			EditorGUI.LabelField(new Rect(rect.x + xOffset, rect.y, rect.width - xOffset, rect.height), labelContent, style == null ? EditorStyles.label : style);
			xOffset += Mathf.RoundToInt(EditorStyles.label.CalcSize(labelContent).x) + propertySpacingHorizontal;
		}

		protected void DrawPowerHelpBox(int basePower, PowerData powerData){
			int numStages = powerData.MaxStage - powerData.MinStage;
			int numCols = numStages > 4 ? 2 : 1;
			string[] lines = new string[Mathf.Max(Mathf.FloorToInt(numStages / numCols), numStages > 4 ? 4 : 1) + (numStages <= 0 ? 0 : 1)];
			int jMod = Mathf.Max(4, lines.Length);

			if(powerData.PowerScalingMode == PowerScaling.Custom){
				if(lastPowerFormulaEvaluated != powerData.PowerFormula || lastMinStage != powerData.MinStage || lastMaxStage != powerData.MaxStage){
					try{
						for(int i = powerData.MinStage, j = 0; i <= powerData.MaxStage; i++, j++){
							lines[numCols == 2 ? j % jMod : j] += string.Format("Stage {0}: {1}, ({2:0.0000})\t", i, powerData.GetScaledPowerInt(basePower, i), powerData.GetScaledPowerFloat(basePower, i, true));
						}

						cachedPowerFormulaMessage = string.Format("Power is {0}\n\n", powerData.PowerFormula) + string.Join("\n", lines);
					}
					catch{
						cachedPowerFormulaMessage = string.Empty;
					}

					lastPowerFormulaEvaluated = powerData.PowerFormula;
					lastMinStage = powerData.MinStage;
					lastMaxStage = powerData.MaxStage;
				}

				if(cachedPowerFormulaMessage != string.Empty){
					if(Regex.IsMatch(powerData.PowerFormula, @"\d[dD]\d")){
						EditorGUILayout.HelpBox("Dice rolls are evaluated on each call; float and int values will mismatch.", MessageType.Warning);
					}
					
					EditorGUILayout.HelpBox(cachedPowerFormulaMessage, MessageType.Info);
				}
				else{
					EditorGUILayout.HelpBox("Power could not be calculated for this custom power formula; invalid format used.", MessageType.Error);
				}
			}
			else{
				for(int i = powerData.MinStage, j = 0; i <= powerData.MaxStage; i++, j++){
					lines[numCols == 2 ? j % jMod : j] += string.Format("Stage {0}: {1}, ({2:0.0000})\t", i, powerData.GetScaledPowerInt(basePower, i), powerData.GetScaledPowerFloat(basePower, i, true));
				}

				if(powerData.PowerScalingMode == PowerScaling.Linear){
					EditorGUILayout.HelpBox(string.Format("Power is {0} + [stage - 1] * {1}\n\n", basePower, powerData.PowerIncrement) + string.Join("\n", lines),
						MessageType.Info);
				}
				else if(powerData.PowerScalingMode == PowerScaling.ExponentialSimple){
					EditorGUILayout.HelpBox(string.Format("Power is {0} * {1}^[stage]\n\n", basePower, powerData.PowerIncrement) + string.Join("\n", lines),
						MessageType.Info);
				}
				else if(powerData.PowerScalingMode == PowerScaling.ExponentialSimpleSymmetric){
					EditorGUILayout.HelpBox(string.Format("Power is {0} * {1}^[stage] for stages > 0; else {0} - ([calculated power] - {0})\n\n", basePower, powerData.PowerIncrement) + string.Join("\n", lines),
						MessageType.Info);
				}
				else if(powerData.PowerScalingMode == PowerScaling.ExponentialComplex){
					EditorGUILayout.HelpBox(string.Format("Power is {0} + {1} * {2}^[stage]\n\n", basePower, powerData.PowerMultiplier, powerData.PowerIncrement) + string.Join("\n", lines),
						MessageType.Info);
				}
				else if(powerData.PowerScalingMode == PowerScaling.ExponentialComplexSymmetric){
					EditorGUILayout.HelpBox(string.Format("Power is {0} + {1} * {2}^[stage] for stages > 0; else {0} - ([calculated power] - {0})\n\n", basePower, powerData.PowerMultiplier, powerData.PowerIncrement) + string.Join("\n", lines),
						MessageType.Info);
				}
			}
		}

		protected void DrawMiscProperties(string[] excludedProperties){
			var propIter = serializedObject.GetIterator();
			int numPropsToDraw = 1;

			propIter.NextVisible(true);

			while(propIter.NextVisible(false)){
				numPropsToDraw++;
			}

			if(numPropsToDraw > excludedProperties.Length){
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Misc.", EditorStyles.boldLabel);


				DrawPropertiesExcluding(serializedObject, excludedProperties);
			}
		}

		protected void PopulateGenericMenu<T>(GenericMenu menu, GenericMenu.MenuFunction2 clickHandler, System.Func<Object, string> labelGetter){
			menu.AddItem(new GUIContent("Default"), false, clickHandler, null);
			menu.AddSeparator("");

			string[] guids = AssetDatabase.FindAssets("t:"+typeof(T).Name);

			foreach(string guid in guids){
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(T));

				menu.AddItem(new GUIContent(labelGetter(obj)), false, clickHandler, obj);
			}
		}
	}
}