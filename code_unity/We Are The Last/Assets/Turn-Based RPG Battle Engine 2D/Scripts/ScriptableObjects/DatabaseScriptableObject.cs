
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClassDB;
using UnityEditor;
using UnityEngine;

[Guid("3A635FB1-E3ED-4913-9C0B-6EDF250F84FA")]
public class DatabaseScriptableObject : ScriptableObject
{
	private static DatabaseScriptableObject m_instance;
	public static DatabaseScriptableObject core {
		get {
			if ( m_instance == null )
			{
				var assets = AssetDatabase.FindAssets( "t:DatabaseScriptableObject" );
				m_instance = AssetDatabase.LoadAssetAtPath<DatabaseScriptableObject>( assets[0] );
			}

			return m_instance;
		}
	}

	public List<SkillAsset> SkillAssets = new List<SkillAsset>();
	public List<CharacterAsset> CharacterAssets = new List<CharacterAsset>();
	public List<GameObject> FXPrefabs = new List<GameObject>();
	public List<WaveAsset> WaveAssets = new List<WaveAsset>();

	//A list of all in-game characters
	public List<character> characters = new List<character>();

	//A list of all in-game items
	public List<item> items = new List<item>();

	//A list of all in-game skills
	public List<skill> skills = new List<skill>();
	
	//A list of all waves 
	public List<Wave> waves = new List<Wave>(); 

	//Used by "EditorDatabase.cs" to determine which tab is currently selected
	[HideInInspector] public int tab;

	public GameObject FindFXByName( string name )
	{
		foreach ( var vfx in FXPrefabs )
		{
			if ( String.Equals( vfx.name, name, StringComparison.CurrentCultureIgnoreCase ) ) return vfx;
		}

		Debug.LogError( $"Can't find vfx {name}. Check if you added it to the database prefab." );
		return null;
	}

	public void Copy( DatabaseScriptableObject toCopy )
	{
		characters.Clear();

		for ( int i = 0; i < toCopy.characters.Count; ++i )
		{
			var n = new character();
			n.Copy( toCopy.characters[i] );
			characters.Add( n );
		}

		items = toCopy.items.DeepClone();
		skills = toCopy.skills.DeepClone();

		foreach ( var asset in toCopy.SkillAssets )
		{
			LoadSkillAsset( asset );
		}
		foreach ( var asset in toCopy.CharacterAssets )
		{
			LoadCharacterAsset( asset );
		}

		waves.Clear();
		waves.AddRange(
	toCopy.WaveAssets
				.Where( x => x.Disabled == false )
				.OrderBy( x => x.OrderID )
				.Select( x => x.wave )
				.ToArray());
	}

	private void LoadSkillAsset( SkillAsset asset )
	{
		for ( int i = 0; i < skills.Count; ++i )
		{
			if ( skills[i].id == asset.Skill.id )
			{
				Debug.LogWarning( $"Replaced skill {i} - {skills[i].name} with asset {asset.name}. IDs match." );
				skills[i] = asset.Skill;
				return;
			}
		}

		skills.Add( asset.Skill );
	}
	private void LoadCharacterAsset( CharacterAsset asset )
	{
		for ( int i = 0; i < characters.Count; ++i )
		{
			if ( characters[i].id == asset.Character.id )
			{
				Debug.LogWarning( $"Replaced character {i} - {characters[i].name} with asset {asset.name}. IDs match." );
				characters[i] = asset.Character;
				return;
			}
		}

		characters.Add( asset.Character );
	}
	
	
#if UNITY_EDITOR

	[Sirenix.OdinInspector.ShowInInspector]
	public void LoadAllAssets()
	{
		AssetDatabase.Refresh();
		var allSkills = Resources.FindObjectsOfTypeAll<SkillAsset>();
		var allCharacters = Resources.FindObjectsOfTypeAll<CharacterAsset>();
		var allWaves = Resources.FindObjectsOfTypeAll<WaveAsset>();

		SkillAssets = new List<SkillAsset>( allSkills );
		CharacterAssets = new List<CharacterAsset>( allCharacters );
		WaveAssets = new List<WaveAsset>( allWaves );

		SkillAssets.Sort( ( a, b ) => a.Skill.id.CompareTo( b.Skill.id ) );
		CharacterAssets.Sort( ( a, b ) => a.Character.id.CompareTo( b.Character.id ) );
		WaveAssets.Sort( ( a, b ) => a.OrderID.CompareTo(b.OrderID) );
		
		var vfx = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs/FX"});
		FXPrefabs = new List<GameObject>( );
		for ( int i = 0; i < vfx.Length; ++i )
		{
			var path = AssetDatabase.GUIDToAssetPath( vfx[i] );
			var objs = AssetDatabase.LoadAllAssetsAtPath( path );
			FXPrefabs.AddRange( objs.OfType<GameObject>().Where(x=> x.transform.parent == null) );
		}

		EditorUtility.SetDirty( this );
	}
	

	[Sirenix.OdinInspector.ShowInInspector]
	public void ImportAllSkillFunctions()
	{
		var allSkills = Resources.FindObjectsOfTypeAll<SkillAsset>();
		foreach (SkillAsset skill in allSkills)
		{
			skill.ImportSkillFunctions(false);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
	[Sirenix.OdinInspector.ShowInInspector]
	public void ExportAllSkillFunctions()
	{
		var allSkills = Resources.FindObjectsOfTypeAll<SkillAsset>();
		foreach (SkillAsset skill in allSkills)
		{
			skill.ExportSkillFunctions();
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

#endif
}