using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//This script should be used in your main project in order to properly initiate battle.
//Please refer to the doc for more info.
public class VariableInitiator : MonoBehaviour {

	public static VariableInitiator core;

	//These variables should be modified on the main scene, before the battle scene is loaded.
	//The content of these variables will be copied over to Battle Manager

	[FormerlySerializedAs( "activePlayerTeam" )] [Tooltip("Current player's team.")]
	public List<int> initialPlayerTeam;

	[FormerlySerializedAs( "activeEnemyTeam" )] [Tooltip("Current enemy team.")]
	public List<int> initialEnemyTeam;

	[Tooltip("Starting character.")]
	public int startingCharacter;

	[Tooltip("Starting battle music id (from ObjectDB.cs).")]
	public int startingMusicId;

	[Tooltip("Index of main audio source.")]
	public int musicSourceIndex;


	[Tooltip("The id of the background sprite (from ObjectDB.cs).")]
	public int backgroundSpriteId;

	[Tooltip("Enable character AI.")]
	public bool autoBattle;

	[Tooltip("Maximum turn points available.")]
	public int maxTurnPoints;

	//On scene start
	void Start () {
		//If battle manager exists on the scene, it means that we are on a battle scene.
		//Thus, we should transfer our setup to the battle manager.
		if (BattleManager.core != null) {
			BattleManager.core.CurrentContext.attackerTeam = initialPlayerTeam;
			BattleManager.core.CurrentContext.defenderTeam = initialEnemyTeam;
			BattleManager.core.startingCharacter = startingCharacter;
			BattleManager.core.startingMusicId = startingMusicId;
			BattleManager.core.musicSourceIndex = musicSourceIndex;
			BattleManager.core.backgroundSpriteId = backgroundSpriteId;
			BattleManager.core.autoBattle = autoBattle;
			BattleManager.core.maxTurnPoints = maxTurnPoints;

			//Start battle sequence after variables have been transfered
			BattleManager.core.initiateBattle();
		}
	}

	void Awake () {
		if (core == null) {
			//Assigning reference
			core = this;
			//Allows to maintain the object active when loading a new scene
			DontDestroyOnLoad(this.gameObject);
		}
	}
}


//(c) Cination - Tsenkilidis Alexandros