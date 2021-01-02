using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kor;
using UnityEngine;
using UnityEditor;
using DryIoc;
using UnityEngine.SceneManagement;

namespace Atomech
{
	public static class LocalizationRecords {


        public static string GenerateSceneLookupPathGuid(GameObject sceneObject, LocalizationRecord newRecord, LocalizedAttribute attribute, string suggestedSuffix)
        {
            var page = GetLocalizationPage(null, newRecord, attribute, out var inferredPage);

            if (!string.IsNullOrWhiteSpace(newRecord?.SheetName))
            {
                page = newRecord.SheetName;
            }

            var activeSceneName = SceneManager.GetActiveScene().name;

            var sceneObjectPath = sceneObject.name;
            var next = sceneObject.transform.parent;
            while(next != null)
            {
                sceneObjectPath = $"{next.name}.{sceneObjectPath}";
                next = next.transform.parent;
            }

            return $"scene.{activeSceneName}.{sceneObjectPath}.{page ?? "Pageless"}.{attribute?.Key ?? suggestedSuffix}";
        }


         public static string GenerateLookupPathGuid(ScriptableObject parentAsset, LocalizationRecord newRecord, LocalizedAttribute attribute, string suggestedSuffix ) {
			var page = GetLocalizationPage(parentAsset, newRecord, attribute, out var inferredPage);

			if (!string.IsNullOrWhiteSpace(newRecord?.SheetName)) {
				page = newRecord.SheetName;
			}
			return "undefined.new";
		}

		public static string InferSheetFromType(ScriptableObject parentAsset, LocalizationRecord newRecord, LocalizedAttribute attribute ) {
			return "undefined";
		}

		public static string GetLocalizationPage(ScriptableObject parentAsset, LocalizationRecord newRecord, LocalizedAttribute attribute, out string suggestedPage) {
            if (parentAsset != null)
            {
                suggestedPage = InferSheetFromType(parentAsset, newRecord, attribute);
            }
            else
            {
                suggestedPage = "undefined";
            }

			if (attribute?.Sheet != null) {
				return attribute.Sheet;
			}


			return newRecord?.SheetName ?? suggestedPage;
		}

		public static LocalizationRecord GetByGuid(string guid) { 
			if (guid == null)
				return null;

			return Kor.Kor.Instance.Container.Resolve<LocalizationRecordCacheService>().GetAssetByGuid(guid);
		}
	}

	[UsedImplicitly]
	public class LocalizationRecordCacheService : IInitializable
	{
		public string Name => "LocalizationRecordCacheService";

		private readonly List<LocalizationRecord> m_localizationRecords = new List<LocalizationRecord>();
		private readonly Dictionary<string, LocalizationRecord> m_localizationGuidToUnityAsset = new Dictionary<string, LocalizationRecord>();
		private readonly Dictionary<LocalizationRecord, string> m_unityAssetToLocalizationRecordGuid = new Dictionary<LocalizationRecord, string>();

		public List<LocalizationRecord> LocRecords => m_localizationRecords;

		public void Initialize() {
            foreach (LocalizationRecord localizationRecord in AssetDatabaseHelper.FindAssetsByType<LocalizationRecord>())
            {
                RegisterLocalizationRecord(localizationRecord);
            }
            //foreach ( LocalizationRecord localizationRecord in Resources.LoadAll<LocalizationRecord>("Localization")) {
			//	RegisterLocalizationRecord(localizationRecord);
            //}

            UnityEngine.Debug.Log($"[Localization] {m_localizationRecords.Count} assets loaded into the localization record cache.");
			LocalizationRecord.AfterDeserialized += OnLocalizationRecordDeserialized;
		}

		public void OnLocalizationRecordGuidChanged(LocalizationRecord locRecord) {
			OnLocalizationRecordDeserialized(locRecord);
		}

		private void OnLocalizationRecordDeserialized(LocalizationRecord locRecord) {
			DeregisterLocalizationRecord(locRecord);
			RegisterLocalizationRecord(locRecord);
		}

		private void DeregisterLocalizationRecord(LocalizationRecord locRecord) {
			if (locRecord is null) return;

			m_localizationRecords.Remove(locRecord);
			if (m_unityAssetToLocalizationRecordGuid.TryGetValue(locRecord, out string oldAssetGuid)) {
				m_localizationGuidToUnityAsset.Remove(oldAssetGuid);
				m_unityAssetToLocalizationRecordGuid.Remove(locRecord);
			}
		}

		private void RegisterLocalizationRecord(LocalizationRecord locRecord) {
			if (locRecord is null) return; 

			m_localizationRecords.Add(locRecord);


			if (string.IsNullOrEmpty(locRecord?.MetaDataLocal?.ID)) {
				DelayedLog.RegisterDelayedDebugLog(() => UnityEngine.Debug.LogWarning($"Localization asset {locRecord.name} is missing a unique ID.", locRecord));
				return;
			}

			if (m_localizationGuidToUnityAsset.TryGetValue(locRecord?.MetaDataLocal?.ID, out LocalizationRecord existingAsset)) {
				if (existingAsset) {
					DelayedLog.RegisterDelayedDebugLog(() => UnityEngine.Debug.LogWarning(
						$"Duplicate Localization Record GUID {locRecord?.MetaDataLocal?.ID}. Asset 1: {existingAsset.name} Asset 2: {locRecord.name}", locRecord));
					return;
				}
				else {
					m_localizationGuidToUnityAsset.Remove(locRecord?.MetaDataLocal?.ID);
				}
			}

			m_localizationGuidToUnityAsset.Add(locRecord?.MetaDataLocal?.ID, locRecord);
			m_unityAssetToLocalizationRecordGuid.Add(locRecord, locRecord?.MetaDataLocal?.ID);

			//DelayedLog.RegisterDelayedDebugLog(() => UnityEngine.Debug.Log($"{quantumAsset.name} registered into the Quantum asset cache."));
		}

		public LocalizationRecord GetAssetByGuid(string quantumGuid) {
			if (m_localizationGuidToUnityAsset.TryGetValue(quantumGuid, out LocalizationRecord record)) {
				return record;
			}
			return null;
		}

	}
}
