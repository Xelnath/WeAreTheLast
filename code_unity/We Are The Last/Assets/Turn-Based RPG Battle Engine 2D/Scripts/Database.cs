using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using Sirenix.OdinInspector;

public class Database : MonoBehaviour {

	public static DatabaseScriptableObject core;

	public DatabaseScriptableObject database;
	
	//A list of all in-game characters
	public List<character> characters = new List<character>();

	//A list of all in-game items
	public List<item> items = new List<item>();

	//A list of all in-game skills
	public List<skill> skills = new List<skill>();

	//Used by "EditorDatabase.cs" to determine which tab is currently selected
	[HideInInspector] public int tab;

	void Awake () { if (core == null) { core = database; } }

	[ShowInInspector]
	void CreateAsset()
	{
		DatabaseScriptableObject dso = ScriptableObject.CreateInstance<DatabaseScriptableObject>();

		dso.characters = characters;
		dso.items = items;
		dso.skills = skills;
		
		ScriptableObjectUtils.CreateAsset( dso, "database" );

		database = dso;
	}
}


//(c) Cination - Tsenkilidis Alexandros