using UnityEngine;
using UnityEditor;
using System.Reflection;
using Ares.Development;

namespace Ares.Editor {
	[CustomEditor(typeof(BattleDelayElement), true)]
	public class BattleDelayElementEditor : UnityEditor.Editor {
		string[] excludedProperties;

		public void OnEnable(){
			excludedProperties = new string[3];
		}

		public override void OnInspectorGUI(){
			excludedProperties[0] = "";
			excludedProperties[1] = "";
			excludedProperties[2] = "";

			if(!serializedObject.FindProperty("autoLinkToLastActiveBattle").boolValue){
				excludedProperties[0] = "delayAutoLinkByOneFrame";
			}

			if(!serializedObject.FindProperty("autoLockBattleAfterLink").boolValue){
				excludedProperties[1] = "delayAutoLockByOneFrame";
				excludedProperties[2] = "autoLockReason";
			}
			else if(serializedObject.FindProperty("delayAutoLinkByOneFrame").boolValue){
				excludedProperties[1] = "delayAutoLockByOneFrame";
			}

			DrawPropertiesExcluding(serializedObject, excludedProperties);

			serializedObject.ApplyModifiedProperties();
		}
	}
}