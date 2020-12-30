using UnityEngine;
using UnityEditor;
using System.Reflection;
using Ares.Development;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(Readme))]
	public class ReadmeEditor : UnityEditor.Editor {
		readonly string[] excludedProperties = new string[]{"m_Script", "titles", "texts", "showFilterButton"};

		static readonly bool dev = false;
		static readonly string filterString = "t:Readme";

		delegate void StringDelegate(string filter);
		delegate bool VoidDelegate();

		StringDelegate SetHierarchySearchFilter;
		VoidDelegate ShouldShowSetFilterButton;

		void OnEnable(){

			var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();

			foreach(var window in windows){
				if(window.titleContent.text == "Hierarchy"){
					#if !UNITY_2018_1_OR_NEWER
					MethodInfo setSearchFilterMethod = window.GetType().GetMethod("SetSearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);
					#endif

					FieldInfo searchFilterField = window.GetType().GetField("m_SearchFilter", BindingFlags.NonPublic | BindingFlags.Instance);

					SetHierarchySearchFilter = new StringDelegate(delegate(string filter){
						#if UNITY_2018_1_OR_NEWER
						SceneModeUtility.SearchForType(string.IsNullOrEmpty(filter) ? null : typeof(Readme));
						#else
						setSearchFilterMethod.Invoke(window, new object[]{filter, 0, true});
						#endif
					});

					ShouldShowSetFilterButton = new VoidDelegate(delegate(){return ((string)searchFilterField.GetValue(window)) != filterString;});

					break;
				}
			}
		}

		public override void OnInspectorGUI(){
			if(dev){
				base.OnInspectorGUI();
			}
			else{
				DrawPropertiesExcluding(serializedObject, excludedProperties);
			}

			EditorStyles.label.wordWrap = true ;

			SerializedProperty spTitles = serializedObject.FindProperty("titles");
			SerializedProperty spTexts = serializedObject.FindProperty("texts");

			for(var i = 0; i < spTitles.arraySize; i++){
				string title = spTitles.GetArrayElementAtIndex(i).stringValue;

				if(title != string.Empty){
					EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
				}

				EditorGUILayout.LabelField(spTexts.GetArrayElementAtIndex(i).stringValue.Replace("\\n","\n"));
				GUILayout.Space(6f);
			}

			if(serializedObject.FindProperty("showFilterButton").boolValue){
				if(ShouldShowSetFilterButton()){
					if(GUILayout.Button("Filter readmes", GUILayout.MaxWidth(150f))){
						SetHierarchySearchFilter(filterString);
					}
				}
				else if(GUILayout.Button("Clear filter", GUILayout.MaxWidth(150f))){
					SetHierarchySearchFilter(string.Empty);
				}
			}
		}
	}
}