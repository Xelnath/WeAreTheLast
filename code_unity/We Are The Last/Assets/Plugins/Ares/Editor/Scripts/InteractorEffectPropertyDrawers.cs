using UnityEngine;
using UnityEditor;
using System.Linq;
using Ares.ActorComponents;

namespace Ares.Editor {
	[CustomPropertyDrawer(typeof(AnimationEffect))]
	public class AnimationEffectEditor : AresPropertyDrawer {
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
			if(!property.FindPropertyRelative("enabled").boolValue){
				return EditorGUIUtility.standardVerticalSpacing * 3;
			}

			int numLines = property.FindPropertyRelative("delayType").enumValueIndex == (int)DelayType.None ? 4 : 5;

			return GetDefaultPropertyHeight(numLines);
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label){
			float minLabelWidth = 115f;
			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			float fullLabelWidth = EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth, minLabelWidth);
			float halfSpacing = 4f;

			SerializedProperty spEnabled = property.FindPropertyRelative("enabled");

			rect.height = EditorGUIUtility.singleLineHeight;

			// ActorAnimation drawer handled in own inspector because from there we have access to the Animator, and can elimate the use of magic strings.
			// Therefore we only have to concern ourselves with Scriptable Object inspectors.
			EditorGUI.PropertyField(rect, spEnabled, GUIContent.none);

			rect.x += 16;
			DrawLabelField(ref rect, label);
			rect.x -= 16;

			if(!spEnabled.boolValue){
				return;
			}

			SerializedProperty spParamType = property.FindPropertyRelative("parameterType");
			SerializedProperty spDelayType = property.FindPropertyRelative("delayType");

			Rect oldRect = rect;

			DrawPropertyField(ref rect, property.FindPropertyRelative("parameterName"));

			rect.width = rect.width * .7f - halfSpacing;

			EditorGUI.PropertyField(rect, spParamType);

			rect.x += rect.width + halfSpacing;
			rect.width = oldRect.width - rect.width - halfSpacing;
			EditorGUIUtility.labelWidth = fullLabelWidth * .3f - halfSpacing;

			if(spParamType.enumValueIndex < 2){
				DrawPropertyField(ref rect, property.FindPropertyRelative("valueFloat"), "Value");
			}
			else if(spParamType.enumValueIndex == 2){
				DrawPropertyField(ref rect, property.FindPropertyRelative("valueBool"), "Value");
			}
			else{
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			rect.x = oldRect.x;
			rect.width = oldRect.width * .7f - halfSpacing;
			EditorGUIUtility.labelWidth = fullLabelWidth;

			EditorGUI.PropertyField(rect, spDelayType, new GUIContent("Battle Delay"));

			rect.x += rect.width + halfSpacing;
			rect.width = oldRect.width - rect.width - halfSpacing;
			EditorGUIUtility.labelWidth = fullLabelWidth * .3f - halfSpacing;

			bool showDelayWarning = false;

			switch(spDelayType.enumValueIndex){
				case (int)DelayType.Time:
					DrawPropertyField(ref rect, property.FindPropertyRelative("delayTime"), "Time");
					showDelayWarning = true;
					break;
				case (int)DelayType.Animation:
					DrawPropertyField(ref rect, property.FindPropertyRelative("delayAnimationLayer"), "Layer");
					showDelayWarning = true;
					break;
			}

			if(showDelayWarning){
				rect.x = oldRect.x;
				rect.y -= 4;
				rect.width = oldRect.width;
				rect.height = EditorGUIUtility.singleLineHeight * 2;
				Color labelColor = EditorStyles.miniLabel.normal.textColor;
				labelColor.a = .5f;
				EditorStyles.miniLabel.normal.textColor = labelColor;
				EditorStyles.miniLabel.wordWrap = true;
				GUI.Label(rect, "*Battle delays only supported on Actors with an ActorAnimation component attached.", EditorStyles.miniLabel);
				EditorStyles.miniLabel.wordWrap = false;
				labelColor.a = 1f;
				EditorStyles.miniLabel.normal.textColor = labelColor;
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;
		}
	}

	[CustomPropertyDrawer(typeof(AudioEffect))]
	public class AudioEffectEditor : AresPropertyDrawer {
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
			if(!property.FindPropertyRelative("enabled").boolValue){
				return EditorGUIUtility.standardVerticalSpacing * 3;
			}

			int numLines = property.serializedObject.targetObject.GetType() == typeof(EnvironmentVariableData) ? 4 : 5;

			return GetDefaultPropertyHeight(numLines);
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label){
			System.Type ownerType = property.serializedObject.targetObject.GetType();

			float minLabelWidth = 115f;
			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth, minLabelWidth);

			SerializedProperty spEnabled = property.FindPropertyRelative("enabled");

			rect.height = EditorGUIUtility.singleLineHeight;

			if(ownerType == typeof(ActorAudio)){
				float shiftX = rect.x - 50f - 20f;
				rect.x -= shiftX;
				rect.width += shiftX;
			}
			else{
				EditorGUI.PropertyField(rect, spEnabled, GUIContent.none);

				rect.x += 16;
				DrawLabelField(ref rect, label);
				rect.x -= 16;

				if(!spEnabled.boolValue){
					return;
				}
			}

			DrawPropertyField(ref rect, property.FindPropertyRelative("clip"));
			DrawPropertyField(ref rect, property.FindPropertyRelative("volume"));

			if(ownerType != typeof(EnvironmentVariableData)){
				DrawPropertyField(ref rect, property.FindPropertyRelative("playPosition"));
			}

			DrawPropertyField(ref rect, property.FindPropertyRelative("delay"));

			EditorGUIUtility.labelWidth = defaultLabelWidth;
		}
	}

	[CustomPropertyDrawer(typeof(InstantiationEffect))]
	public class InstantiationEffectEditor : AresPropertyDrawer {
		public static Object lastInspectorWindowShown; //Hacky, but prevents constant, needless reloading of ability effect list

		static string[] oldabilityEffectGuids;
		static string[] abilityEffectGuids;
		static InstantiationEffectInstance[] abilityEffects;
		static string[] abilityEffectNames;
		static string[] abilityEffectNamesWithNone;

		bool showTransformNotSupportedText = false;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
			if(!property.FindPropertyRelative("enabled").boolValue){
				return EditorGUIUtility.standardVerticalSpacing * 3;
			}

			SerializedProperty spTargetMode = property.FindPropertyRelative("targetMode");
			InstantiationTargetMode targetMode = (InstantiationTargetMode)System.Enum.GetValues(typeof(InstantiationTargetMode)).GetValue(spTargetMode.enumValueIndex);
			int numLines = 7;

			if(targetMode == InstantiationTargetMode.FindByName || targetMode == InstantiationTargetMode.Transform){
				numLines++;

				if(property.FindPropertyRelative("parentToTarget").boolValue){
					numLines++;
				}
			}

			if(!EditorGUIUtility.wideMode){
				numLines += 3;
			}

			return GetDefaultPropertyHeight(numLines);
		}

		public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label){
			bool isOnActor = property.serializedObject.targetObject.GetType() == typeof(ActorInstantiation);

			float minLabelWidth = 115f;
			float defaultLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth, minLabelWidth);

			SerializedProperty spEnabled = property.FindPropertyRelative("enabled");
			SerializedProperty spTargetMode = property.FindPropertyRelative("targetMode");
			InstantiationTargetMode targetMode = (InstantiationTargetMode)System.Enum.GetValues(typeof(InstantiationTargetMode)).GetValue(spTargetMode.enumValueIndex);
			SerializedProperty spParentToTarget = property.FindPropertyRelative ("parentToTarget");

			rect.height = EditorGUIUtility.singleLineHeight;

			if(isOnActor){
				float shiftX = rect.x - 50f - 20f;
				rect.x -= shiftX;
				rect.width += shiftX;
			}
			else{
				EditorGUI.PropertyField(rect, spEnabled, GUIContent.none);

				rect.x += 16;
				DrawLabelField(ref rect, label);
				rect.x -= 16;

				if(!spEnabled.boolValue){
					return;
				}
			}

			if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ABILITY_EFFECT_OPTIONS, true)){
				if(abilityEffectGuids == null || lastInspectorWindowShown != property.serializedObject.targetObject){
					string[] oldabilityEffectGuids = abilityEffectGuids;

					abilityEffectGuids = AssetDatabase.FindAssets("t:Prefab");

					if(oldabilityEffectGuids == null || !oldabilityEffectGuids.SequenceEqual(abilityEffectGuids)){
						abilityEffects = abilityEffectGuids.Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).
							Select(g => g.GetComponent<InstantiationEffectInstance>()).Where(e => e != null).OrderBy(e => e.name).ToArray();
				
						abilityEffectNames = abilityEffects.Select(d => d.name).OrderBy(n => n).ToArray();
						abilityEffectNamesWithNone = new string[]{ "[Missing!]" }.Concat(abilityEffectNames).ToArray();
					}
				}

				Rect oldRect = rect;

				SerializedProperty spEffect = property.FindPropertyRelative("effect");
				int enumIndex = 0;

				if(spEffect.objectReferenceValue == null){
					GUI.backgroundColor = AresEditor.InvalidEnumColor;
					enumIndex = EditorGUI.Popup(rect, "Effect", enumIndex, abilityEffectNamesWithNone);
					GUI.backgroundColor = Color.white;

					if(enumIndex > 0){
						spEffect.objectReferenceValue = abilityEffects[enumIndex - 1];
					}
				}
				else{
					enumIndex = System.Array.IndexOf(abilityEffectNames, ((InstantiationEffectInstance)spEffect.objectReferenceValue).name);
					enumIndex = EditorGUI.Popup(rect, "Effect", enumIndex, abilityEffectNames);

					if(enumIndex == -1){
						spEffect.objectReferenceValue = null;
					}
					else{
						spEffect.objectReferenceValue = abilityEffects[enumIndex];
					}
				}

				rect = oldRect;
				rect.y += EditorGUIUtility.singleLineHeight + 1;
			}
			else{
				DrawPropertyField(ref rect, property.FindPropertyRelative("effect"));
			}

			float onActorCorrection = (isOnActor ? rect.x - 27f : 0f);
			float totalWidth = GUILayoutUtility.GetLastRect().width + 32 - onActorCorrection;
			float defaultX = rect.x;
			float defaultWidth = rect.width;
			string offsetLabel = "Position";


			rect.width = EditorGUIUtility.labelWidth + Mathf.Max(Mathf.Min(totalWidth * .2f, 90f), 40f);
			EditorGUI.PropertyField(rect, spTargetMode, new GUIContent("Target"));
			rect.x += rect.width + 4;

			switch(targetMode){
				case InstantiationTargetMode.Transform:
					rect.width = totalWidth - rect.width - 93;

					if(isOnActor){
						EditorGUI.PropertyField(rect, property.FindPropertyRelative("targetTransform"), GUIContent.none);
					}
					else{
						if(property.serializedObject.targetObject.GetType() == typeof(InstantiationEffect)){
							((InstantiationEffect)fieldInfo.GetValue(property.serializedObject.targetObject)).NotifyTransformTargetNotSupported();
							showTransformNotSupportedText = true;
						}
						else{
							spTargetMode.enumValueIndex = (int)InstantiationTargetMode.FindByName;
						}
					}

					rect.x = defaultX;
					rect.width = defaultWidth - onActorCorrection;
					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					DrawPropertyField(ref rect, spParentToTarget);

					if(spParentToTarget.boolValue){
						DrawPropertyField(ref rect, property.FindPropertyRelative ("inheritParentScale"));
					}

					offsetLabel = "Offset";
					break;
				case InstantiationTargetMode.FindByName:
					rect.width = totalWidth - rect.width - 93 - onActorCorrection - 14;

					EditorGUI.PropertyField(rect, property.FindPropertyRelative("targetName"), GUIContent.none);
					rect.x += rect.width + 3;

					EditorGUI.LabelField(rect, "in");
					rect.x += 17;

					rect.width = 50;
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("targetActor"), GUIContent.none);

					rect.x = defaultX;
					rect.width = defaultWidth - onActorCorrection;
					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

					DrawPropertyField(ref rect, spParentToTarget);

					if(spParentToTarget.boolValue){
						DrawPropertyField(ref rect, property.FindPropertyRelative ("inheritParentScale"));
					}

					offsetLabel = "Offset";
					break;
				case InstantiationTargetMode.LocalPosition:
					EditorGUI.LabelField(rect, "relative to");
					rect.x += 60;

					rect.width = Mathf.Max(Mathf.Min(totalWidth * .15f, 50f), 15f);
					EditorGUI.PropertyField(rect, property.FindPropertyRelative("targetActor"), GUIContent.none);

					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					break;
				case InstantiationTargetMode.WorldPosition:
					rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					break;
			}

			rect.x = defaultX;
			rect.width = defaultWidth;

			DrawPropertyField(ref rect, property.FindPropertyRelative("offset"), offsetLabel);

			if(!EditorGUIUtility.wideMode){
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			DrawPropertyField(ref rect, property.FindPropertyRelative("rotation"));

			if(!EditorGUIUtility.wideMode){
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}

			DrawPropertyField(ref rect, property.FindPropertyRelative("scale"));

			if(!EditorGUIUtility.wideMode){
				rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			}
				
			DrawPropertyField(ref rect, property.FindPropertyRelative("delay"));

			if(showTransformNotSupportedText){
				int up = EditorGUIUtility.wideMode ? 8 : 11;

				rect.x += 180;
				rect.width -= 180;
				rect.y -= (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * up;
				EditorGUI.HelpBox(rect, "Transform targets are not supported on Scriptable Objects.", MessageType.Warning);
			}

			EditorGUIUtility.labelWidth = defaultLabelWidth;
			rect.x = defaultX;
			rect.width = defaultWidth;

			lastInspectorWindowShown = property.serializedObject.targetObject;
		}
	}
}