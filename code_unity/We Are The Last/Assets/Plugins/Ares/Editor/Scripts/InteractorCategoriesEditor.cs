using UnityEngine;
using UnityEditor;
using Ares.Development;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Ares.Editor {
	[CustomPropertyDrawer(typeof(InteractorCategoryAttribute))]
	public class InteractorCategoryDrawer : PropertyDrawer {
		InteractorCategories target;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label){
			InteractorCategoryAttribute interactorCategory = (InteractorCategoryAttribute)attribute;

			Rect rect = position;
			rect.width -= 28;

			if(target == null){
				Resources.LoadAll<InteractorCategories>("");
				target = Resources.FindObjectsOfTypeAll<InteractorCategories>().First(c => c.Type == interactorCategory.categoryType);
			}

			InteractorCategoriesData interactorCategoriesData = target.GetAll();
			property.intValue = EditorGUI.IntPopup(rect, "Category", property.intValue, interactorCategoriesData.Names, interactorCategoriesData.Ids);

			rect.x += rect.width + 4;
			rect.width = 24;

			if(GUI.Button(rect, "+")){
				Selection.activeObject = target;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label){
			return EditorGUIUtility.singleLineHeight;
		}
	}

	[CustomEditor(typeof(InteractorCategories))]
	public class InteractorCategoriesEditor : UnityEditor.Editor {
		Dictionary<string, List<InteractorCategory>> categories;
		Dictionary<string, string> newCategoryNames;
		string currentlyEditedPath;

		public void OnEnable(){
			newCategoryNames = new Dictionary<string, string>();

			SerializedProperty spCategories = serializedObject.FindProperty("categories");

			for(int i = 0; i < spCategories.arraySize; i++){
				SerializedProperty spCategory = spCategories.GetArrayElementAtIndex(i);
				SerializedProperty spPath = spCategory.FindPropertyRelative("path");

				if(!newCategoryNames.ContainsKey(spPath.stringValue)){
					newCategoryNames.Add(spPath.stringValue, "");
				}
			}

			UpdateCategories();
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			foreach(InteractorCategory category in categories[""]){
				if(category.path != "None"){
					DrawCategory(category.path, 0);
				}
			}

			if(GUILayout.Button("New base category")){
				CreateNewCategory(GetNewDefaultCategoryName(), true);
			}
		}

		string GetNewDefaultCategoryName(){
			return "New category " + (serializedObject.FindProperty("maxId").intValue - 1).ToString();
		}

		void CreateNewCategory(string path, bool setNewCategoryAsCurrentlyEdited){
			SerializedProperty spCategories = serializedObject.FindProperty("categories");
			SerializedProperty spMaxId = serializedObject.FindProperty("maxId");

			spCategories.arraySize++;
			SerializedProperty newCategory = spCategories.GetArrayElementAtIndex(spCategories.arraySize - 1);
			newCategory.FindPropertyRelative("path").stringValue = path;
			newCategory.FindPropertyRelative("id").intValue = spMaxId.intValue;
			spMaxId.intValue++;

			newCategoryNames.Add(path, GetNewDefaultCategoryName());

			serializedObject.ApplyModifiedProperties();
			UpdateCategories();
		}

		void DeleteCategory(string path){
			SerializedProperty spCategories = serializedObject.FindProperty("categories");

			int deletedCategories = 0;

			for(int i = 0; i < spCategories.arraySize; i++){
				SerializedProperty spCategory = spCategories.GetArrayElementAtIndex(i);
				SerializedProperty spPath = spCategory.FindPropertyRelative("path");

				if(spPath.stringValue.StartsWith(path)){
					for(int j = i; j < spCategories.arraySize - 1; j++){
						SerializedProperty spCurrentCategory = spCategories.GetArrayElementAtIndex(j);
						SerializedProperty spNextCategory = spCategories.GetArrayElementAtIndex(j+1);

						spCurrentCategory.FindPropertyRelative("path").stringValue = spNextCategory.FindPropertyRelative("path").stringValue;
						spCurrentCategory.FindPropertyRelative("id").intValue = spNextCategory.FindPropertyRelative("id").intValue;
					}

					deletedCategories++;
				}
			}

			spCategories.arraySize -= deletedCategories;

			serializedObject.ApplyModifiedProperties();
			UpdateCategories();
		}

		void UpdateCategories(){
			categories = new Dictionary<string, List<InteractorCategory>>();

			serializedObject.Update();

			SerializedProperty spCategories = serializedObject.FindProperty("categories");
			SerializedProperty spMaxId = serializedObject.FindProperty("maxId");

			for(int i = 0; i < spCategories.arraySize; i++){
				SerializedProperty spCategory = spCategories.GetArrayElementAtIndex(i);
				SerializedProperty spPath = spCategory.FindPropertyRelative("path");

				string[] pathMembers = spPath.stringValue.Split('/');
				string pathStart = string.Join("/", pathMembers, 0, pathMembers.Length - 1);

				InteractorCategory newCategory = new InteractorCategory(spMaxId.intValue, spPath.stringValue);
				spMaxId.intValue++;

				if(!categories.ContainsKey(pathStart)){
					categories.Add(pathStart, new List<InteractorCategory>());
					categories.Add(spPath.stringValue, new List<InteractorCategory>());
				}

				categories[pathStart].Add(newCategory);
			}
		}

		int DrawCategory(string path, int depth){
			GUILayout.BeginHorizontal();
			GUILayout.Space(depth * 15f);

			int numChildren = 1;

			if(currentlyEditedPath != path){
				GUILayout.Label(path);
			}
			else{
				string newPath = EditorGUILayout.DelayedTextField(path);

				if(newPath != path){
					SerializedProperty spCategories = serializedObject.FindProperty("categories");

					for(int i = 0; i < spCategories.arraySize; i++){
						SerializedProperty spCategory = spCategories.GetArrayElementAtIndex(i);
						SerializedProperty spPath = spCategory.FindPropertyRelative("path");

						spPath.stringValue = Regex.Replace(spPath.stringValue, "^"+path, newPath);

						if(!newCategoryNames.ContainsKey(spPath.stringValue)){
							newCategoryNames.Add(spPath.stringValue, GetNewDefaultCategoryName());
						}
					}

					serializedObject.ApplyModifiedProperties();

					UpdateCategories();
				}
			}

			Rect lasRect = GUILayoutUtility.GetLastRect();

			if(GUILayout.Button("Rename", GUILayout.Width(60f))){
				currentlyEditedPath = path;
			}
			if(GUILayout.Button("X", GUILayout.Width(30f)) && EditorUtility.DisplayDialog("Delete " + path, "Warning: You are about to delete '" + path + "' and all its subcategories.", "Continue", "Cancel")){
				DeleteCategory(path);
			}

			GUILayout.EndHorizontal();

			if(categories.ContainsKey(path)){
				foreach(InteractorCategory category in categories[path]){
					numChildren += DrawCategory(category.path, depth + 1);
				}
			}

			Color col = (depth % 2) == 0 ? new Color(.4f, .4f, .4f, .4f) : new Color(.2f, .2f, .2f, .4f);

			EditorGUI.DrawRect(new Rect(lasRect.x + 7, lasRect.y + 16, 3, (numChildren) * 20 - 8), col);
			EditorGUI.DrawRect(new Rect(lasRect.x + 7, lasRect.y + 16 + (numChildren) * 20 - 8, 10, 3), col);

			GUILayout.BeginHorizontal();
			GUILayout.Space((depth + 1) * 15f);
			newCategoryNames[path] = GUILayout.TextField(newCategoryNames[path]);

			if(GUILayout.Button("Add", GUILayout.Width(40f))){
				CreateNewCategory(path + "/" + newCategoryNames[path], false);
				newCategoryNames[path] = GetNewDefaultCategoryName();
			}

			GUILayout.Space(110f);
			GUILayout.EndHorizontal();

			return numChildren + 1;
		}
	}
}