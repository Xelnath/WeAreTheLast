using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using UnityEngine.Serialization;

//This script stores object references for easier access
public class ObjectDB : MonoBehaviour {

	public static ObjectDB core;
    public StoryControl story;

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
	public GameObject battleUIPopupPrefab;
	
	//Used to display health
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
	
	//Enemy Threat Arrow parent object
	public GameObject threatArrowSpawn;
	
	//Enemy Threat Arrow prefab
	[FormerlySerializedAs( "ThreatArrowPrefab" )] public GameObject NormalThreatArrowPrefab;
	[FormerlySerializedAs( "SpecialArrowPrefab" )] public GameObject SpecialThreatArrowPrefab;
	[FormerlySerializedAs( "HelpArrowPrefab" )] public GameObject FriendlyArrowPrefab;
	[FormerlySerializedAs( "SpecialHelpArrowPrefab" )] public GameObject SpecialFriendlyArrowPrefab;

	//A list of audioclips
	public List<audioInfo> AudioClips = new List<audioInfo>();

	//A list of all sprites
	public List<spriteInfo> Sprites = new List<spriteInfo>();

	void Awake () { if (core == null) { core = this; } }

}


//(c) Cination - Tsenkilidis Alexandros