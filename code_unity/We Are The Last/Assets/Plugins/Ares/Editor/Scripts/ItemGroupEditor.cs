using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ares.Editor {
	[CustomEditor(typeof(ItemGroup))]
	public class ItemGroupEditor : AresEditor {
		readonly string[] hiddenProperties = {"m_Script", "displayName", "itemData"};

		ReorderableList itemsList;

		void OnEnable() {
			PropertyInfo displayNamePropertyItemData = typeof(ItemData).GetProperty("DisplayName");

			itemsList = new ReorderableList(serializedObject, serializedObject.FindProperty("itemData"), true, true, true, true);

			itemsList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Items");
			};

			itemsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 5);
			};

			itemsList.onAddCallback = (ReorderableList list) => {
				if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ITEM_OPTIONS, true)){
					GenericMenu menu = new GenericMenu();

					PopulateGenericMenu<ItemData>(menu, ItemAddHandler, o => displayNamePropertyItemData.GetValue(o, null).ToString());
					menu.ShowAsContext();
				}
				else{
					ItemAddHandler(null);
				}
			};

			itemsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = itemsList.serializedProperty.GetArrayElementAtIndex(index);

				float totalWidth = GUILayoutUtility.GetLastRect().width + 20;
				int xOffset = 0;
				rect.y += 2;

				DrawLabel(rect, ref xOffset, "Item");
				xOffset += 11;
				DrawAssetObjectField<ItemData>(rect, ref xOffset, (int)((totalWidth - xOffset) * .75f), element);
			};
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

			itemsList.DoLayoutList();

			var propIter = serializedObject.GetIterator();
			int numPropsToDraw = 1;

			propIter.NextVisible(true);

			while(propIter.NextVisible(false)){
				numPropsToDraw++;
			}

			if(numPropsToDraw > hiddenProperties.Length){
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Misc.", EditorStyles.boldLabel);

				DrawPropertiesExcluding(serializedObject, hiddenProperties);
			}

			serializedObject.ApplyModifiedProperties();
		}

		void ItemAddHandler(object target){
			itemsList.serializedProperty.arraySize++;
			itemsList.serializedProperty.GetArrayElementAtIndex(itemsList.serializedProperty.arraySize - 1).objectReferenceValue = (ItemData)target;

			serializedObject.ApplyModifiedProperties();
		}
	}
}