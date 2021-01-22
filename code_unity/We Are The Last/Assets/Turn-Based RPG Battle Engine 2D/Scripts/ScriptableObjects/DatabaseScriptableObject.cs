
using System.Collections.Generic;
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
	#if UNITY_EDITOR

	[Sirenix.OdinInspector.ShowInInspector]
	public void LoadAllAssets()
	{
		var allSkills = Resources.FindObjectsOfTypeAll<SkillAsset>();
		var allCharacters = Resources.FindObjectsOfTypeAll<CharacterAsset>();

		SkillAssets = new List<SkillAsset>( allSkills );
		CharacterAssets = new List<CharacterAsset>( allCharacters );

		SkillAssets.Sort( ( a, b ) => { return a.Skill.id.CompareTo( b.Skill.id ); } );
		CharacterAssets.Sort( ( a, b ) => { return a.Character.id.CompareTo( b.Character.id ); } );
}

	[Sirenix.OdinInspector.ShowInInspector]
	public void ImportAllSkillFunctions()
	{
		var allSkills = Resources.FindObjectsOfTypeAll<SkillAsset>();
		foreach (SkillAsset skill in allSkills)
		{
			if (skill.SkillText != null)
			{
				skill.Skill.functionsToCall = SkillTextParser.parseFunctionsToCall(skill.SkillText);
				skill.Skill.endOfRound = SkillTextParser.parseEndOfRound(skill.SkillText);
				skill.Skill.sacrificeActions = SkillTextParser.parseSacrifice(skill.SkillText);
				EditorUtility.SetDirty(skill);
			}
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

	public List<SkillAsset> SkillAssets = new List<SkillAsset>();
	public List<CharacterAsset> CharacterAssets = new List<CharacterAsset>();

	//A list of all in-game characters
	public List<character> characters = new List<character>();

	//A list of all in-game items
	public List<item> items = new List<item>();

	//A list of all in-game skills
	public List<skill> skills = new List<skill>();

	//Used by "EditorDatabase.cs" to determine which tab is currently selected
	[HideInInspector] public int tab;

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
}