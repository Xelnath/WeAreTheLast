using UnityEngine;
using UnityEditor;
using Ares.Development;

namespace Ares.Editor {
	public class AresPreferences {
		static public string ARES_PREF_ADD_ABILITY_OPTIONS {get{return "AresUseAddAbilityOptions";}}
		static public string ARES_PREF_ADD_AFFLICTION_OPTIONS {get{return "AresUseAddAfflictionOptions";}}
		static public string ARES_PREF_ADD_ITEM_OPTIONS {get{return "AresUseAddItemOptions";}}
		static public string ARES_PREF_ADD_ITEM_GROUP_OPTIONS {get{return "AresUseAddItemGroupOptions";}}
		static public string ARES_PREF_ADD_ABILITY_EFFECT_OPTIONS {get{return "AresUseAddAbilityEffectOptions";}}
		static public string ARES_PREF_USE_VERBOSE_LOGGING {get{return "AresUseVerboseLogging";}}

		static public string ARES_PREF_LOG_COLOR_REGULAR {get{return "AresRegularLogColor";}}
		static public string ARES_PREF_LOG_COLOR_STATE1 {get{return "AresState1LogColor";}}
		static public string ARES_PREF_LOG_COLOR_STATE2 {get{return "AresState2LogColor";}}
		static public string ARES_PREF_LOG_COLOR_ACTION {get{return "AresActionLogColor";}}
		static public string ARES_PREF_LOG_COLOR_UNIMPORTANT {get{return "AresUnimportanceLogColor";}}

		static bool prefsLoaded = false;

		public static bool useAddAbilityOptions = false;
		public static bool useAddAfflictionOptions = false;
		public static bool useAddItemOptions = false;
		public static bool useAddItemGroupOptions = false;
		public static bool useAddAbilityEffectOptions = false;
		public static bool useVerboseLogging = false;

		static SerializedObject soVerboseLoggerSettings;

		[PreferenceItem("Ares")]
		public static void PreferencesGUI(){
			SerializedProperty[] spVerboseLoggerColors = null;

			if(!prefsLoaded){
				useAddAbilityOptions = EditorPrefs.GetBool(ARES_PREF_ADD_ABILITY_OPTIONS, true);
				useAddAfflictionOptions = EditorPrefs.GetBool(ARES_PREF_ADD_AFFLICTION_OPTIONS, true);
				useAddItemOptions = EditorPrefs.GetBool(ARES_PREF_ADD_ITEM_OPTIONS, true);
				useAddItemGroupOptions = EditorPrefs.GetBool(ARES_PREF_ADD_ITEM_GROUP_OPTIONS, true);
				useAddAbilityEffectOptions = EditorPrefs.GetBool(ARES_PREF_ADD_ABILITY_EFFECT_OPTIONS, true);
				useVerboseLogging = EditorPrefs.GetBool(ARES_PREF_USE_VERBOSE_LOGGING, false);

				prefsLoaded = true;
			}

			if(spVerboseLoggerColors == null){
				VerboseLoggerSettingsObject[] settings = Resources.LoadAll<VerboseLoggerSettingsObject>("");

				if(settings.Length == 0){
					GUILayout.Label("No ARES Verbose Logger Settings could be loaded.\nPlease ensure one exists inside a \"Resources\" folder.");
				}
				else{
					soVerboseLoggerSettings = new SerializedObject(settings[0]);

					spVerboseLoggerColors = new SerializedProperty[] {
						soVerboseLoggerSettings.FindProperty("regularColor"),
						soVerboseLoggerSettings.FindProperty("state1Color"),
						soVerboseLoggerSettings.FindProperty("state2Color"),
						soVerboseLoggerSettings.FindProperty("actionColor"),
						soVerboseLoggerSettings.FindProperty("unimportantColor")
					};
				}
			}

//			EditorGUILayout.LabelField("Enum options on list add", EditorStyles.boldLabel);
			useAddAbilityOptions = EditorGUILayout.Toggle("Show ability options on list add", useAddAbilityOptions);
			useAddAfflictionOptions = EditorGUILayout.Toggle("Show affliction options on list add", useAddAfflictionOptions);
			useAddItemOptions = EditorGUILayout.Toggle("Show item options on list add", useAddItemOptions);
			useAddItemGroupOptions = EditorGUILayout.Toggle("Show item group options on list add", useAddItemGroupOptions);

			EditorGUILayout.Space();

			useAddAbilityEffectOptions = EditorGUILayout.Toggle("Show instantiation effect as enum", useAddAbilityEffectOptions);

			if(GUI.changed){
				EditorPrefs.SetBool(ARES_PREF_ADD_ABILITY_OPTIONS, useAddAbilityOptions);
				EditorPrefs.SetBool(ARES_PREF_ADD_AFFLICTION_OPTIONS, useAddAfflictionOptions);
				EditorPrefs.SetBool(ARES_PREF_ADD_ITEM_OPTIONS, useAddItemOptions);
				EditorPrefs.SetBool(ARES_PREF_ADD_ITEM_GROUP_OPTIONS, useAddItemGroupOptions);
				EditorPrefs.SetBool(ARES_PREF_ADD_ABILITY_EFFECT_OPTIONS, useAddAbilityEffectOptions);
			}

			EditorGUILayout.Space();
			EditorGUI.BeginChangeCheck();

			useVerboseLogging = EditorGUILayout.Toggle("Use verbose debug logging", useVerboseLogging);

			if(EditorGUI.EndChangeCheck()){
				EditorPrefs.SetBool(ARES_PREF_USE_VERBOSE_LOGGING, useVerboseLogging);

				string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

				if(useVerboseLogging && !symbols.Contains("ENABLE_ARES_VERBOSE_LOGGING")){
					PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols + ";ENABLE_ARES_VERBOSE_LOGGING");
				}
				else if(!useVerboseLogging && symbols.Contains("ENABLE_ARES_VERBOSE_LOGGING")){
					PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols.Replace("ENABLE_ARES_VERBOSE_LOGGING", ""));
				}
			}

			if(useVerboseLogging){
				foreach(SerializedProperty prop in spVerboseLoggerColors){
					prop.colorValue = EditorGUILayout.ColorField(prop.displayName.Replace("Color", "verbose logging color"), prop.colorValue);
				}

				GUILayout.BeginHorizontal();
				if(GUILayout.Button("Default light skin colors")){
					spVerboseLoggerColors[0].colorValue = new Color(0f, 0.2f, 0f, 1f);
					spVerboseLoggerColors[1].colorValue = new Color(0f, .3f, .4f, 1f);
					spVerboseLoggerColors[2].colorValue = new Color(0f, .2f, .3f, 1f);
					spVerboseLoggerColors[3].colorValue = new Color(.36f, .02f, .3f, 1f);
					spVerboseLoggerColors[4].colorValue = new Color(0f, 0f, 0f, .5f);
				}
				if(GUILayout.Button("Default pro skin colors")){
					spVerboseLoggerColors[0].colorValue = new Color(0.56f, 1f, 0.56f, 1f);
					spVerboseLoggerColors[1].colorValue = new Color(0.15f, .74f, .93f, 1f);
					spVerboseLoggerColors[2].colorValue = new Color(0.6f, .85f, .98f, 1f);
					spVerboseLoggerColors[3].colorValue = new Color(.98f, .64f, .92f, 1f);
					spVerboseLoggerColors[4].colorValue = new Color(1f, 1f, 1f, .5f);
				}
				GUILayout.EndHorizontal();

				if(GUI.changed){
					soVerboseLoggerSettings.ApplyModifiedProperties();
				}
			}

			GUILayout.Space(EditorGUIUtility.singleLineHeight);

			EditorGUILayout.LabelField("Verbose Debug Logging adds the ENABLE_ARES_VERBOSE_LOGGING symbol to the Scripting Define Symbols list " +
				"(Edit > Project Settings > Player) for the current target plaform. These verbose functions are 1:1 wrappers for the Debug.Log() family of methods.",
				EditorStyles.wordWrappedMiniLabel);
		}

		static void ColorFromLoadedString(ref Color color, Color defaultColor, string loadedString){
			if(string.IsNullOrEmpty(loadedString)){
				color = defaultColor;
			}
			else{
				ColorUtility.TryParseHtmlString(loadedString, out color);
			}
		}
	}
}