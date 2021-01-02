using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Sini.Unity;
using GoogleSheetsForUnity;
using System;
using System.Linq;
using UnityEngine.Events;

// Must match LocalizedString's field names
public enum LocalizationLanguages
{
    ShowAll = -1,
    English = 0,
    French,
    Italian,
    German,
    Spanish,
    Japanese,
    Portugese,
    ChineseStandard,
    Russian,
    COUNT
}

[CustomEditor(typeof(LocalizationRecord))]
public class LocalizationRecordEditor : Editor
{
    SerializedProperty spConnectionData;
    SerializedProperty spSheetName;

    SerializedProperty spLocalID;
    SerializedProperty spLocalChangeToID;
    SerializedProperty spLocalDirections;
    SerializedProperty spLocalCharacter;

    SerializedProperty spLocalRecordingLine;
    SerializedProperty spRemoteRecordingLine;

    SerializedProperty spLocalEmotion;
    SerializedProperty spRemoteEmotion;

    SerializedProperty spLocalTags;
    SerializedProperty spRemoteTags;

    SerializedProperty spRemoteID;
    SerializedProperty spRemoteChangeToID;
    SerializedProperty spRemoteDirections;
    SerializedProperty spRemoteCharacter;

    SerializedProperty spLocalEnglish;
    SerializedProperty spLocalOtherLanguage;

    SerializedProperty spRemoteEnglish;
    SerializedProperty spRemoteOtherLanguage;

    public LocalizationLanguages CurrentLanguage = LocalizationLanguages.ShowAll;

    public void OnEnable()
    {
        spConnectionData = serializedObject.FindProperty("connectionData");
        spSheetName = serializedObject.FindProperty("SheetName");
        spLocalID = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("ID");
        spLocalChangeToID = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("ChangeToID");
        spLocalDirections = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("LineDirections");
        spLocalCharacter = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("Character");
        spLocalRecordingLine = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("RecordingLine");
        spLocalEmotion = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("Emotion");
        spLocalTags = serializedObject.FindProperty("MetaDataLocal").FindPropertyRelative("Tags");

        spRemoteID = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("ID");
        spRemoteChangeToID = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("ChangeToID");
        spRemoteDirections = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("LineDirections");
        spRemoteCharacter = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("Character");
        spRemoteRecordingLine = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("RecordingLine");
        spRemoteEmotion = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("Emotion");
        spRemoteTags = serializedObject.FindProperty("MetaDataRemote").FindPropertyRelative("Tags");

        spLocalEnglish = serializedObject.FindProperty("Local").FindPropertyRelative("English");
        spLocalOtherLanguage = serializedObject.FindProperty("Local").FindPropertyRelative(CurrentLanguage.ToString());
        spRemoteEnglish = serializedObject.FindProperty("Remote").FindPropertyRelative("English");
        spRemoteOtherLanguage = serializedObject.FindProperty("Remote").FindPropertyRelative("English");
    }

    private bool m_expanded = false;


    public override void OnInspectorGUI()
    {
        var obj = serializedObject;
        EditorGUI.BeginChangeCheck();
        obj.Update();

        LocalizationRecord asset = (LocalizationRecord)targets[0];

        LocalizationSuggestedAction suggestedAction = asset.GetSuggestedAction();

        EditorGUILayout.PropertyField(spConnectionData);

        GUILayout.Label("", GUI.skin.horizontalSlider);

        var rect = GUILayoutUtility.GetRect(300f, 16f);

        Rect FetchRemote = rect.WithWidth(120f);
        Rect Accept = FetchRemote.RightOf(FetchRemote).Translate(12f, 0f).WithWidth(150f);
        Rect Upload = Accept.RightOf(Accept).Translate(12f, 0f).WithWidth(120f);

        Color reasonColor = Color.white;

        bool refreshOutOfDate = asset.RefreshTimeMoreThanFourHoursOld;
        int localRemoteDelta = asset.MetaDataLocal.Version - asset.MetaDataRemote.Version;

        switch (suggestedAction)
        {
            case LocalizationSuggestedAction.FetchRemote:
                reasonColor = Color.green;
                GUI.backgroundColor = Color.green;
                break;
            case LocalizationSuggestedAction.ResolveConflicts:
                if (reasonColor == Color.white) reasonColor = Color.yellow;
                GUI.backgroundColor = Color.yellow;
                break;
            case LocalizationSuggestedAction.NoActionRequired:
                GUI.backgroundColor = Color.white;
                break;
        }

        if (refreshOutOfDate && asset.LastRefreshTime != DateTime.MinValue) GUI.backgroundColor = Color.yellow;

        if (GUI.Button(FetchRemote, "Fetch Remote"))
        {
            LocalizationRecordHelper.FetchAssetRecord(asset);
        }

        switch (suggestedAction)
        {
            case LocalizationSuggestedAction.AcceptChanges:
                reasonColor = Color.green;
                GUI.backgroundColor = Color.green;
                break;
            case LocalizationSuggestedAction.ResolveConflicts:
                if (reasonColor == Color.white) reasonColor = Color.yellow;
                GUI.backgroundColor = Color.yellow;
                break;
            case LocalizationSuggestedAction.NoActionRequired:
            case LocalizationSuggestedAction.FetchRemote:
            case LocalizationSuggestedAction.UploadNewRecord:
            case LocalizationSuggestedAction.PushToRemote:
                GUI.backgroundColor = Color.white;
                break;
        }


        if (GUI.Button(Accept, "Accept Remote Version"))
        {
            asset.AcceptRemoteChanges();
        }

        switch (suggestedAction)
        {
            case LocalizationSuggestedAction.PushToRemote:
            case LocalizationSuggestedAction.UploadNewRecord:
                reasonColor = Color.green;
                GUI.backgroundColor = Color.green;
                break;
            case LocalizationSuggestedAction.ResolveConflicts:
                if (reasonColor == Color.white) reasonColor = Color.yellow;
                GUI.backgroundColor = Color.yellow;
                break;
            case LocalizationSuggestedAction.AcceptChanges:
            case LocalizationSuggestedAction.NoActionRequired:
                GUI.backgroundColor = Color.white;
                break;
        }


        if (GUI.Button(Upload, "Push to Remote"))
        {
            LocalizationRecordHelper.PushAssetRecord(asset);
        }

        GUI.backgroundColor = reasonColor;

        if (refreshOutOfDate)
        {
            if (asset.LastRefreshTime == DateTime.MinValue)
            {
                GUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label("Needs to be added to google spreadsheet");
                GUILayout.EndHorizontal();
            }
            else
            {
                var ogBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.yellow;
                GUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label("Needs to be refreshed");
                GUILayout.EndHorizontal();
                GUI.backgroundColor = ogBackgroundColor;
            }
        }
        {
            GUILayout.BeginHorizontal("HelpBox");
            if (localRemoteDelta == 0)
            {
                GUILayout.Label("Versions Match");
            }
            else if (localRemoteDelta < 0)
            {
                GUILayout.Label("Remote Version Higher");
            }
            else if (localRemoteDelta > 0)
            {
                GUILayout.Label("Local Version Higher");
            }
            GUILayout.EndHorizontal();
        }

        var localTimestamp = asset.LocalTimestamp;
        var remoteTimestamp = asset.RemoteTimestamp;

        bool remoteVersionIsHigher = (localRemoteDelta < 0);
        bool remoteVersionIsNewer = (localTimestamp < remoteTimestamp);

        if ( localTimestamp == remoteTimestamp)
        {

        }
        else if ( localTimestamp > remoteTimestamp)
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("Local Version More Recent");
            GUILayout.EndHorizontal();
        }
        else if ( localTimestamp < remoteTimestamp )
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("Remote version more recent");
            GUILayout.EndHorizontal();
        }

        if (asset.Local.English == asset.Remote.English)
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("Local English matches spreadsheet english");
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Label("English version different from remote");
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Label("", GUI.skin.horizontalSlider);

        EditorGUILayout.PropertyField(spSheetName);

        var rect2 = GUILayoutUtility.GetRect(300f, 16f);
        EditorGUI.LabelField(rect2.LeftHalf().LeftHalf(), "Current Language");
        CurrentLanguage = (LocalizationLanguages)EditorGUI.EnumPopup(rect2.LeftHalf().RightHalf(), CurrentLanguage);

        float originalLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 75;
        var rect3 = GUILayoutUtility.GetRect(300f, 16f);
        if (string.IsNullOrWhiteSpace(spRemoteChangeToID.stringValue) == false)
        {
            ComparativeTextArea(rect3, spLocalChangeToID, spRemoteChangeToID, remoteVersionIsHigher);
        }
        else
        {
            ComparativeTextArea(rect3, spLocalID, spRemoteID, remoteVersionIsHigher);
        }

        var rectIdCommands = GUILayoutUtility.GetRect(300f, 16f);
        if (string.IsNullOrWhiteSpace(spLocalID.stringValue))
        {
            GUI.enabled = false;
        }
        if (GUI.Button(rectIdCommands.RightHalf(), "Rename to ID"))
        {
            obj.targetObject.name = spLocalID.stringValue;
        }
        GUI.enabled = true;

        var rectCharacter = GUILayoutUtility.GetRect(300f, 16f);

        ComparativeTextArea(rectCharacter, spLocalCharacter, spRemoteCharacter, remoteVersionIsHigher);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Line Directions");
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        ComparativeEnumProperty(spLocalEmotion, spRemoteEmotion, remoteVersionIsHigher);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        var rectLocalDirections = GUILayoutUtility.GetRect(200f, 64f);
        ComparativeTextArea(rectLocalDirections, spLocalDirections, spRemoteDirections, remoteVersionIsHigher, false);

        GUILayout.Label("Recording Line");
        var rectRecording = GUILayoutUtility.GetRect(200f, 64f);

        ComparativeTextArea(rectRecording, spLocalRecordingLine, spRemoteRecordingLine, remoteVersionIsHigher, false);

        ComparativeTextField(spLocalTags, spRemoteTags, remoteVersionIsHigher);
        GUILayout.Space(4f);

        var rectTags = GUILayoutUtility.GetRect(200f, 16f);
        ComparativeTags(rectTags, spLocalTags, spRemoteTags, remoteVersionIsHigher);

        GUILayout.Space(16f);

        GUILayout.Label("", GUI.skin.horizontalSlider);

        EditorGUIUtility.labelWidth = 75;

        GUILayout.Label("Original English String");

        if (CurrentLanguage != LocalizationLanguages.ShowAll)
        {
            var rect7 = GUILayoutUtility.GetRect(200f, 64f);

            GUILayout.Label("", GUI.skin.horizontalSlider);

            ComparativeTextArea(rect7, spLocalEnglish, spRemoteEnglish, remoteVersionIsHigher);

            spLocalOtherLanguage = serializedObject.FindProperty("Local").FindPropertyRelative(CurrentLanguage.ToString());
            spRemoteOtherLanguage = serializedObject.FindProperty("Remote").FindPropertyRelative(CurrentLanguage.ToString());

            var rect8 = GUILayoutUtility.GetRect(200f, 64f);

            ComparativeTextArea(rect8, spLocalOtherLanguage, spRemoteOtherLanguage, remoteVersionIsHigher);

        }
        else
        {
            ComparativeTextField(spLocalEnglish, spRemoteEnglish, remoteVersionIsHigher);
            for (int i = 1; i < (int)LocalizationLanguages.COUNT; ++i)
            {
                LocalizationLanguages curr = (LocalizationLanguages)i;
                spLocalOtherLanguage = serializedObject.FindProperty("Local").FindPropertyRelative(curr.ToString());
                spRemoteOtherLanguage = serializedObject.FindProperty("Remote").FindPropertyRelative(curr.ToString());

                ComparativeTextField(spLocalOtherLanguage, spRemoteOtherLanguage, remoteVersionIsHigher);
            }
        }

        GUILayout.Label("", GUI.skin.horizontalSlider);
        EditorGUIUtility.labelWidth = originalLabelWidth;

        obj.ApplyModifiedProperties();
        EditorGUI.EndChangeCheck();

        m_expanded = EditorGUILayout.Foldout(m_expanded, "Defaults");
        /*
		CustomInspectorUtils.DrawDefaultInspector(serializedObject, "",
			new string[] { "SheetName" }, 
			false
			);
			*/
        if (m_expanded)
        {
            base.OnInspectorGUI();
        }
    }
    #region UI Comparators
    public void ComparativeTags(Rect rect, SerializedProperty a, SerializedProperty aRemote, bool remoteVersionIsNewer)
    {
        var tagStringsLocal = a.stringValue;
        var tagsLocal = tagStringsLocal.Split(',').Select(s => s.Trim()).ToArray();

        var tagStringsRemote = aRemote.stringValue;
        var tagsRemote = new List<string>(tagStringsRemote.Split(',').Select(s => s.Trim()));

        var backgroundColor = GUI.backgroundColor;

        var prevRect = rect.WithWidth(0);
        List<string> tagsNotInRemote = new List<string>();
        for (int i = 0; i < tagsLocal.Length; ++i)
        {
            if (string.IsNullOrEmpty(tagsLocal[i]))
            {
                continue;
            }
            var tagCurrent = tagsLocal[i];

            var tagRect = prevRect.RightOf(prevRect).Translate(4f, 0f).WithWidth(100);

            var foundInRemote = tagsRemote.Contains(tagCurrent);
            if (foundInRemote == false)
            {
                tagsNotInRemote.Add(tagCurrent);
                continue;
            }

            GUI.backgroundColor = foundInRemote ? new Color(0f, 0f, 0f, 0.1f) : Quantum.PolymerColor.paper_green_800.ToColor();
            EditorGUI.DrawRect(tagRect, GUI.backgroundColor);
            GUI.Label(tagRect, tagCurrent);
            prevRect = tagRect;
        }

        for (int i = 0; i < tagsRemote.Count; ++i)
        {
            if (string.IsNullOrEmpty(tagsRemote[i]))
            {
                continue;
            }
            var tagCurrent = tagsRemote[i];
            var tagRect = prevRect.RightOf(prevRect).Translate(4f, 0f).WithWidth(100);

            var foundInLocal = tagsLocal.Contains(tagCurrent);
            if (foundInLocal == false)
            {
                GUI.backgroundColor = Quantum.PolymerColor.paper_red_800.ToColor();
                EditorGUI.DrawRect(tagRect, GUI.backgroundColor);
                GUI.Label(tagRect, tagCurrent);
                prevRect = tagRect;
            }
        }

        for (int i = 0; i < tagsNotInRemote.Count; ++i)
        {
            if (string.IsNullOrEmpty(tagsNotInRemote[i]))
            {
                continue;
            }
            var tagCurrent = tagsNotInRemote[i];
            var tagRect = prevRect.RightOf(prevRect).Translate(4f, 0f).WithWidth(100);

            GUI.backgroundColor = Quantum.PolymerColor.paper_green_800.ToColor();
            EditorGUI.DrawRect(tagRect, GUI.backgroundColor);
            GUI.Label(tagRect, tagCurrent);
            prevRect = tagRect;
        }

        GUI.backgroundColor = Color.white;

    }

    public void ComparativeTextField(SerializedProperty a, SerializedProperty aRemote, bool remoteVersionIsNewer)
    {
        EditorGUILayout.PropertyField(a);
        if (a.stringValue != aRemote.stringValue)
        {
            GUI.backgroundColor = remoteVersionIsNewer ? Color.green : Color.red;
            GUI.enabled = false;

            var textArea = GUILayoutUtility.GetRect(300f, 16f);
            var useAreaRect = textArea.WithWidth(EditorGUIUtility.labelWidth);
            var originalTextArea = textArea.SquashLeft(useAreaRect.width);

            GUI.TextArea(originalTextArea, aRemote.stringValue);
            GUI.enabled = true;

            if (GUI.Button(useAreaRect, "Use Remote"))
            {
                a.stringValue = aRemote.stringValue;
            }
            GUI.backgroundColor = Color.white;
        }
    }

    public void ComparativeTextArea(Rect rect, SerializedProperty a, SerializedProperty aRemote, bool remoteVersionIsNewer, bool label = true)
    {
        EditorStyles.textField.wordWrap = true;
        EditorGUI.PropertyField(rect, a);
        if (a.stringValue != aRemote.stringValue)
        {
            GUI.backgroundColor = remoteVersionIsNewer ? Color.green : Color.red;
            GUI.enabled = false;

            GUILayout.Space(64f);

            rect = rect.BelowSelf();
            var useAreaRect = rect.WithWidth(EditorGUIUtility.labelWidth);
            var originalTextArea = rect.SquashLeft(useAreaRect.width);

            float labelWidth = EditorGUIUtility.labelWidth;
            if (label == false)
            {
                EditorGUIUtility.labelWidth = 0;
            }

            GUI.TextArea(originalTextArea, aRemote.stringValue);
            GUI.enabled = true;

            if (GUI.Button(useAreaRect, "Use Remote"))
            {
                a.stringValue = aRemote.stringValue;
            }
            GUI.backgroundColor = Color.white;
            EditorGUIUtility.labelWidth = labelWidth;
        }
        EditorStyles.textField.wordWrap = false;
    }


    public void ComparativeEnumProperty(SerializedProperty a, SerializedProperty aRemote, bool remoteVersionIsNewer, bool label = true)
    {
        EditorGUILayout.PropertyField(a);

        if (a.intValue != aRemote.intValue)
        {
            GUI.backgroundColor = remoteVersionIsNewer ? Color.green : Color.red;
            GUI.enabled = false;

            var textArea = GUILayoutUtility.GetRect(300, 16);
            var useAreaRect = textArea.WithWidth(EditorGUIUtility.labelWidth);
            var originalTextArea = textArea.SquashLeft(useAreaRect.width);

            float labelWidth = EditorGUIUtility.labelWidth;
            if (label == false)
            {
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.PropertyField(originalTextArea, aRemote, new GUIContent(""), true);
            GUI.enabled = true;

            if (GUI.Button(useAreaRect, "Use Remote"))
            {
                a.intValue = aRemote.intValue;
            }
            GUI.backgroundColor = Color.white;
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }

    public void ComparativeEnumPropertyArea(Rect rect, SerializedProperty a, SerializedProperty aRemote, bool remoteVersionIsNewer, bool label = true)
    {
        EditorGUI.PropertyField(rect, a);

        if (a.intValue != aRemote.intValue)
        {
            GUI.backgroundColor = remoteVersionIsNewer ? Color.green : Color.red;
            GUI.enabled = false;

            GUILayoutUtility.GetRect(rect.width, rect.height);
            rect = rect.BelowSelf();
            var useAreaRect = rect.WithWidth(EditorGUIUtility.labelWidth);
            var originalTextArea = rect.SquashLeft(useAreaRect.width);

            float labelWidth = EditorGUIUtility.labelWidth;
            if (label == false)
            {
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.PropertyField(originalTextArea, aRemote, true);
            GUI.enabled = true;

            if (GUI.Button(useAreaRect, "Use Remote"))
            {
                a.intValue = aRemote.intValue;
            }
            GUI.backgroundColor = Color.white;
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
    #endregion

}
