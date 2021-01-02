﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using UnityEditor;

//This script stores object references for easier access
public class ObjectDatabase : ScriptableObject
{

	private static ObjectDatabase m_instance;
	public static ObjectDatabase core {
		get {
			if ( m_instance == null )
			{
				var assets = AssetDatabase.FindAssets( "t:ObjectDatabase" );
				m_instance = AssetDatabase.LoadAssetAtPath<ObjectDatabase>( assets[0] );
			}

			return m_instance;
		}
	}
	
	public ObjectDB import;

	//Options and actions
	public GameObject battleUIOptionPrefab;
	public GameObject battleUIActionsWindow;
	public GameObject battleUIActionDescriptionObject;
	
	//Character info
	public GameObject battleUICharacterInfoPrefab;
	public GameObject battleUICharacterInfoWindow;

	//Body
	public GameObject battleUIBody;
	
	//Used to spawn attribute changes
	public GameObject battleUIValuePrefab;

	//Used to display turn points
	public GameObject turnObject;

	//Used to display the cost of the selected action
	public GameObject actionCostObject;

	//Used to display warnings
	public GameObject warningObject;

	//Used to display the number of targets selected
	public GameObject actionTargetsObject;

	//Auto battle button object
	public GameObject autoBattleButtonObject;

	//Used to display the outcome of the encounter
	public GameObject outcomeWidow;

	//Player and enemy battler prafab
	public GameObject battlerPrefab;

	//Enemy team spawns parent object
	public GameObject enemyTeamSpawns;

	//Player teams spawns parent object
	public GameObject playerTeamSpawns;

	//Backrgound object
	public GameObject backgroundSpriteObject;

	//Combat Area FX Object
	public GameObject FXObject;

	//FX prefab
	public GameObject FXPrefab;

	//A list of audioclips
	public List<audioInfo> AudioClips = new List<audioInfo>();

	//A list of all sprites
	public List<spriteInfo> Sprites = new List<spriteInfo>();


}


//(c) Cination - Tsenkilidis Alexandros