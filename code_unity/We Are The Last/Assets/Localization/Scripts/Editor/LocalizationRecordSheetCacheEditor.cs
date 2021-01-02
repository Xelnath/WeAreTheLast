using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using GoogleSheetsForUnity;
using UnityEngine.Events;

[CustomEditor(typeof(LocalizationRecordSheetCache))]
public class LocalizationRecordSheetCacheEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var obj = serializedObject;
        obj.Update();

        LocalizationRecordSheetCache asset = (LocalizationRecordSheetCache) obj.targetObject;
        if (GUILayout.Button("Update Sheet List"))
        {
            SetupConnection(asset.connectionData);
            FetchSheets(asset);
        }

        base.OnInspectorGUI();
    }

    #region Sheets Communication 
    public static void SetupConnection(ConnectionData connection)
    {
        if (connection != null)
        {
            Drive.ConnectionData = connection;
        }
        else
        {
            var def = AssetDatabaseHelper.FindAssetsByType<DriveConnection>();
            Drive.ConnectionData = def[0].connectionData;
        }
    }
    public static void FetchSheets(LocalizationRecordSheetCache asset)
    {
        HandleOneCallRemoteFetch(asset);
        Drive.GetTableNames();
    }

    public static void HandleOneCallRemoteFetch(LocalizationRecordSheetCache asset)
    {

        UnityAction<Drive.DataContainer> anonymous = null;
        anonymous = (dataContainer) =>
        {
            //if ( dataContainer.objType != "area.isolaSacra" && dataContainer.objType != "quests")
            //	return;

            // First check the type of answer.
            if (dataContainer.QueryType == Drive.QueryType.getTableNames)
            {
                string rawJSon = dataContainer.payload;
                Debug.LogFormat("Sheet names retrieved: {0}", rawJSon);

                var tableNames = JsonHelper.ArrayFromJson<string>(rawJSon);
                asset.Sheets = tableNames;

                EditorUtility.SetDirty(asset);
                Drive.responseCallback -= anonymous;
            }
        };
        Drive.responseCallback += anonymous;
    }

    #endregion
}

