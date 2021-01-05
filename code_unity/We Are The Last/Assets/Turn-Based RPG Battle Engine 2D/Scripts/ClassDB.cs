using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//A collection of custom classes used across the asset

namespace ClassDB {
	
	[System.Serializable]
	public class item {
		public string name;
		public int id;
		public string description;

		//The amount of turn points it costs to use the item.
		public int turnPointCost;
		
		//A list of functions to be called when the item is used
		public List<callInfo> functionsToCall = new List<callInfo>(); 

	}

	[System.Serializable]
	public class characterItemInfo {
		public int id;
		public float quantity;
	}

	[System.Serializable]
	public class skill {

		public string name;
		public int id;
		public string description;

		//Can the skill be used?
		public bool unlocked;

		//The amount of turn points it costs to use the skill
		public int turnPointCost;

		//A list of functions to be called when the skill is used
		public List<callInfo> functionsToCall = new List<callInfo>();
		
		//A list of functions to be called when the round ends
		public List<callInfo> endOfRound = new List<callInfo>();
	}

	[System.Serializable]
	public class character {

		public string name;
		public int id;
		public string description;
		public Sprite icon;

		//Animator component
		public RuntimeAnimatorController animationController;

		//A list of all available skills
		public List<int> skills = new List<int>();

		//A list of items available to the player by ids
		public List<characterItemInfo> items = new List<characterItemInfo>();

		//A list of character attributes
		public List<characterAttribute> characterAttributes = new List<characterAttribute>();

		//A list of A.I functions
		public List<callInfo> aiFunctions = new List<callInfo>();

		public int counterSkill = -1;
		
		//Is the character active
		public bool isActive;
		
		//Is the character being misdirected onto using the mentalist
		public bool isTaunting => characterAttributes.Any( x => x.name == "TAUNT" && x.curValue > 0 );

		public void useTauntCharge()
		{
			var attr = characterAttributes.Where( x => x.name == "TAUNT" && x.curValue > 0 );
			if ( attr.Any() )
			{
				attr.First().curValue--;
			}
		}

		public List<callInfo> getCounter()
		{
			if ( counterSkill > -1 )
			{
				//Getting skill data
				var skillIndex = FunctionDB.core.findSkillIndexById( counterSkill );
				var skill = Database.dynamic.skills[skillIndex];

				if ( skill.unlocked )
				{
					//Getting function data
					var functionsToCall = new List<callInfo>( skill.functionsToCall );
					return functionsToCall;
				}
			}
			return new List<callInfo>() { };
		}

		public void addDontReplaceAttribute( characterAttribute newAttribute )
		{
			for ( int i = 0; i < characterAttributes.Count; ++i )
			{
				if ( characterAttributes[i].name == newAttribute.name )
				{
					return;
				}
			}

			newAttribute.id = characterAttributes.Count;
			characterAttributes.Add( newAttribute );
		}

		public IEnumerator endRound()
		{
			for ( int i = 0; i < skills.Count; ++i )
			{
				var skillId = skills[i];
			    var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(skillId)];

			    var functionsToCall = skill.endOfRound;
			    if ( functionsToCall.Count > 0 )
			    {
				    BattleManager.core.CurrentContext.Init( id, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
				    BattleManager.core.CurrentContext.functionQueue = functionsToCall;
				    yield return BattleManager.functionQueueCaller( BattleManager.core.CurrentContext );
			    }
			}

			if ( counterSkill >= 0 )
			{
				var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(counterSkill)];

				var functionsToCall = skill.endOfRound;
			    if ( functionsToCall.Count > 0 )
			    {
				    BattleManager.core.CurrentContext.Init( id, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
				    BattleManager.core.CurrentContext.functionQueue = functionsToCall;
				    yield return BattleManager.functionQueueCaller( BattleManager.core.CurrentContext );
			    }
			}
		}

		public void Copy( character toCopy )
		{
			name = toCopy.name;
			id = toCopy.id;
			description = toCopy.description;
			icon = toCopy.icon;
			animationController = toCopy.animationController;
			aiFunctions = toCopy.aiFunctions;
			isActive = toCopy.isActive;
			counterSkill = toCopy.counterSkill;

			skills = toCopy.skills.DeepClone();
			items = toCopy.items.DeepClone();
			characterAttributes = toCopy.characterAttributes.DeepClone();
			aiFunctions = toCopy.aiFunctions.DeepClone();
		}

		public override string ToString()
		{
			return $"Character: {name} isTaunting {isTaunting}";
		}
	}

	[System.Serializable]
	public class characterAttribute {
		public string name;
		public int id;

		//The maximum value that the attribute can have
		public float maxValue;

		//The current value the attribute has
		public float curValue;
	}

	[System.Serializable]
	public class callInfo {
		public string functionName;
		public List<string> parametersArray = new List<string>();
		public bool waitForPreviousFunction;
		public bool isCoroutine;
		public bool isRunning;

		public override string ToString()
		{
			return $"callInfo ({functionName}) - {isRunning}";
		}
	}

	[System.Serializable]
	public class characterInfo {
		public GameObject instanceObject;
		public GameObject uiObject;
		public GameObject spawnPointObject;
		public string currentAnimation;
		public int characterId;
	}
	
	//Used by main action menu, not skills and items.
	[System.Serializable]
	public class actionInfo {
		public string name;
		public string description;

		//The functions to be called when the action is selected
		public List<string> functions = new List<string>();
	}

	//Used to display info about elements of the current action list
	[System.Serializable]
	public class curActionInfo {
		public GameObject actionObject;
		public string description;
		public int turnPointCost;
	}

	
	[System.Serializable]
	public class audioInfo {
		public int id;
		public AudioClip clip;
	}

	[System.Serializable]
	public class spriteInfo {
		public int id;
		public Sprite sprite;
	}
	
}

//(c) Cination - Tsenkilidis Alexandros