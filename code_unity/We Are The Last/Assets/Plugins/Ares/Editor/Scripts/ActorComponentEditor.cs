using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Ares.ActorComponents;

namespace Ares.Editor {
	public struct QuickMenuAfflictionEntry{
		public Object @object;
		public int amount;

		public QuickMenuAfflictionEntry(Object @object, int amount){
			this.@object = @object;
			this.amount = amount;
		}
	}

	public abstract class ActorComponentEditor : UnityEditor.Editor {
		public static float labelWidthSmall = 40f;
		public static Actor actor;

		internal static string currentAbilityName;
		internal static bool currentAbilityEnabled;
		internal static bool currentAbilityIsFallback;

		protected readonly string[] hiddenProperties = new string[]{"m_Script", "defaultAbilityCallback", "defaultItemCallback", "defaultAfflictionCallback"};

		protected ReorderableList rlAbilityCallbacks;
		protected ReorderableList rlEventCallbacks;
		protected ReorderableList rlAfflictionCallbacks;
		protected ReorderableList rlItemCallbacks;
		
		protected SerializedProperty spAbilityCallbacks;
		protected SerializedProperty spEventCallbacks;
		protected SerializedProperty spAfflictionCallbacks;
		protected SerializedProperty spItemCallbacks;
		
		SerializedProperty spShowEvents;
		SerializedProperty spShowAbilities;
		SerializedProperty spItems;
		SerializedProperty spAfflictions;
		
		MethodInfo doListHeader;

		protected virtual void OnEnable(){
			((ActorComponent)target).SetupEventCallbacks();

			doListHeader = typeof(ReorderableList).GetMethod("DoListHeader", BindingFlags.NonPublic | BindingFlags.Instance); 
			
			spShowEvents = serializedObject.FindProperty("showEvents");
			spShowAbilities = serializedObject.FindProperty("showAbilities");
			spItems = serializedObject.FindProperty("showItems");
			spAfflictions = serializedObject.FindProperty("showAfflictions");
			
			spAbilityCallbacks = serializedObject.FindProperty("abilityCallbacks");
			spEventCallbacks = serializedObject.FindProperty("eventCallbacks");
			spAfflictionCallbacks = serializedObject.FindProperty("afflictionCallbacks");
			spItemCallbacks = serializedObject.FindProperty("itemCallbacks");
			
			rlAbilityCallbacks = new ReorderableList(serializedObject, spAbilityCallbacks, false, false, false, false);
			rlAbilityCallbacks.drawElementCallback += DrawAbilityCallback;

			rlEventCallbacks = new ReorderableList(serializedObject, spEventCallbacks, false, false, false, false);
			rlEventCallbacks.drawElementCallback += DrawEventCallback;
			
			rlAfflictionCallbacks = new ReorderableList(serializedObject, spAfflictionCallbacks, true, false, true, true);
			rlAfflictionCallbacks.drawElementCallback += DrawAfflictionCallback;
			rlAfflictionCallbacks.onAddDropdownCallback += OnAfflictionAddClicked;
			
			rlItemCallbacks = new ReorderableList(serializedObject, spItemCallbacks, true, false, true, true);
			rlItemCallbacks.drawElementCallback += DrawItemCallback;
			rlItemCallbacks.onAddDropdownCallback += OnItemAddClicked;
		}
		
		protected virtual void OnDisable(){
			rlAbilityCallbacks.drawElementCallback -= DrawAbilityCallback;
			rlEventCallbacks.drawElementCallback -= DrawEventCallback;
			rlAfflictionCallbacks.drawElementCallback -= DrawAfflictionCallback;
			rlItemCallbacks.drawElementCallback -= DrawItemCallback;
			rlAfflictionCallbacks.onAddDropdownCallback -= OnAfflictionAddClicked;
			rlItemCallbacks.onAddDropdownCallback -= OnItemAddClicked;
		}

		void OnAfflictionAddClicked(Rect buttonRect, ReorderableList list){
			ShowCallbackAddMenu(typeof(AfflictionData), AfflictionClickHandler);
		}
		
		protected virtual void AfflictionClickHandler(object target){
			CallbackMenuClickHandler(target, rlAfflictionCallbacks, "affliction");
		}
		
		void OnItemAddClicked(Rect buttonRect, ReorderableList list){
			ShowCallbackAddMenu(typeof(ItemData), ItemClickHandler);
		}
		
		protected virtual void ItemClickHandler(object target){
			CallbackMenuClickHandler(target, rlItemCallbacks, "item");
		}

		void ShowCallbackAddMenu(System.Type filter, GenericMenu.MenuFunction2 clickHandler){
//			PropertyInfo displayNameProperty = filter.GetProperty("DisplayName");
			GenericMenu menu = new GenericMenu();
			System.Func<Object, GUIContent> labelGetter;

			menu.AddItem(new GUIContent("Default"), false, clickHandler, null);
			menu.AddSeparator("");

			string propName = filter == typeof(AfflictionData) ? "affliction" : "item";
			List<string> guids = AssetDatabase.FindAssets("t:"+filter.Name).ToList();

			SerializedProperty spAfflictionCallbacks = serializedObject.FindProperty(propName + "Callbacks");
			Dictionary<string, int> guidValues = new Dictionary<string, int>(); //<5 means at least 1 more can be added (n occurences), 5 occurences or an "always" type means it can't

			if(filter == typeof(AfflictionData)){
				labelGetter = LabelGetterAfflictionData;

				for(int i = 0; i < spAfflictionCallbacks.arraySize; i++){
					SerializedProperty spElem = spAfflictionCallbacks.GetArrayElementAtIndex(i);
					string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spElem.FindPropertyRelative(propName).objectReferenceValue));
					int value = spElem.FindPropertyRelative("type").enumValueIndex == (int)AfflictionCallbackType.Always ? 5 : 1;

					if(value >= 5){
						guids.Remove(guid);
						continue;
					}

					if(guidValues.ContainsKey(guid)){
						guidValues[guid] += value;
					}
					else{
						guidValues.Add(guid, value);
					}

					if(guidValues[guid] == 5){
						guids.Remove(guid);
					}
				}
			}
			else{
				if(filter == typeof(AbilityData)){
					labelGetter = LabelGetterAbilityData;
				}
			   else{
					labelGetter = LabelGetterItemData;
				}
//				labelGetter = (filter == typeof(AbilityData)) ? LabelGetterAbilityData : LabelGetterItemData;

				for(int i = 0; i < spAfflictionCallbacks.arraySize; i++){
					string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spAfflictionCallbacks.GetArrayElementAtIndex(i).FindPropertyRelative(propName).objectReferenceValue));
					if(guids.Any(id => id == guid)){
						guids.Remove(guid);
					}
				}
			}
			
			foreach(string guid in guids){
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Object obj = AssetDatabase.LoadAssetAtPath(path, filter);
//				GUIContent name = labelGetter(obj);

				if(filter == typeof(AfflictionData)){
					AfflictionData afflictionData = (AfflictionData)obj;
//					string name = afflictionData.DisplayName;
					menu.AddItem(new GUIContent(afflictionData.DisplayName + " (single)"), false, clickHandler, new QuickMenuAfflictionEntry(obj, 1));
					
					int num = 5;

					if(guidValues.ContainsKey(guid) && guidValues[guid] < 5){
						num = 5 - guidValues[guid];
					}

					menu.AddItem(new GUIContent(afflictionData.DisplayName + " (all missing)"), false, clickHandler, new QuickMenuAfflictionEntry(obj, num));
				}
				else{
					menu.AddItem(labelGetter(obj), false, clickHandler, new QuickMenuAfflictionEntry(obj, 1));
				}
			}

			menu.ShowAsContext();
		}

		GUIContent LabelGetterAbilityData(Object obj){
			AbilityData abilityData = (AbilityData)obj;
			string categoryPath = abilityData.FullCategoryPath;
			return new GUIContent(string.IsNullOrEmpty(categoryPath) ? abilityData.DisplayName : (categoryPath + "/" + abilityData.DisplayName));
		}

		GUIContent LabelGetterItemData(Object obj){
			ItemData itemData = (ItemData)obj;
			string categoryPath = itemData.FullCategoryPath;
			return new GUIContent(string.IsNullOrEmpty(categoryPath) ? itemData.DisplayName : (categoryPath + "/" + itemData.DisplayName));
		}

		GUIContent LabelGetterAfflictionData(Object obj){
			AfflictionData afflictionData = (AfflictionData)obj;
			Debug.Log(afflictionData.DisplayName);
			return new GUIContent(afflictionData.DisplayName);
		}

		void CallbackMenuClickHandler(object target, ReorderableList list, string property){
			if(property == "affliction"){
				SerializedProperty spAfflictionCallbacks = serializedObject.FindProperty("afflictionCallbacks");

				List<int> typesToAdd = new List<int>();
				typesToAdd.Add(0);
				typesToAdd.Add(1);
				typesToAdd.Add(2);
				typesToAdd.Add(3);
				typesToAdd.Add(4);

				QuickMenuAfflictionEntry entry = target == null ? new QuickMenuAfflictionEntry(null, 1) : (QuickMenuAfflictionEntry)target;

				for(int i = 0; i < spAfflictionCallbacks.arraySize; i++){
					SerializedProperty spElem = spAfflictionCallbacks.GetArrayElementAtIndex(i);

					if(spElem.FindPropertyRelative(property).objectReferenceValue == entry.@object){
						typesToAdd.Remove(spElem.FindPropertyRelative("type").enumValueIndex);
					}
				}

				SerializedProperty spElement;

				for(int i = 0; i < entry.amount; i++){
					spElement = AddCallbackElementWithDefaultPropertyValue(entry.@object, list, property);
					spElement.FindPropertyRelative("type").enumValueIndex = typesToAdd[i];

					OnAfflictionAdd(spElement);
				}


			}
			else{
				Object obj = target == null ? null : ((QuickMenuAfflictionEntry)target).@object;
				
				AddCallbackElementWithDefaultPropertyValue(obj, list, property);
			}

			serializedObject.ApplyModifiedProperties();
		}

		SerializedProperty AddCallbackElementWithDefaultPropertyValue(object obj, ReorderableList list, string property){
			int index = list.count;
			
			list.serializedProperty.arraySize++;

			SerializedProperty spElement = list.serializedProperty.GetArrayElementAtIndex(index);

				spElement.FindPropertyRelative(property).objectReferenceValue = (Object)obj; //real one

			return spElement;
		}

		void DrawAbilityCallback(Rect rect, int index, bool active, bool focused){
//			actor = ((MonoBehaviour)target).GetComponent<Actor>();

			if(index == actor.Abilities.Length && actor.FallbackAbility != null && actor.FallbackAbility.Data != null){
				currentAbilityName = actor.FallbackAbility.Data.DisplayName;
				currentAbilityEnabled = actor.FallbackAbility.Enabled;
				currentAbilityIsFallback = true;

				EditorGUI.PropertyField(rect, serializedObject.FindProperty("abilityCallbacks").GetArrayElementAtIndex(index));
			}
			else if(index < actor.Abilities.Length && actor.Abilities[index].Data != null){
				currentAbilityName = actor.Abilities[index].Data.DisplayName;
				currentAbilityEnabled = actor.Abilities[index].Enabled;
				currentAbilityIsFallback = false;

				EditorGUI.PropertyField(rect, serializedObject.FindProperty("abilityCallbacks").GetArrayElementAtIndex(index));
			}
		}
		
		void DrawEventCallback(Rect rect, int index, bool active, bool focused){
			EditorGUI.PropertyField(rect, spEventCallbacks.GetArrayElementAtIndex(index));
		}
		
		void DrawAfflictionCallback(Rect rect, int index, bool active, bool focused){
			EditorGUI.PropertyField(rect, spAfflictionCallbacks.GetArrayElementAtIndex(index));
		}
		
		void DrawItemCallback(Rect rect, int index, bool active, bool focused){
			EditorGUI.PropertyField(rect, serializedObject.FindProperty("itemCallbacks").GetArrayElementAtIndex(index));
		}

		public override void OnInspectorGUI(){
			actor = ((MonoBehaviour)target).GetComponent<Actor>();

			EnsureAbilityCount();
			DrawDefaultInspector();
		}

		void EnsureAbilityCount(){
			if(spAbilityCallbacks != null && spAbilityCallbacks.arraySize != actor.Abilities.Length){
				spAbilityCallbacks.arraySize = actor.Abilities.Length;
				spAbilityCallbacks.serializedObject.ApplyModifiedProperties();
			}
		}

		protected void DrawLists(){
			Rect rect;

			DrawList(rlEventCallbacks, !spShowEvents.boolValue, out rect);
			spShowEvents.boolValue = EditorGUI.Foldout(rect, spShowEvents.boolValue, "Events");

			DrawList(rlAbilityCallbacks, !spShowAbilities.boolValue, out rect);
			spShowAbilities.boolValue = EditorGUI.Foldout(rect, spShowAbilities.boolValue, "Abilities");
//			DrawDefaultEffect(spShowAbilities.boolValue, serializedObject.FindProperty("defaultAbilityCallback"));
			
			DrawList(rlAfflictionCallbacks, !spAfflictions.boolValue, out rect);
			spAfflictions.boolValue = EditorGUI.Foldout(rect, spAfflictions.boolValue, "Afflictions");
//			DrawDefaultEffect(spAfflictions.boolValue, serializedObject.FindProperty("defaultAfflictionCallback"));
			
			DrawList(rlItemCallbacks, !spItems.boolValue, out rect);
			spItems.boolValue = EditorGUI.Foldout(rect, spItems.boolValue, "Items");
//			DrawDefaultEffect(spItems.boolValue, serializedObject.FindProperty("defaultItemCallback"));
		}

//		void DrawDefaultEffect(bool shouldDraw, SerializedProperty property){
//			if(spShowAbilities.boolValue){
//				Rect defaultRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
//				defaultRect.y -= EditorGUIUtility.singleLineHeight * .5f;
//
//				EditorGUI.DrawRect(defaultRect, AresEditor.FallbackFieldBackgroundColor);
//				EditorGUI.PropertyField(defaultRect, property);
//			}
//		}

		protected void DrawList(ReorderableList list, bool headerOnly, out Rect headerLabelPosition){
			if(headerOnly){
				headerLabelPosition = GUILayoutUtility.GetRect(0f, list.headerHeight, new GUILayoutOption[]{GUILayout.ExpandWidth(true)});
				
				doListHeader.Invoke(list, new object[]{headerLabelPosition});
				
				headerLabelPosition.x += 18;
				headerLabelPosition.y += 1;

			}
			else{
				headerLabelPosition = GUILayoutUtility.GetRect(0f, 0f, new GUILayoutOption[]{GUILayout.ExpandWidth(true)});
				list.DoLayoutList();

				bool hasFooter = list.displayAdd || list.displayRemove;
				
				headerLabelPosition.y += 2;
				headerLabelPosition.x += 18;
				headerLabelPosition.height = EditorGUIUtility.singleLineHeight;

				if(hasFooter){
					EditorGUILayout.Space();
					EditorGUILayout.Space();
				}
			}
		}

		protected void ShowEventTargetWarningIfNecessary(){
			SerializedProperty spEventCallbacks = rlEventCallbacks.serializedProperty;

			for(int i=0; i<spEventCallbacks.arraySize; i++){
				if(spEventCallbacks.GetArrayElementAtIndex(i).FindPropertyRelative("effect.enabled").boolValue){
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("Notice", EditorStyles.boldLabel);
					EditorGUILayout.HelpBox("For Event effects, the Target and Caster play positions both point to the actor this component is attached to.", MessageType.Info);
					break;
				}
			}
		}

		protected abstract void OnAfflictionAdd(SerializedProperty property);
	}

	public abstract class ActorCallbackElementDrawerBase : PropertyDrawer {
		public static int lineSpacing = 6;
		public static int elementSpacing = 4;

		protected SerializedProperty spElemEnabled;
		protected SerializedProperty spElemExpanded;
		
		protected float spacing = 6f;
		protected float thirdWidth;
		protected Rect curPosition;
		protected float dragHandleCorrection = 14f;
		protected float paramWidthShift;
		
		// Normally the individual draw code for Abilities, Afflictions and Items would go into their respective
		// property drawers. However, keeping them here makes it easier to create new callback components, and prevents
		// the exact same boilerplate code from being needed on each individual property drawer.
		// While individual drawer classes are still needed for each component, they now only contain a single call in
		// their OnGUI() methods, keeping the actual drawing code centralized here, and shared between all drawers.

		protected void DrawAbility(Rect position, SerializedProperty property, GUIContent label, string enabledLocation = ""){
			position.x += dragHandleCorrection;
			position.width -= dragHandleCorrection;

			EditorGUI.BeginProperty(position, label, property);

			BeginDraw(position, property, enabledLocation);

			float labelWidth = EditorStyles.label.CalcSize(new GUIContent(ActorAnimationEditor.currentAbilityName)).x;
			float labelWidth2 = ActorAnimationEditor.currentAbilityEnabled ? 0f : 50f; //from EditorStyles.miniLabel.CalcSize(new GUIContent("(Disabled)")).x

			if(ActorComponentEditor.currentAbilityIsFallback){
				Color oldMiniLabelColor = EditorStyles.miniLabel.normal.textColor;
				EditorStyles.miniLabel.normal.textColor = new Color(.3f, .3f, .3f, 1f);

				GUI.Label(new Rect(curPosition.x + labelWidth, curPosition.y, labelWidth + 18, EditorGUIUtility.singleLineHeight), "(Fallback Ability)", EditorStyles.miniLabel);

				EditorStyles.miniLabel.normal.textColor = oldMiniLabelColor;

				labelWidth2 += 29f;
			}
			else if(!ActorComponentEditor.currentAbilityEnabled){
				Color oldMiniLabelColor = EditorStyles.miniLabel.normal.textColor;
				EditorStyles.miniLabel.normal.textColor = new Color(.3f, .3f, .3f, 1f);

				GUI.Label(new Rect(curPosition.x + labelWidth, curPosition.y, labelWidth + 18, EditorGUIUtility.singleLineHeight), "(Disabled)", EditorStyles.miniLabel);

				EditorStyles.miniLabel.normal.textColor = oldMiniLabelColor;
			}
			
			if(GUI.Button(new Rect(curPosition.x, curPosition.y, labelWidth + labelWidth2 + 18, EditorGUIUtility.singleLineHeight),
				ActorComponentEditor.currentAbilityName, EditorStyles.label) && spElemExpanded != null){
				spElemExpanded.boolValue = !spElemExpanded.boolValue;
			}

			if(spElemExpanded != null){
				GUI.Toggle(new Rect(curPosition.x + labelWidth + labelWidth2, curPosition.y, 22, EditorGUIUtility.singleLineHeight),
					spElemExpanded.boolValue, GUIContent.none, EditorStyles.foldout);
			}
			
			if(spElemExpanded == null || spElemExpanded.boolValue){
				DrawSpecifics(position, property);
			}

			EditorGUI.EndProperty();
		}

		protected void DrawEvent(Rect position, SerializedProperty property, GUIContent label, string enabledLocation = ""){
			position.x += dragHandleCorrection;
			position.width -= dragHandleCorrection;

			SerializedProperty spType = property.FindPropertyRelative("type");

			EditorGUI.BeginProperty(position, label, property);

			BeginDraw(position, property, enabledLocation);

			float labelWidth = EditorStyles.label.CalcSize(new GUIContent(spType.enumDisplayNames[spType.enumValueIndex])).x;

			if(GUI.Button(new Rect(curPosition.x, curPosition.y, labelWidth + 18, EditorGUIUtility.singleLineHeight),
				spType.enumDisplayNames[spType.enumValueIndex], EditorStyles.label) && spElemExpanded != null){
				spElemExpanded.boolValue = !spElemExpanded.boolValue;
			}

			if(spElemExpanded != null){
				GUI.Toggle(new Rect(curPosition.x + labelWidth, curPosition.y, 22, EditorGUIUtility.singleLineHeight),
					spElemExpanded.boolValue, GUIContent.none, EditorStyles.foldout);
			}

			if(spElemExpanded == null || spElemExpanded.boolValue){
				DrawSpecifics(position, property);
			}

			EditorGUI.EndProperty();
		}
		
		protected void DrawAffliction(Rect position, SerializedProperty property, GUIContent label, string enabledLocation = ""){
			SerializedProperty spAffliction = property.FindPropertyRelative("affliction");
			SerializedProperty spType = property.FindPropertyRelative("type");

			EditorGUI.BeginProperty(position, label, property);

			BeginDraw(position, property, enabledLocation);
			curPosition.height = EditorGUIUtility.singleLineHeight;

			spAffliction.objectReferenceValue = EditorGUI.ObjectField(curPosition, "", spAffliction.objectReferenceValue, typeof(AfflictionData), false);
			curPosition.x += curPosition.width + 6;
			curPosition.width = 88 - paramWidthShift;

			EditorGUI.PropertyField(curPosition, spType, GUIContent.none);
			curPosition.x += 5;

			if(spElemExpanded != null){
				spElemExpanded.boolValue = GUI.Toggle(new Rect(curPosition.x + curPosition.width, curPosition.y, 22, EditorGUIUtility.singleLineHeight),
					spElemExpanded.boolValue, GUIContent.none, EditorStyles.foldout);
			}
			
			if(spElemExpanded == null || spElemExpanded.boolValue){
				DrawSpecifics(position, property);
			}

			EditorGUI.EndProperty();
		}
		
		protected void DrawItem(Rect position, SerializedProperty property, GUIContent label, string enabledLocation = ""){
			SerializedProperty spItem = property.FindPropertyRelative("item");
			EditorGUI.BeginProperty(position, label, property);

			BeginDraw(position, property, enabledLocation);
			curPosition.height = EditorGUIUtility.singleLineHeight;

			spItem.objectReferenceValue = EditorGUI.ObjectField(curPosition, "", spItem.objectReferenceValue, typeof(ItemData), false);

			if(spElemExpanded != null){
				spElemExpanded.boolValue = GUI.Toggle(new Rect(curPosition.x + curPosition.width, curPosition.y, 22, EditorGUIUtility.singleLineHeight),
					spElemExpanded.boolValue, GUIContent.none, EditorStyles.foldout);
			}
			
			if(spElemExpanded == null || spElemExpanded.boolValue){
				DrawSpecifics(position, property);
			}

			EditorGUI.EndProperty();
		}
		
		void BeginDraw(Rect position, SerializedProperty property, string enabledLocation = ""){
			if(enabledLocation == string.Empty){
				spElemEnabled = property.FindPropertyRelative("enabled");
			}
			else{
				spElemEnabled = property.FindPropertyRelative(enabledLocation + ".enabled");
			}

			spElemExpanded = property.FindPropertyRelative("editorShowExpanded");
			
			thirdWidth = position.width / 3f - spacing * 2;
			curPosition = new Rect(position.x, position.y, 14, position.height);
			paramWidthShift = Mathf.Clamp((330 - position.width) * .5f, 0f, 40f);
			
			EditorGUIUtility.labelWidth = ActorAnimationEditor.labelWidthSmall;

			spElemEnabled.boolValue = EditorGUI.Toggle(curPosition, spElemEnabled.boolValue);
			
			curPosition = new Rect(position.x + 19, position.y, thirdWidth - 19 - dragHandleCorrection, position.height);
		}

		protected void DrawEventIgnoreFlags(SerializedProperty property, Rect position, float labelWidth){
			EventCallbackType eventType = (EventCallbackType)property.FindPropertyRelative("type").intValue;

			if(eventType != EventCallbackType.TakeDamage && eventType != EventCallbackType.Heal && eventType != EventCallbackType.BuffStat &&
				eventType != EventCallbackType.DebuffStat){
				return;
			}

			SerializedProperty spIgnoreEvents = property.FindPropertyRelative("ignoreEvents");

			if(spIgnoreEvents != null){
				EditorGUIUtility.labelWidth = labelWidth;
				EditorGUI.PropertyField(position, spIgnoreEvents, new GUIContent("Ignore Own"));
				EditorGUIUtility.labelWidth = 0f;
			}
		}

		protected abstract void DrawSpecifics(Rect position, SerializedProperty property);
	}
}