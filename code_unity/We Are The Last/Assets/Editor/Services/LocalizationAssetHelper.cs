using UnityEditor;
using UnityEngine;

namespace Atomech
{
    public static class LocalizationAssetHelper
    {
        public static string LocalizationPath = "/Assets/localization";
        public static LocalizationRecord CreateLooseRecord(string sheetName, string locID)
        {
            string[] path = locID.Split('.');

            string folder = LocalizationPath;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "localization");
            }

            for (int i = 0; i < path.Length - 1; ++i)
            {
                var nextFolder = $"{folder}/{path[i]}";
                if (!AssetDatabase.IsValidFolder(nextFolder))
                {
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

        public static LocalizationRecord CreateChildRecord(string sheetName, string locID, string childName, ScriptableObject parent)
        {
            var path = AssetDatabase.GetAssetPath(parent);
            var objects = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            for (int i = 0; i < objects.Length; ++i)
            {
                var obj = objects[i];

                if (obj is LocalizationRecord locRecord)
                {
                    if (obj.name == childName || locRecord.MetaDataLocal.ID == locID)
                    {
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