
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
	}
}