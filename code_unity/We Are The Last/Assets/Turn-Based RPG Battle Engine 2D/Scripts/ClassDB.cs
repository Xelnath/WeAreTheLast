﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

//A collection of custom classes used across the asset

namespace ClassDB {
	
	[System.Serializable]
	public class item {
		public string name;
		public int id;
		public string description;

		//The amount of turn points it costs to use the item.
		public int turnPointCost;
		public int manaCost;
		public int superCost;
		
		//A list of functions to be called when the item is used
		public List<callInfo> functionsToCall = new List<callInfo>(); 

	}

	[System.Serializable]
	public class characterItemInfo {
		public int id;
		public float quantity;
	}

	[System.Serializable]
	public class targetProvider
	{
		public enum ArrowType
		{
			NormalHostile,
			SpecialHostile,
			NormalFriendly,
			SpecialFriendly,
		}

		public bool preserved = false;
		public ArrowType arrowType = ArrowType.NormalHostile;
		public List<callInfo> targetCalls = new List<callInfo>();
	}

	[System.Serializable]
	public class skill
	{

		public string name;
		public int id;
		public string description;
		public bool DEBUG = false;

		public int sacrificeReplacementId = -1;
		
		//Can the skill be used?
		[FormerlySerializedAs( "unlocked" )] 
		public bool activeSkill;

		public bool isAttack = false;

		//The amount of turn points it costs to use the skill
		public int turnPointCost;

		public int manaCost;
		public int superCost;

		// a list of target providing functions to run
		public List<targetProvider> targetProviders = new List<targetProvider>(); 
		
		//A list of functions to be called when the skill is used
		public List<callInfo> functionsToCall = new List<callInfo>();
		
		//A list of functions to be called when the round ends
		public List<callInfo> endOfRound = new List<callInfo>();

		//A list of functions to be called when the skill is sacrificed
		public List<callInfo> sacrificeActions = new List<callInfo>();
		
		public override string ToString()
		{
			return $"{name} ({id}) - {activeSkill} - pts: {turnPointCost} - mana: {manaCost}";
		}
	}

	[System.Serializable]
	public class character {

		public string name;
		public int id;
		public string description;
		public Sprite icon;

		public int speed;
		
		//Animator component
		public GameObject prefab;

		public AnimationAsset[] Animations;

		//A list of all available skills
		public List<int> skills = new List<int>();
		
		//A list of all available skills
		public List<int> ultimates = new List<int>();

		//A list of items available to the player by ids
		public List<characterItemInfo> items = new List<characterItemInfo>();

		//A list of character attributes
		public List<characterAttribute> characterAttributes = new List<characterAttribute>();

		//A list of A.I functions
		public List<callInfo> aiFunctions = new List<callInfo>();

		public List<callInfo> onDeath = new List<callInfo>();

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

				//Getting function data
				var functionsToCall = new List<callInfo>( skill.functionsToCall );
				return functionsToCall;
			}
			return new List<callInfo>() { };
		}

		public List<callInfo> getOnDeath()
		{
			return new List<callInfo>( onDeath );
		}

		public int getSpeed()
		{
			return speed;
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

		public IEnumerator endRound(InstanceID character, BattleManager.BattleManagerContext context)
		{
			for ( int i = 0; i < skills.Count; ++i )
			{
				var skillId = skills[i];
			    skill skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(skillId)];

			    var functionsToCall = skill.endOfRound;
			    if ( functionsToCall.Count > 0 )
			    {
				    BattleManager.BattleManagerContext c = new BattleManager.BattleManagerContext();
				    c.Init( character, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
				    c.functionQueue = functionsToCall;
				    c.activeSkillId = skillId;
				    c.actionTargets.Clear();
				    c.DEBUG = skill.DEBUG;
				    yield return BattleManager.core.functionQueueCaller( c );
			    }
			}

			if ( counterSkill >= 0 )
			{
				var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(counterSkill)];

				var functionsToCall = skill.endOfRound;
			    if ( functionsToCall.Count > 0 )
			    {
				    BattleManager.core.CurrentContext.Init( character, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
				    BattleManager.core.CurrentContext.functionQueue = functionsToCall;
  		            BattleManager.core.CurrentContext.activeSkillId = skill.id;
				    BattleManager.core.CurrentContext.actionTargets.Clear();

				    yield return BattleManager.core.functionQueueCaller( BattleManager.core.CurrentContext );
			    }
			}
		}

		public void Copy( character toCopy )
		{
			name = toCopy.name;
			id = toCopy.id;
			description = toCopy.description;
			icon = toCopy.icon;
			prefab = toCopy.prefab;
			aiFunctions = toCopy.aiFunctions;
			isActive = toCopy.isActive;
			counterSkill = toCopy.counterSkill;
			Animations = toCopy.Animations;

			skills = toCopy.skills.DeepClone();
			ultimates = toCopy.ultimates.DeepClone();
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

		public bool isComment => ( functionName.StartsWith( "-" ) || functionName.StartsWith( "/" ) || functionName.StartsWith(":") );

		public override string ToString()
		{
			return $"callInfo ({functionName}) - {isRunning}";
		}
	}

	[System.Serializable]
	public class characterInfo {
		public characterInfo( character toCopy )
		{
			characterCopy = new character();
			characterCopy.Copy( toCopy );
		}

		[System.Serializable]
		public class targetInfo
		{
			public targetProvider.ArrowType type = targetProvider.ArrowType.NormalHostile;
			public List<InstanceID> targetIds = new List<InstanceID>();
		}

		public List<targetInfo> preplannedTargets = new List<targetInfo>();
		public GameObject instanceObject;
		public GameObject uiObject;
		public GameObject spawnPointObject;
		public List<GameObject> threatArrows;
		public string currentAnimation = "idle";
		public AnimationAsset currentAnimationAsset;
		public int currentAnimationFrame = 0;
		public float currentAnimationTime = 0f;
		public InstanceID characterInstanceId;
		public bool isAlive = true;
		public character characterCopy;
		public int lastSkillUsed;

		public AnimationAsset GetAnimationAsset( string name )
		{
			AnimationAsset asset = null;
			if ( characterCopy.Animations == null )
			{
				return null;
			}

			foreach ( var animationAsset in characterCopy.Animations )
			{
				if ( animationAsset.Name == name )
				{
					asset = animationAsset;
					break;
				}
			}

			// Cheap and lazy
			if ( (asset?.Alternates?.Length ?? 0) > 0 )
			{
				bool chosen = false;
				foreach ( var alternateAsset in asset.Alternates )
				{
					switch (alternateAsset.Rule)
					{
						case AnimationAsset.AlternateRule.LowHealth:
							var index = FunctionDB.core.findAttributeIndexByName( "HP", characterCopy );
							var charAttribute = characterCopy.characterAttributes[index];

							if ( charAttribute.curValue < 2f )
							{
								asset = alternateAsset.Alternate;
								chosen = true;
							}
							break;
					}

					if ( chosen ) break;
				}
			}

			return asset;
		}

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
		public int manaPointCost;
		public int superCost;
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