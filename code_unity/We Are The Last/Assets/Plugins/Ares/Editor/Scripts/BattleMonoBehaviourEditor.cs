using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ares.Editor {
	[CustomEditor(typeof(BattleMonoBehaviour), true)]
	public class BattleMonoBehaviourEditor : AresEditor {
		ReorderableList groupsList;
		ReorderableList envVarList;
		ReorderableList delayElementsList;
		ReorderableList delayRequestsList;
		GUIStyle hugeLabel1;
		GUIStyle hugeLabel2;

		private void OnEnable(){
			hugeLabel1 = new GUIStyle();
			hugeLabel1.normal.textColor = EditorGUIUtility.isProSkin ? new Color(.8f, .8f, .8f, 1f) : new Color(.2f, .2f, .2f, 1f);
			hugeLabel1.fontSize = 20;

			hugeLabel2 = new GUIStyle();
			hugeLabel2.normal.textColor = hugeLabel1.normal.textColor;
			hugeLabel2.fontSize = 14;

			groupsList = new ReorderableList(null, typeof(BattleGroup), false, true, false, false);
			envVarList = new ReorderableList(null, typeof(EnvironmentVariable), false, true, false, false);
			delayElementsList = new ReorderableList(null, typeof(BattleDelayElement), false, true, false, false);
			delayRequestsList = new ReorderableList(null, typeof(DelayRequest), false, true, false, false);

			groupsList.drawHeaderCallback = (Rect rect) => {
				int oneFourth = (int)(rect.width * .25f);

				EditorGUI.LabelField(rect, "Group Name:");
				rect.x = oneFourth * 2.3f;
				EditorGUI.LabelField(rect, "Actors (alive/total):");
			};

			groupsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			};

			groupsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>{
				BattleGroup group = (BattleGroup)groupsList.list[index];
				int oneFourth = (int)(rect.width * .25f);

				rect.y += 2;
				rect.width = oneFourth * 2.3f;

				EditorGUI.LabelField(rect, group.Name);
				rect.x = oneFourth * 2.3f;
				EditorGUI.LabelField(rect, string.Format("{0}/{1}", group.Actors.Where(a => a.HP > 0).Count(), group.Actors.Count));
			};

			envVarList.drawHeaderCallback = (Rect rect) => {
				int oneFourth = (int)(rect.width * .25f);

				EditorGUI.LabelField(rect, rect.width > 410 ? "Environment Variable:" : "Variable:");
				rect.x = oneFourth * 1.5f;
				EditorGUI.LabelField(rect, "Stage:");
				rect.x = oneFourth * 2.2f;
				EditorGUI.LabelField(rect, rect.width > 410 ? "Rounds Remaining:" : "Remaining:");
				rect.x = oneFourth * 3.3f;
				EditorGUI.LabelField(rect, "Created by:");
			};

			envVarList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			};

			envVarList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>{
				EnvironmentVariable envVar = (EnvironmentVariable)envVarList.list[index];
				int oneFourth = (int)(rect.width * .25f);

				rect.y += 2;
				rect.width = oneFourth * 1.1f;

				EditorGUI.LabelField(rect, envVar.Data.DisplayName);
				rect.x = oneFourth * 1.5f;
				EditorGUI.LabelField(rect, envVar.Stage.ToString());
				rect.x = oneFourth * 2.2f;

				EditorGUI.LabelField(rect, envVar.RoundsRemaining.ToString());

				rect.x = oneFourth * 3.3f;
				rect.width = oneFourth;
				EditorGUI.LabelField(rect, envVar.Setter.DisplayName);
			};

			delayElementsList.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Delay Element");
			};

			delayElementsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
			};

			delayElementsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>{
				BattleDelayElement delayElement = (BattleDelayElement)delayElementsList.list[index];

				EditorGUI.LabelField(rect, delayElement.gameObject.name);
			};

			delayRequestsList.drawHeaderCallback = (Rect rect) => {
				EditorGUI.LabelField(rect, "Time Remaining:");
			};

			delayRequestsList.elementHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

			delayRequestsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>{
				DelayRequest delayRequest = (DelayRequest)delayRequestsList.list[index];

				EditorGUI.LabelField(rect, delayRequest.TimeRemaining.ToString());
			};
		}
			
		public override void OnInspectorGUI(){
			serializedObject.Update();

			Battle battle;

			for(int i = 0; i < Battle.ActiveBattles.Count; i++){
				battle = Battle.ActiveBattles[i];

//				EditorGUILayout.LabelField(string.Format("Battle " + (i+1).ToString(), hugeLabel3, GUILayout.Height(25f)));
				EditorGUILayout.LabelField(string.Format("Battle {0}", i+1), hugeLabel1, GUILayout.Height(25f));
				EditorGUILayout.LabelField(string.Format("round {0}/{1}", battle.CurrentRound, battle.Rules.MaxRounds), hugeLabel2, GUILayout.Height(17f));

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Battle Groups", EditorStyles.boldLabel);

				groupsList.list = battle.Groups;
				groupsList.DoLayoutList();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Environment Variables", EditorStyles.boldLabel);

				envVarList.list = battle.EnvironmentVariables;
				envVarList.DoLayoutList();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Delay Elements", EditorStyles.boldLabel);

				delayElementsList.list = battle.ProgressDelayLocks.ToList();
				delayElementsList.DoLayoutList();

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Delay Requests", EditorStyles.boldLabel);

				delayRequestsList.list = battle.ProgressDelayRequests;
				delayRequestsList.DoLayoutList();
			}

			EditorUtility.SetDirty(target); //Force a refresh every frame
		}
	}
}