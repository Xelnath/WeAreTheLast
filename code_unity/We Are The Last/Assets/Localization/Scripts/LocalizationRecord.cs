using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GoogleSheetsForUnity;
using JetBrains.Annotations;
using UnityEngine;

	[Serializable]
	public enum LocalizationEmotion
	{
		Neutral = 0,
		Anger,
		Sadness,
		Joy,
		Disgust,
		Surprised,
		Confused,
		Trusting,
		Anticipation,
		Shame,
		Pity,
		Envy,
		Indignation,
		Love,
		Friendship,
		Mocking,
		Dismissive,
		Curious,
	}

	[Serializable]
	public enum LocalizationSuggestedAction {
        UploadNewRecord,
		FetchRemote,
		AcceptChanges,
        ResolveConflicts,
		PushToRemote, 
		NoActionRequired
	}

[Serializable]
public class LocalizationMetaData
{
    public string ID;
    public string ChangeToID;
    public int Version;
    public string Timestamp;
    public string LineDirections;
    public LocalizationEmotion Emotion;
    public string RecordingLine;
    public string Tags;
    public string Character;
    public string SoundFile;
    public string Screenshot;

    public bool Equals(LocalizationMetaData other)
    {
        return
            ID == other.ID &&
            Version == other.Version &&
            Emotion == other.Emotion &&
            SafeEquals(LineDirections, other.LineDirections ) &&
            SafeEquals(RecordingLine, other.RecordingLine) &&
            SafeEquals(Tags, other.Tags) &&
            SafeEquals(Character, other.Character) &&
            SafeEquals(SoundFile, other.SoundFile) &&
            SafeEquals(Screenshot, other.Screenshot);
    }

    public bool SafeEquals(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return true;
        return a == b;
    }
    public LocalizationMetaData()
    {
    }
    public LocalizationMetaData(LocalizationMetaData other)
    {
        Copy(other);
    }
    public void Copy(LocalizationMetaData other)
    {
        ID = other.ID;
        Version = other.Version;
        Emotion = other.Emotion;
        LineDirections = SafeCopy(other.LineDirections);
        RecordingLine = SafeCopy(other.RecordingLine);
        Tags = SafeCopy(other.Tags);
        Character = SafeCopy(other.Character);
        SoundFile = SafeCopy(other.SoundFile);
        Screenshot = SafeCopy(other.Screenshot);
    }

    [CanBeNull]
    public string SafeCopy(string a)
    {
        return a == "null" ? null : a;
    }
}

[Serializable]
public class LocalizedStringData
{
    public string English;
    public string French;
    public string Italian;
    public string German;
    public string Spanish;
    public string Japanese;
    public string Portugese;
    public string ChineseStandard;
    public string Russian;

    public bool Equals(LocalizedStringData other)
    {
        return
            SafeEquals(English, other.English) &&
            SafeEquals(French, other.French) &&
            SafeEquals(Italian, other.Italian) &&
            SafeEquals(German, other.German) &&
            SafeEquals(Spanish, other.Spanish) &&
            SafeEquals(Japanese, other.Japanese) &&
            SafeEquals(Portugese, other.Portugese) &&
            SafeEquals(ChineseStandard, other.ChineseStandard) &&
            SafeEquals(Russian, other.Russian);
    }

    public bool SafeEquals([CanBeNull] string a, [CanBeNull] string b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return true;
        return a == b;
    }

    public LocalizedStringData()
    {
    }
    public LocalizedStringData(LocalizedStringData other)
    {
        Copy(other);
    }
    public void Copy(LocalizedStringData other)
    {
        English = SafeCopy(other.English);
        French = SafeCopy(other.French);
        Italian = SafeCopy(other.Italian);
        German = SafeCopy(other.German);
        Spanish = SafeCopy(other.Spanish);
        Japanese = SafeCopy(other.Japanese);
        Portugese = SafeCopy(other.Portugese);
        ChineseStandard = SafeCopy(other.ChineseStandard);
        Russian = SafeCopy(other.Russian);
    }

    public string SafeCopy(string a)
    {
        return a == "null" ? null : a;
    }
}

[CreateAssetMenu(fileName = "LocalizationData", menuName = "Google Sheets For Unity/Localization Data")]
public class LocalizationRecord : ScriptableObject, ISerializationCallbackReceiver
{
    public static event Action<LocalizationRecord> AfterDeserialized;

    public string Guid = System.Guid.NewGuid().ToString().ToLowerInvariant();
    public ConnectionData connectionData;

    public string SheetName;

    public LocalizationMetaData MetaDataLocal;
    public LocalizationMetaData MetaDataRemote;

    public string LastRefresh = DateTime.MinValue.ToString("u");

    public LocalizedStringData Local;
    public LocalizedStringData Remote;

    public enum Language
    {
        English,
        French,
        Italian,
        German,
        Spanish,
        Japanese,
        Portugese,
        ChineseStandard,
        Russian
    }
    public static Language CurrentLanguage = Language.English;

    // This allows for both a language and a fallback pattern, e.g. spanish vs iberian spanish
    public string TranslatedString
    {
        get {
            switch (CurrentLanguage)
            {
                case Language.English:
                    return Local?.English;
                case Language.French:
                    return Local?.French ?? Local?.English;
                case Language.Italian:
                    return Local?.Italian ?? Local?.English;
                case Language.German:
                    return Local?.German ?? Local?.English;
                case Language.Spanish:
                    return Local?.Spanish ?? Local?.English;
                case Language.Japanese:
                    return Local?.Japanese ?? Local?.English;
                case Language.Portugese:
                    return Local?.Portugese ?? Local?.English;
                case Language.ChineseStandard:
                    return Local?.ChineseStandard ?? Local?.English;
            }

            return "Uninitialized value";
        }
    }

    public void Init()
    {
        Local = new LocalizedStringData();
        Remote = new LocalizedStringData();
        MetaDataLocal = new LocalizationMetaData();
        MetaDataRemote = new LocalizationMetaData();
    }

    public void AcceptRemoteChanges()
    {
        MetaDataLocal.Copy( MetaDataRemote );
        Local.Copy( Remote );
    }
    public void CopyLocalToRemoteCache()
    {
        MetaDataRemote.Copy(MetaDataLocal);
        Remote.Copy(Local);
			MetaDataLocal.Version = Mathf.Max(MetaDataRemote.Version, MetaDataLocal.Version);
			MetaDataLocal.Timestamp = DateTime.UtcNow.ToString("u");
    }

    public void RejectRemoteChanges()
    {
        Remote.English = Local.English;
        MetaDataLocal.Version = Mathf.Max(MetaDataRemote.Version, MetaDataLocal.Version);
        MetaDataLocal.Timestamp = DateTime.UtcNow.ToString("u");
    }

    public DateTime LastRefreshTime
    {
        get {
            DateTime lastRefreshTime = DateTime.MinValue;
            if (string.IsNullOrEmpty(LastRefresh) == false)
            {
                lastRefreshTime = DateTime.Parse(LastRefresh, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            return lastRefreshTime;
        }
    }
    public DateTime LocalTimestamp
    {
        get {
            DateTime localTimestamp = DateTime.MinValue;
            if (string.IsNullOrEmpty(MetaDataLocal.Timestamp) == false)
            {
                localTimestamp = DateTime.Parse(MetaDataLocal.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            return localTimestamp;
        }
    }
    public DateTime RemoteTimestamp
    {
        get {
            DateTime remoteTimestamp = DateTime.MinValue;
            if (string.IsNullOrEmpty(MetaDataRemote.Timestamp) == false)
            {
                remoteTimestamp = DateTime.Parse(MetaDataRemote.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            }
            return remoteTimestamp;
        }
    }

    public bool RefreshTimeMoreThanFourHoursOld
    {
        get {
            var timeSinceRefresh = DateTime.UtcNow.Subtract(LastRefreshTime);
            bool refreshMoreThanFourHoursOld = (timeSinceRefresh.TotalHours > 4);
            return refreshMoreThanFourHoursOld;
        }
    }

    public LocalizationSuggestedAction GetSuggestedAction()
    {
        DateTime lastRefreshTime = LastRefreshTime;
        DateTime localTimestamp = LocalTimestamp;
        DateTime remoteTimestamp = RemoteTimestamp;

        bool refreshMoreThanFourHoursOld = RefreshTimeMoreThanFourHoursOld;

        // If you have the minimum value, this has never been pushed
        if (lastRefreshTime == DateTime.MinValue || string.IsNullOrWhiteSpace(MetaDataRemote.ID))
        {
            return LocalizationSuggestedAction.UploadNewRecord;
        }

        // If you've pushed, but not refresh
        if (localTimestamp > lastRefreshTime || refreshMoreThanFourHoursOld )
        {
            return LocalizationSuggestedAction.FetchRemote;
        }

        if (MetaDataRemote.Version > MetaDataLocal.Version)
        {
            return LocalizationSuggestedAction.AcceptChanges;
        }

        if (MetaDataRemote.Version < MetaDataLocal.Version)
        {
            if (localTimestamp > lastRefreshTime)
            {
                return LocalizationSuggestedAction.FetchRemote;
            }
            else
            {
                return LocalizationSuggestedAction.PushToRemote;
            }
        }

        if (MetaDataRemote.Version == MetaDataLocal.Version)
        {

            if (!Local.Equals(Remote))
            {
                if ( localTimestamp > remoteTimestamp )
                {
                    return LocalizationSuggestedAction.PushToRemote;
                }
                else
                {
                    return LocalizationSuggestedAction.ResolveConflicts;
                }
            }
            if (!MetaDataLocal.Equals(MetaDataRemote))
            {
                return LocalizationSuggestedAction.PushToRemote;
            }
        }

        return LocalizationSuggestedAction.NoActionRequired;
    }
    public virtual void OnBeforeSerialize() { }
    public virtual void OnAfterDeserialize()
    {
        AfterDeserialized?.Invoke(this);
    }
}
