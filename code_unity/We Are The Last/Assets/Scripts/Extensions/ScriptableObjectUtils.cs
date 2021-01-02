using UnityEditor;
using UnityEngine;

public static class ScriptableObjectUtils  {

	public static T CreateAsset<T> (string assetName, string path = "Assets") where T : ScriptableObject
	{
		T asset = ScriptableObject.CreateInstance<T> ();
 
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/" + assetName + ".asset");
 
		AssetDatabase.CreateAsset (asset, assetPathAndName);
 
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh();

		return asset;
	}

	public static void CreateAsset(ScriptableObject localAsset, string assetName, string path = "Assets")
	{
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");

		AssetDatabase.CreateAsset(localAsset, assetPathAndName);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}
