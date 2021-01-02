using System;
using System.Globalization;
using System.Linq;
using GoogleSheetsForUnity;
using Quantum;
using UnityEditor;
using UnityEngine;


/// <summary>
/// This is a quick and dirty uploader/downloader to allow Joe to modify data
/// in Google Sheets. Eventually, a nicer version should be made with version tracking,
/// etc, but this is a lot of work and it didn't seem as urgent yet.
/// </summary>

[CreateAssetMenu(fileName = "GoogleSheetDriveConnectionData", menuName = "Google Sheets For Unity/Quantum Drive Connection Data")]
public class GoogleSheetDriveConnectionData : ScriptableObject
{
	public enum ExportFormatter
	{
		String,
		FP, 
		Int,
		Bool,
		AssetName,
		WeaponMulti,
		AttackPayloadMulti,
		AssetRef,
		Enum,
		ActionSection,
		ActionSectionWithMaskArray,
		WindupNormalStartupFrame,
		WindupChargedStartupFrame,
		ActionClassMask,
		AttackPayloadSchools,
		PayloadMulti,
		PayloadSchools,
		HeroActionCostHelper,
		MultiHeroActionCostHelper,
	}
	[Serializable]
	public class AssetFieldData
	{
		public AssetFieldData() {
			FieldName = null;
			ColumnName = null;
			Export = true;
			Format = ExportFormatter.FP;
		}
		public string FieldName;
		public string ColumnName;
		public bool Export;
		public bool Import;
		public ExportFormatter Format;
	}
	[Serializable]
	public class AssetConnectionData
	{
		public string ClassName;
		public string SheetName;
		public ConnectionData Connection;
		public AssetFieldData[] FieldsToExport;
	}

	public AssetConnectionData[] QuantumAssets;

	public AssetConnectionData GetLookupData(string className) {
		for ( int i = 0; i < QuantumAssets.Length; ++i ) {
			var aconData = QuantumAssets[i];
			if ( aconData.ClassName == className )
				return aconData;
		}
		return null;
	}

	public static string Formatter(ExportFormatter format, object value) {
	#if UNITY_EDITOR

		switch (format) {
			case ExportFormatter.Int:
				return value.ToString();
			case ExportFormatter.String:
				return value.ToString();
			case ExportFormatter.AssetName:
				return ( (ScriptableObject) value ).name;
			// case ExportFormatter.FP:
			// 	FP fp = (FP) value;
			// 	return fp.AsFloat.ToString(CultureInfo.InvariantCulture);
			case ExportFormatter.Bool:
				return ((bool) value).ToString();
			case ExportFormatter.AssetRef:
				return GetAssetGuid(value);
			case ExportFormatter.Enum:
				return value.ToString();
		}
#endif
		return "Unsupported format. Please implement in GoogleSheetDriveConnectionData.cs";
	}

	public static string GetAssetGuid(object value) {
		var type = value.GetType();
		var guid = type.GetField("Guid");
		var stringGuid = guid.GetValue(value);
		return stringGuid.ToString();
	}
	
	public static bool Importer(ExportFormatter format, object target, string fieldName, string newValue) {
#if UNITY_EDITOR
		var t = target.GetType();
		var field = t.GetField(fieldName);
		var fieldValue = field.GetValue(target);
		
		switch (format) {
			case ExportFormatter.Int:
				if ( Int32.TryParse(newValue, out int value) ) {
					field.SetValue(target, value );
					return true;
				}
				return false;
			case ExportFormatter.String:
				field.SetValue(target, newValue );

				return true;
			case ExportFormatter.AssetName:
				( (ScriptableObject) target ).name = newValue;
			 	return true;
			// case ExportFormatter.FP:
			// 	FP fp = (FP) FP.FromString(newValue);
			// 	field.SetValue(target, fp);
			// 	return true;
			case ExportFormatter.Bool:
				if (Boolean.TryParse(newValue, out bool bValue) ){
					field.SetValue(target, bValue);
					return true;
				}
				return false;
			case ExportFormatter.AssetRef:
				return false;
			case ExportFormatter.Enum:
				var eVal = Enum.Parse(field.FieldType, newValue, true);
				field.SetValue(target, eVal);
				return true;
		}
#endif
		UnityEngine.Debug.LogError( "Unsupported format. Please implement in GoogleSheetDriveConnectionData.cs" );
		return false;
	}
	
}