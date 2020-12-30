using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ares.Editor {
	[CustomEditor(typeof(InventoryGenerator))]
	public class InventoryGeneratorEditor : AresEditor {
		public static InventoryGenerator.GroupSelectMode GroupSelectionMode {get; private set;}

		readonly float fieldWidth = .4f;

		ReorderableList guaranteedItemsList;
		ReorderableList chanceItemsList;
		ReorderableList itemGroupsList;

		InventoryGenerator.InventoryType mockInventoryType;
		Inventory mockInventory;

		void OnEnable() {
			PropertyInfo displayNamePropertyItemData = typeof(ItemData).GetProperty("DisplayName");
			PropertyInfo displayNamePropertyItemGroup = typeof(ItemGroup).GetProperty("DisplayName");

			PropertyInfo categoryPropertyItemData = typeof(ItemData).GetProperty("FullCategoryPath");

			// Guaranteed items
			guaranteedItemsList = new ReorderableList(serializedObject, serializedObject.FindProperty("guaranteedItems"), true, true, true, true);

			guaranteedItemsList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Guaranteed items");
			};

			guaranteedItemsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 5);
			};

			guaranteedItemsList.onAddCallback = (ReorderableList list) => {
				if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ITEM_OPTIONS, true)){
					GenericMenu menu = new GenericMenu();

					PopulateGenericMenu<ItemData>(menu, GuaranteedItemAddHandler, o => {return LabelGetter(o, categoryPropertyItemData, displayNamePropertyItemData);});

					menu.ShowAsContext();
				}
				else{
					GuaranteedItemAddHandler(null);
				}
			};

			guaranteedItemsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = guaranteedItemsList.serializedProperty.GetArrayElementAtIndex(index);

				float totalWidth = GUILayoutUtility.GetLastRect().width + 20;
				int xOffset = 0;
				rect.y += 2;

				DrawLabel(rect, ref xOffset, "Item");
				xOffset += 11;
				DrawAssetObjectField<ItemData>(rect, ref xOffset, (int)((totalWidth - xOffset) * fieldWidth), element.FindPropertyRelative("itemData"));
				DrawLabel(rect, ref xOffset, "Amount");
				DrawField(rect, ref xOffset, (int)((totalWidth - xOffset - 46)), element.FindPropertyRelative("amount"));
			};

			// Chance items
			chanceItemsList = new ReorderableList(serializedObject, serializedObject.FindProperty("chanceItems"), true, true, true, true);

			chanceItemsList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Random items");
			};

			chanceItemsList.elementHeightCallback = (index) => {
				return (EditorGUIUtility.singleLineHeight + 5);
			};
			
			chanceItemsList.onAddCallback = (ReorderableList list) =>{
				list.serializedProperty.arraySize++;
				
				SerializedProperty spChanceItem = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
				spChanceItem.FindPropertyRelative("chance").floatValue = 1f;
			};

			chanceItemsList.onAddCallback = (ReorderableList list) => {
				if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ITEM_OPTIONS, true)){
					GenericMenu menu = new GenericMenu();

					PopulateGenericMenu<ItemData>(menu, ChanceItemAddHandler, o => {return LabelGetter(o, categoryPropertyItemData, displayNamePropertyItemData);});

					menu.ShowAsContext();
				}
				else{
					ChanceItemAddHandler(null);
				}
			};

			chanceItemsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = chanceItemsList.serializedProperty.GetArrayElementAtIndex(index);

				float totalWidth = GUILayoutUtility.GetLastRect().width + 20;
				int xOffset = 0;
				rect.y += 2;

				DrawLabel(rect, ref xOffset, "Item");
				xOffset += 11;
				DrawAssetObjectField<ItemData>(rect, ref xOffset, (int)((totalWidth - xOffset) * fieldWidth), element.FindPropertyRelative("itemData"));
				DrawLabel(rect, ref xOffset, "Chance");
				DrawField(rect, ref xOffset, (int)((totalWidth - xOffset - 46)), element.FindPropertyRelative("chance"));
			};

			// Item groups
			itemGroupsList = new ReorderableList(serializedObject, serializedObject.FindProperty("itemGroups"), true, true, true, true);

			itemGroupsList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Item groups");
			};

			itemGroupsList.elementHeightCallback = (index) => {
				if(InventoryGeneratorEditor.GroupSelectionMode == InventoryGenerator.GroupSelectMode.ItemsPerGroup){
					return (EditorGUIUtility.singleLineHeight * 3 + 9);
				}
				else{
					return (EditorGUIUtility.singleLineHeight + 5);
				}
			};

			itemGroupsList.onAddCallback = (ReorderableList list) =>{
				list.serializedProperty.arraySize++;

				SerializedProperty spItemGroup = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
				spItemGroup.FindPropertyRelative("minItems").intValue = 1;
				spItemGroup.FindPropertyRelative("maxItems").intValue = 1;
			};

			itemGroupsList.onAddCallback = (ReorderableList list) => {
				if(EditorPrefs.GetBool(AresPreferences.ARES_PREF_ADD_ITEM_GROUP_OPTIONS, true)){
					GenericMenu menu = new GenericMenu();

					PopulateGenericMenu<ItemGroup>(menu, ItemGroupAddHandler, o => displayNamePropertyItemGroup.GetValue(o, null).ToString());
					menu.ShowAsContext();
				}
				else{
					ItemGroupAddHandler(null);
				}
			};

			itemGroupsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
				SerializedProperty element = itemGroupsList.serializedProperty.GetArrayElementAtIndex(index);
				SerializedProperty spGroup = element.FindPropertyRelative("group");

				float totalWidth = GUILayoutUtility.GetLastRect().width + 20;
				int xOffset = 0;
				rect.y += 2;

				DrawLabel(rect, ref xOffset, "Group");
				DrawAssetObjectField<ItemGroup>(rect, ref xOffset, (int)((totalWidth - xOffset) * fieldWidth), spGroup);

				int col2X = xOffset;;
				DrawLabel(rect, ref xOffset, "Chance");
				DrawField(rect, ref xOffset, totalWidth - xOffset - 47, element.FindPropertyRelative("chance"));

				rect.y += EditorGUIUtility.singleLineHeight + 2;
				xOffset = 0;

				if(InventoryGeneratorEditor.GroupSelectionMode == InventoryGenerator.GroupSelectMode.ItemsPerGroup){
					DrawLabel(rect, ref xOffset, "Pick Between [");
					xOffset -= 5;

					float fieldWidthItems = Mathf.Max(((totalWidth - xOffset) * fieldWidth - 60) * .5f, 20);

					SerializedProperty spMinItems = element.FindPropertyRelative("minItems");
					SerializedProperty spMaxItems = element.FindPropertyRelative("maxItems");

					DrawField(rect, ref xOffset, fieldWidthItems, spMinItems);
					DrawLabel(rect, ref xOffset, "-");
					DrawField(rect, ref xOffset, fieldWidthItems, spMaxItems);
					xOffset -= 5;
					DrawLabel(rect, ref xOffset, "]");

					xOffset = col2X;
					
					int numItemsInGroup = spGroup.objectReferenceValue == null ? 0 : ((ItemGroup)spGroup.objectReferenceValue).ItemData.Length;

					if(numItemsInGroup >= spMaxItems.intValue){
						DrawLabel(rect, ref xOffset, "Allow duplicate picks");
						DrawField(rect, ref xOffset, 20, element.FindPropertyRelative("allowDuplicatePicks"));
					}
					else{
						GUI.enabled = false;
						DrawLabel(rect, ref xOffset, "Allow duplicate picks");
						EditorGUI.Toggle(new Rect(rect.x + xOffset, rect.y, 20, rect.height), true);
						GUI.enabled = true;
					}

					rect.y += EditorGUIUtility.singleLineHeight + 1;
					xOffset = 0;

					DrawLabel(rect, ref xOffset, string.Format("Group contains {0} items", numItemsInGroup), EditorStyles.miniLabel);

					if(numItemsInGroup < spMaxItems.intValue){
						xOffset = col2X;
						DrawLabel(rect, ref xOffset, "Some items may be reused.", EditorStyles.miniLabel);
					}
				}
			};
		}

		public string LabelGetter(object obj, PropertyInfo categoryPropertyInfo, PropertyInfo displayNamePropertyInfo){
			object path = categoryPropertyInfo.GetValue(obj, null);
			return (path == null ? "" : (path.ToString() + "/")) + displayNamePropertyInfo.GetValue(obj, null).ToString();
		}

		public override void OnInspectorGUI(){
			serializedObject.Update();

			SerializedProperty spChanceSelection = serializedObject.FindProperty("chanceSelection");
			SerializedProperty spGroupSelection = serializedObject.FindProperty("groupSelection");
			SerializedProperty spGroupSelectMode = serializedObject.FindProperty("groupSelectMode");

			GroupSelectionMode = (InventoryGenerator.GroupSelectMode)spGroupSelectMode.enumValueIndex;

//			EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);
//			EditorGUILayout.PropertyField(serializedObject.FindProperty("inventoryType"));
//
//			if(((InventoryBuilder)target).GetComponent<Actor>() != null){
//				EditorGUILayout.PropertyField(serializedObject.FindProperty("assignToActorOnAwake"));
//			}
//
//			EditorGUILayout.Space();

			int numChanceItems = serializedObject.FindProperty("chanceItems").arraySize;

			EditorGUILayout.LabelField("Random Items", EditorStyles.boldLabel);
			GUI.enabled = numChanceItems > 0;

			EditorGUILayout.PropertyField(spChanceSelection, new GUIContent("Random Item Selection Limit"));

			bool showAllowDuplicates = true;
			
			switch(spChanceSelection.enumValueIndex){
				case (int)InventoryGenerator.SelectionLimit.MinimumAmount:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minChanceItems"), new GUIContent("Minimum Random Items"));
					break;
				case (int)InventoryGenerator.SelectionLimit.MaximumAmount:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxChanceItems"), new GUIContent("Maximum Random Items"));
					break;
				case (int)InventoryGenerator.SelectionLimit.Range:
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Random items to pick", GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 4));
					EditorGUIUtility.labelWidth = 30;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minChanceItems"), new GUIContent("Min"), GUILayout.ExpandWidth(false));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxChanceItems"), new GUIContent("Max"), GUILayout.ExpandWidth(false));
					EditorGUIUtility.labelWidth = 0;
					EditorGUILayout.EndHorizontal();
					break;
				default:
					showAllowDuplicates = false;
					break;
			}

			if(showAllowDuplicates){
				if(numChanceItems >= serializedObject.FindProperty("minChanceItems").intValue || numChanceItems == 0){
					EditorGUILayout.PropertyField(serializedObject.FindProperty("allowChanceDuplicates"), new GUIContent("Allow Duplicate Random Items"));
				}
				else{
					GUI.enabled = false;
					EditorGUILayout.Toggle("Allow Duplicate Random Items", true);
					GUI.enabled = true;

					Rect allowChanceDuplicatesRect = GUILayoutUtility.GetLastRect();
					GUI.enabled = true;

					GUI.Label(new Rect(allowChanceDuplicatesRect.x + EditorGUIUtility.labelWidth + 16, allowChanceDuplicatesRect.y - 4, 400, EditorGUIUtility.singleLineHeight*2),
						string.Format("Some items have to reused.\n({0} items to pick from)", numChanceItems), EditorStyles.miniLabel);
				}

			}

			if(numChanceItems == 0){
				GUI.enabled = true;
				EditorGUILayout.LabelField("No random items assigned, settings will be ignored.", EditorStyles.miniLabel);
			}

			EditorGUILayout.Space();

			int numGroups = serializedObject.FindProperty("itemGroups").arraySize;

			GUI.enabled = true;
			EditorGUILayout.LabelField("Item Groups", EditorStyles.boldLabel);
			GUI.enabled = numGroups > 0;

			EditorGUILayout.PropertyField(spGroupSelectMode, new GUIContent("Pick From Group"));
			EditorGUILayout.PropertyField(spGroupSelection, new GUIContent("Group Selection Limit"));

			showAllowDuplicates = true;

			switch(spGroupSelection.enumValueIndex){
				case (int)InventoryGenerator.SelectionLimit.MinimumAmount:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minGroupPicks"), new GUIContent("Minimum Item Groups"));
					break;
				case (int)InventoryGenerator.SelectionLimit.MaximumAmount:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxGroupPicks"), new GUIContent("Maximum Item Groups"));
					break;
				case (int)InventoryGenerator.SelectionLimit.Range:
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Random groups to pick", GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 4));
					EditorGUIUtility.labelWidth = 30;
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minGroupPicks"), new GUIContent("Min"), GUILayout.ExpandWidth(false));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxGroupPicks"), new GUIContent("Max"), GUILayout.ExpandWidth(false));
					EditorGUIUtility.labelWidth = 0;
					EditorGUILayout.EndHorizontal();
					break;
				default:
					showAllowDuplicates = false;
					break;
			}

			if(showAllowDuplicates){
				if(numGroups >= serializedObject.FindProperty("minGroupPicks").intValue || numGroups == 0){
					EditorGUILayout.PropertyField(serializedObject.FindProperty("allowGroupDuplicates"), new GUIContent("Allow Duplicate Group Selections"));
				}
				else{
					GUI.enabled = false;
					EditorGUILayout.Toggle("Allow Duplicate Group Selections", true);
					GUI.enabled = true;

					Rect allowGroupDuplicatesRect = GUILayoutUtility.GetLastRect();
					GUI.enabled = true;

					GUI.Label(new Rect(allowGroupDuplicatesRect.x + EditorGUIUtility.labelWidth + 16, allowGroupDuplicatesRect.y - 4, 400, EditorGUIUtility.singleLineHeight*2),
						string.Format("Some groups have to reused.\n({0} groups to pick from)", numGroups), EditorStyles.miniLabel);
				}
			}

			if(numGroups == 0){
				GUI.enabled = true;
				EditorGUILayout.LabelField("No item groups assigned, settings will be ignored.", EditorStyles.miniLabel);
			}

			EditorGUILayout.Space();

			guaranteedItemsList.DoLayoutList();
			EditorGUILayout.Space();

			chanceItemsList.DoLayoutList();
			EditorGUILayout.Space();

			itemGroupsList.DoLayoutList();

			serializedObject.ApplyModifiedProperties();

			// Debug functionality
			EditorGUILayout.LabelField("Mocking", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();
			if(GUILayout.Button("Generate mock inventory", GUILayout.Width(150))){
				if(mockInventoryType == InventoryGenerator.InventoryType.Linear){
					mockInventory = ((InventoryGenerator)target).GenerateInventory<LinearInventory>();
				}
				else{
					mockInventory = ((InventoryGenerator)target).GenerateInventory<StackedInventory>();
				}
			}
			mockInventoryType = (InventoryGenerator.InventoryType)EditorGUILayout.EnumPopup(mockInventoryType, GUILayout.Width(60));
			GUILayout.EndHorizontal();
			
			if(mockInventory != null){
				if(mockInventory != null){
					EditorGUILayout.HelpBox(mockInventory.ToString(), MessageType.None);
				}
			}
		}

		void GuaranteedItemAddHandler(object target){
			guaranteedItemsList.serializedProperty.arraySize++;

			SerializedProperty spGuaranteedItem = guaranteedItemsList.serializedProperty.GetArrayElementAtIndex(guaranteedItemsList.serializedProperty.arraySize - 1);

			spGuaranteedItem.FindPropertyRelative("itemData").objectReferenceValue = (ItemData)target;
			spGuaranteedItem.FindPropertyRelative("amount").intValue = 1;

			serializedObject.ApplyModifiedProperties();
		}

		void ChanceItemAddHandler(object target){
			chanceItemsList.serializedProperty.arraySize++;

			SerializedProperty spChanceItem = chanceItemsList.serializedProperty.GetArrayElementAtIndex(chanceItemsList.serializedProperty.arraySize - 1);

			spChanceItem.FindPropertyRelative("itemData").objectReferenceValue = (ItemData)target;
			spChanceItem.FindPropertyRelative("chance").floatValue = 1f;

			serializedObject.ApplyModifiedProperties();
		}

		void ItemGroupAddHandler(object target){
			itemGroupsList.serializedProperty.arraySize++;

			SerializedProperty spItemGroup = itemGroupsList.serializedProperty.GetArrayElementAtIndex(itemGroupsList.serializedProperty.arraySize - 1);

			spItemGroup.FindPropertyRelative("group").objectReferenceValue = (ItemGroup)target;
			spItemGroup.FindPropertyRelative("chance").floatValue = 1f;
			spItemGroup.FindPropertyRelative("minItems").intValue = 1;
			spItemGroup.FindPropertyRelative("maxItems").intValue = target == null ? 1 : ((ItemGroup)target).ItemData.Length;
			spItemGroup.FindPropertyRelative("allowDuplicatePicks").boolValue = false;

			serializedObject.ApplyModifiedProperties();
		}
	}
}