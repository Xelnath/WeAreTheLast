using UnityEngine;
using UnityEditor;
using Quantum;
using Sini.Unity;
using System.Linq;

namespace Atomech
{
	[CustomPropertyDrawer(typeof(LocalizationRecord))]
	public class LocalizationRecordDrawer : PropertyDrawer
	{
		
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var localizedAttribute = (LocalizedAttribute) fieldInfo.GetCustomAttributes(typeof(LocalizedAttribute), true).FirstOrDefault();

			if ( localizedAttribute != null ) {
				return base.GetPropertyHeight(property, label) * (localizedAttribute.Lines + 2);
			}
			return base.GetPropertyHeight(property, label) * 3f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            //if ( property.objectReferenceValue == null )
            //{
            //    EditorGUI.LabelField(position, "This is not a serialized object");
            //    return;
            //}

			var fieldInfo = this.fieldInfo;
			var localizedAttribute = (LocalizedAttribute)fieldInfo.GetCustomAttributes(typeof(LocalizedAttribute), true).FirstOrDefault();
			var localizeFromAttr = (LocalizeFromAttribute)fieldInfo.GetCustomAttributes(typeof(LocalizeFromAttribute), true).FirstOrDefault();

            ScriptableObject parentAsset = null;
            if (property.serializedObject.targetObject is ScriptableObject)
            {
                parentAsset = (ScriptableObject)property.serializedObject.targetObject;
            }
			var localizationRecord = (LocalizationRecord)property.objectReferenceValue;

			var path = property.propertyPath;
			var lastDot = path.LastIndexOf('.');
			var pathSnip = lastDot != -1 ? path.Substring(0, lastDot) : "";
			var localizeFromPath = pathSnip + localizeFromAttr?.Field ?? "";

			SerializedProperty fromPath = property.serializedObject.FindProperty(localizeFromPath);

			string oldValue = "";
			string newValue = null;

			var p = position.WithHeight(18);
			property.FindPropertyRelative("");

			// Restore for debugging
			//GUI.Label(p, pathSnip);
			//p = p.BelowSelf();
			EditorGUI.PropertyField(p, property, label);

			p = p.BelowSelf().Translate(0,2);

            if (localizationRecord?.Local?.English != null) oldValue = localizationRecord.Local.English;
            else if (!string.IsNullOrWhiteSpace(localizeFromPath)) oldValue = fromPath?.stringValue;

            GUI.enabled = localizationRecord != null;
			bool wrap = EditorStyles.textField.wordWrap;
			EditorStyles.textField.wordWrap = true;
			p = p.WithHeight(16f * (localizedAttribute?.Lines ?? 2));
			newValue = EditorGUI.TextArea(p, oldValue);
			EditorStyles.textField.wordWrap = wrap;
			if (oldValue != newValue && localizationRecord != null) {
				localizationRecord.Local.English = newValue;
				EditorUtility.SetDirty(localizationRecord);
			}
			GUI.enabled = true;

            var sheet = LocalizationRecords.GetLocalizationPage(parentAsset, localizationRecord, localizedAttribute, out var suggestedPage);
            string lookupPath;
            if (parentAsset != null)
            {
                lookupPath = LocalizationRecords.GenerateLookupPathGuid(parentAsset, localizationRecord, localizedAttribute, property.name);
            }
            else
            {
                var o = (MonoBehaviour)property.serializedObject.targetObject;
                lookupPath = LocalizationRecords.GenerateSceneLookupPathGuid(
                    o.gameObject, 
                    localizationRecord, localizedAttribute, property.name);
            }

            p = p.BelowSelf().WithHeight(16f);
			if (localizationRecord == null) {


                GUI.enabled = (parentAsset is ScriptableObject);

				if (GUI.Button(p.WithWidth(p.width*0.75f), lookupPath)) {
					var lr2 = CreateChildRecord(sheet, 
						locID: lookupPath, 
						childName: lookupPath, 
						parentAsset);

					lr2.Local.English = oldValue;
					EditorUtility.SetDirty(lr2);
					property.objectReferenceValue = lr2;
				}
				GUI.enabled = true;
				if (GUI.Button(p.Translate(p.width*0.75f,0).WithWidth(p.width*0.25f), "Create Loose")) {
					var lr2 = CreateLooseRecord(sheet, lookupPath);

					lr2.Local.English = oldValue;
					EditorUtility.SetDirty(lr2);
					property.objectReferenceValue = lr2;
				}
			}
			else {
				var p2 = p.WithWidth(p.width/4f).Translate((p.width/4f)-8f, 0);
				if (GUI.Button(p2, "Upload")) {
                    LocalizationRecordHelper.PushAssetRecord(localizationRecord);
				}
				GUI.enabled = (!string.IsNullOrWhiteSpace(localizationRecord.MetaDataRemote.ID));
				p2 = p2.RightOfSelf().Translate(4f, 0);
				if (GUI.Button(p2, "Update")) {
                    LocalizationRecordHelper.FetchAssetRecord(localizationRecord);
				}
				GUI.enabled = true;
				p2 = p2.RightOfSelf().Translate(4f, 0);
				if (GUI.Button(p2, "Select")) {
					Selection.activeObject = localizationRecord;

				}
			}

		
		}

		// Temporary until I figure out how to do this from the 'inside' of a drawer
		public static bool ShowInternals = false;
		public static string LocalizationPath = "Assets/localization";

		public static void DrawLocalizationRecord(Rect rect, SerializedProperty prop, LocalizedAttribute localizedData, string rootString, string childName, ScriptableObject asset) {
			var lr = prop.objectReferenceValue as LocalizationRecord;
			if (lr != null) {
				Rect select = rect.WithWidth(20f).InnerAlignWithCenterRight(rect);
				Rect field = rect.SquashRight(20f);

				if (GUI.Button(select, "S")) {
					Selection.activeObject = lr;
				}
				string oldValue = lr.Local.English;
				string newValue = null;

				if (localizedData.Lines <= 1) {
					newValue = EditorGUI.TextField(field, ObjectNames.NicifyVariableName(prop.name), oldValue);
				}
				else {
					GUI.Label(field.WithHeight(20f), ObjectNames.NicifyVariableName(prop.name));
					bool wrap = EditorStyles.textField.wordWrap;
					EditorStyles.textField.wordWrap = true;
					newValue = EditorGUI.TextArea(field.SquashTop(20f), oldValue);
					EditorStyles.textField.wordWrap = wrap;
				}

				if (oldValue != newValue) {
					//Log.Info($"{prop.name}-{lr.name} in {lr.SheetName} ({rootString}.{localizedData?.Key}) set to {newValue}.");
					lr.Local.English = newValue;
					EditorUtility.SetDirty(lr);
				}
			}
			else {
				string id = $"{rootString}.{localizedData?.Key}";
				if (GUI.Button(rect, "Create " + id)) {
					//var lr2 = CreateLooseRecord(localizedData.Sheet, id);
					var lr2 = CreateChildRecord(localizedData.Sheet, id, childName, asset);
					prop.objectReferenceValue = lr2;
				}
			}
		}

		public static LocalizationRecord CreateLooseRecord(string sheetName, string locID) {
			string[] path = locID.Split('.');

			string folder = LocalizationPath;

			if (!AssetDatabase.IsValidFolder(folder)) {
				AssetDatabase.CreateFolder("Assets", "localization");
			}

			for (int i = 0; i < path.Length - 1; ++i) {
				var nextFolder = $"{folder}/{path[i]}";
				if (!AssetDatabase.IsValidFolder(nextFolder)) {
					AssetDatabase.CreateFolder(folder, path[i]);
				}

				folder = nextFolder;
			}

			UnityEngine.Debug.Log($"Asset destination is {folder}. Filename is {locID}");

			LocalizationRecord lr = ScriptableObject.CreateInstance(typeof(LocalizationRecord)) as LocalizationRecord;
			lr.Init();
			lr.SheetName = sheetName;
			lr.MetaDataLocal.ID = locID;
			lr.name = locID;
			AssetDatabase.CreateAsset(lr, $"{folder}/{locID}.asset");
			return lr;
		}

		public static LocalizationRecord CreateChildRecord(string sheetName, string locID, string childName, ScriptableObject parent) {
			var path = AssetDatabase.GetAssetPath(parent);
			var objects = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
			for (int i = 0; i < objects.Length; ++i) {
				var obj = objects[i];

				if (obj is LocalizationRecord locRecord) {
					if (obj.name == childName || locRecord.MetaDataLocal.ID == locID) {
						return locRecord;
					}
				}
			}

			LocalizationRecord lr = ScriptableObject.CreateInstance(typeof(LocalizationRecord)) as LocalizationRecord;
			lr.Init();
			lr.SheetName = sheetName;
			lr.MetaDataLocal.ID = locID;
			lr.name = locID;
			AssetDatabase.AddObjectToAsset(lr, parent);
			AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(parent));

			return lr;
		}
	}
}
