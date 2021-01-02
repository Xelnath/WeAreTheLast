using GoogleSheetsForUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public class LocalizationRecordHelper 
{


    public static void FetchAssetRecord(LocalizationRecord asset)
    {
        SetupConnection(asset.connectionData);
        FetchRemoteRecord(asset.SheetName, asset.MetaDataLocal.ID, asset);
    }
    public static void PushAssetRecord(LocalizationRecord asset)
    {
        SetupConnection(asset.connectionData);
        if (asset.Local.English != asset.Remote.English)
        {
            UpdateVersion(asset, asset.MetaDataLocal, asset.MetaDataRemote);
        }
        UpdateRefreshTime(asset);
        PushLocalRecordToRemote(asset);
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

    public static void PushLocalRecordToRemote(LocalizationRecord asset)
    {
        string sheetName = asset.SheetName;
        LocalizationMetaData local = asset.MetaDataLocal;
        LocalizationMetaData remote = asset.MetaDataRemote;
        LocalizedStringData localStrings = asset.Local;
        LocalizedStringData remoteStrings = asset.Remote;


        UnityAction<Drive.DataContainer> anonymous = null;
        anonymous = (dataContainer) => {
            if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
            {
                Debug.Log(dataContainer.msg + " " + dataContainer.searchValue);

                if (dataContainer.searchValue == asset.MetaDataLocal.ID)
                {
                    asset.CopyLocalToRemoteCache();
                    UpdateRefreshTime(asset);
                    EditorUtility.SetDirty(asset);
                }

                Drive.responseCallback -= anonymous;
            }
        };
        Drive.responseCallback += anonymous;

        string ID = local.ID;
        string obj = BuildObject(local, remote, localStrings, remoteStrings);
        Drive.UpdateObjects(sheetName, "ID", ID, obj, create: true, runtime: false); ;
    }

    public static void UpdateVersion(LocalizationRecord record, LocalizationMetaData local, LocalizationMetaData remote)
    {
        local.Version = Mathf.Max(local.Version, remote.Version);
        local.Timestamp = System.DateTime.UtcNow.ToString("u");
    }
    public static void UpdateRefreshTime(LocalizationRecord record)
    {
        record.LastRefresh = System.DateTime.UtcNow.ToString("u");
        EditorUtility.SetDirty(record);
    }

    public static string BuildObject(LocalizationMetaData local, LocalizationMetaData remote,
        LocalizedStringData localStrings, LocalizedStringData remoteStrings)
    {

        Dictionary<string, string> PreJSON = new Dictionary<string, string>();

        Action<string, string> safeAdd = (a, b) =>
        {
            var safeB = EscapeString(b);
            safeB = safeB ?? "";
            PreJSON.Add(a, safeB);
        };

        if (string.IsNullOrWhiteSpace(local.ChangeToID) == false)
        {
            PreJSON.Add("ID", local.ChangeToID);
            local.ID = local.ChangeToID;
            PreJSON.Add("ChangeToID", "");
        }
        else
        {
            safeAdd("ID", local.ID);
        }

        if (local.Character != remote.Character)
            safeAdd("Character", local.Character);
        if (local.LineDirections != remote.LineDirections)
            safeAdd("LineDirections", local.LineDirections);
        if (local.RecordingLine != remote.RecordingLine)
            safeAdd("RecordingLine", local.RecordingLine);
        if (local.Screenshot != remote.Screenshot)
            safeAdd("Screenshot", local.Screenshot);
        if (local.SoundFile != remote.SoundFile)
            safeAdd("SoundFile", local.SoundFile);
        if (local.Timestamp != remote.Timestamp)
            safeAdd("Timestamp", local.Timestamp.ToString());
        if (local.Version != remote.Version)
            safeAdd("Version", local.Version.ToString());
        if (local.Tags != remote.Tags)
            safeAdd("Tags", local.Tags.ToString());
        if (local.Emotion != remote.Emotion)
            safeAdd("Emotion", local.Emotion.ToString());

        if (localStrings.English != remoteStrings.English)
            safeAdd("English", localStrings.English);

        if (localStrings.English != remoteStrings.English)
            safeAdd("ChineseStandard", localStrings.ChineseStandard);
        if (localStrings.French != remoteStrings.French)
            safeAdd("French", localStrings.French);
        if (localStrings.German != remoteStrings.German)
            safeAdd("German", localStrings.German);
        if (localStrings.Italian != remoteStrings.Italian)
            safeAdd("Italian", localStrings.Italian);
        if (localStrings.Japanese != remoteStrings.Japanese)
            safeAdd("Japanese", localStrings.Japanese);
        if (localStrings.Portugese != remoteStrings.Portugese)
            safeAdd("Portugese", localStrings.Portugese);
        if (localStrings.Russian != remoteStrings.Russian)
            safeAdd("Russian", localStrings.Russian);
        if (localStrings.Spanish != remoteStrings.Spanish)
            safeAdd("Spanish", localStrings.Spanish);

        string json = "{";
        foreach (var kvp in PreJSON)
        {
            json += string.Format("\"{0}\" : \"{1}\",", kvp.Key, kvp.Value);
        }
        json += "}";
        return json;
    }

    // This just replaces " with \" 
    private static string EscapeString(string dirty)
    {
        return dirty.Replace("\"", "\\\"");
    }

    // Processes the data received from the cloud.
    private static void HandlePushCommand(Drive.DataContainer dataContainer)
    {
        if (dataContainer.QueryType != Drive.QueryType.createTable || dataContainer.QueryType != Drive.QueryType.createObjects)
        {
            Debug.Log(dataContainer.msg);
        }
    }

    public static void FetchRemoteRecord(string sheetName, string ID, LocalizationRecord record)
    {
        HandleOneCallRemoteFetch(record);
        Drive.GetObjectsByField(sheetName, "ID", ID, false);
    }

    public static void HandleOneCallRemoteFetch(LocalizationRecord asset)
    {

        UnityAction<Drive.DataContainer> anonymous = null;
        anonymous = (dataContainer) =>
        {
            //if ( dataContainer.objType != "area.isolaSacra" && dataContainer.objType != "quests")
            //	return;

            // First check the type of answer.
            if (dataContainer.QueryType == Drive.QueryType.getObjectsByField)
            {
                string rawJSon = dataContainer.payload;
                Debug.LogFormat("Data from Google Drive received.", rawJSon);

                LocalizationMetaData[] meta = JsonHelper.ArrayFromJson<LocalizationMetaData>(rawJSon);
                LocalizedStringData[] strings = JsonHelper.ArrayFromJson<LocalizedStringData>(rawJSon);

                // RISKY: fix later!
                // Confirm the asset matches!
                if (meta[0].ID == asset.MetaDataLocal.ID && dataContainer.objType == asset.SheetName)
                {
                    UpdateRecord(asset, meta[0], strings[0]);
                    Drive.responseCallback -= anonymous;
                }
            }
        };
        Drive.responseCallback += anonymous;
    }

    public static void UpdateRecord(LocalizationRecord record, LocalizationMetaData meta, LocalizedStringData strings)
    {
        var oldRemote = record.Remote;

        // If a local change occurred
        if (record.Local.English != record.Remote.English && record.MetaDataLocal.Timestamp == record.MetaDataRemote.Timestamp)
        {
            record.MetaDataLocal.Version = record.MetaDataRemote.Version + 1;
        }
        record.MetaDataLocal.Timestamp = DateTime.UtcNow.ToString("u");

        if (record.Remote != strings)
        {
            bool englishChanged = record.Remote.English != strings.English;
            bool matchesLocal = record.Local.English == strings.English;

            record.MetaDataRemote = new LocalizationMetaData(meta);
            record.Remote = new LocalizedStringData(strings);

            if (englishChanged && !matchesLocal)
            {
                record.MetaDataRemote.Version = meta.Version + 1;
            }
        }

        UpdateRefreshTime(record);

        EditorUtility.SetDirty(record);

    }
    #endregion

}
