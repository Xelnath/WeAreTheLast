using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using System.Runtime.CompilerServices;
using ClassDB;
using Random = UnityEngine.Random;

//A class containing custom methods
public class BattleMethods : MonoBehaviour
{
  public static BattleMethods core;

  //---------------AI--------------------//
  //This is a sample AI function that simply runs the first skill available to the AI on a random target
  void sampleAi(BattleManager.BattleManagerContext context)
  {

    //Getting character
    var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
    var skill = selectAISkill(characterInstance);
    
    characterInstance.lastSkillUsed += 1;
    if (characterInstance.lastSkillUsed >= characterInstance.characterCopy.skills.Count)
    {
      characterInstance.lastSkillUsed = 0;
    }
    
    //Getting functions to call
    var functionsToCall = skill.functionsToCall;

    //Storing the functions list to functionQueue
    context.activeSkillId = skill.id;
    context.functionQueue = functionsToCall;
    BattleManager.core.StartCoroutine( BattleManager.core.functionQueueCaller( context ) );
  }
  
  void betrayalAi(BattleManager.BattleManagerContext context)
	  {
	    var characterId = context.activeCharacterId;

      var charInfo = BattleManager.core.findCharacterInstanceById(characterId);
	    
	    var attr = FunctionDB.core.findAttributeByName(  characterId, "BETRAYAL" );
	    if ( attr != null )
	    {
	      //This is a special Betrayal attack that decrements the Betrayal status -- could be anything so long as it has the proper End of Round calls
        //It might make more sense to manually add those functions to the end of round, if you want the betrayed AI to be more complex
        int skillId = 200;
        int skillIndex = FunctionDB.core.findSkillIndexById( skillId );	
        if ( skillIndex == -1 )	
        {	
          Debug.LogError( $"Unable to find skill by id {skillId}." );	
          return;	
        }	
		
        var skill = Database.dynamic.skills[skillIndex];
        
	      //Getting functions to call
	      var functionsToCall = skill.functionsToCall;
	      
	      context.activeSkillId = skill.id;  
	      context.functionQueue = functionsToCall;
	      
	      BattleManager.core.StartCoroutine( BattleManager.core.functionQueueCaller( context ) );
	    }
	  }
	
	  void changeSelfAi(BattleManager.BattleManagerContext context, string aiName)
    {
      character self = BattleManager.core.findCharacterInstanceById(context.activeCharacterId).characterCopy;
      
	    self.aiFunctions.Clear();
	    self.aiFunctions.Add(new callInfo()
	    {
	      functionName = aiName,
	      isRunning = true 
	    });
	    
	    BattleManager.setQueueStatus( context,  "changeSelfAi", false );
	  }
    //---------------AI--------------------//

    void checkBetrayed(BattleManager.BattleManagerContext context)
    {
	    context.actionTargets.Clear();
	    var attr = FunctionDB.core.findAttributeByName(  context.activeCharacterId, "BETRAYAL" );
	    if (attr != null)
      {
	      if (attr.curValue == 3 || attr.curValue == 0)  //3 being the current max value of Betray
	      {
          context.actionTargets.Add(context.activeCharacterId);

          if (attr.curValue == 0)
          {
            var character = BattleManager.core.findCharacterInstanceById(context.activeCharacterId).characterCopy;
            int attrIndex = FunctionDB.core.findAttributeIndexById(attr.id, character);
            character.characterAttributes.RemoveAt(attrIndex);
          }
        }
	    }
	    
	    BattleManager.setQueueStatus( context,  "checkBetrayed", false );
	  }
  /*
   This function is used to prepare the AI's targets at the top of the round and when called (ie Taunt,
   which necessitates recalculation)
   Currently it, like sampleAi, just generates it for whatever the first slot is
   Currently allows enemies to target players that have already been killed that turn
   */
  public void preplanAI(InstanceID instanceID)
  {
    var characterInstance = BattleManager.core.findCharacterInstanceById( instanceID );

    var skill = selectAISkill(characterInstance);

    //Getting functions to call
    while ( characterInstance.preplannedTargets.Count < skill.targetProviders.Count )
    {
       characterInfo.targetInfo newTarget = new characterInfo.targetInfo();
       characterInstance.preplannedTargets.Add( newTarget );
    }

    for ( int i = 0; i < skill.targetProviders.Count; ++i )
    {
      characterInfo.targetInfo preplannedTargetInfos = characterInstance.preplannedTargets[i];
      targetProvider staticTP = skill.targetProviders[i];
      preplannedTargetInfos.type = staticTP.arrowType;
      
      // Skip calls if none are needed
      if ( staticTP.targetCalls.Count == 0 )
      {
        continue;
      }

      // Execute a command stack otherwise
      BattleManager.BattleManagerContext c = new BattleManager.BattleManagerContext( BattleManager.core.CurrentContext ){};
      c.Init( instanceID, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
			c.functionQueue = staticTP.targetCalls;
			c.activeSkillId = skill.id;
      if ( staticTP.preserved )
      {
        c.actionTargets.AddRange( preplannedTargetInfos.targetIds );
      }

      c.DEBUG = skill.DEBUG;

      for ( c.runningFunctionIndex = 0; c.runningFunctionIndex < c.functionQueue.Count; c.runningFunctionIndex++ )
      {
        var ftc = c.functionQueue[c.runningFunctionIndex];
        if ( ftc.isComment ) continue;
        
        BattleManager.core.callFtc( c, ftc, c.runningFunctionIndex );
      }

      preplannedTargetInfos.targetIds.Clear();
      preplannedTargetInfos.targetIds.AddRange( c.actionTargets );
    }
  }

  //This function emulates the 2 targeting functions, but sets the charInfo targetIds instead of the context.actionTargets
  //I plan on creating a function to reduce redundancy between this and the aforemtnioned targeting functions
  // void preplanAITargets(characterInfo characterInstance, callInfo functionInfo)
  // {
  //   string targetFunctionName = functionInfo.functionName;
  //   if (targetFunctionName.Equals("autoSelectTargets"))
  //   {
  //     characterInstance.targetIds.Clear();
  //     int targetLimit = int.Parse(functionInfo.parametersArray[0].Substring(4));
  //     bool allowFriendly = functionInfo.parametersArray[1].Equals("bool:true");
  //     bool allowHostile = functionInfo.parametersArray[2].Equals("bool:true");
  //
  //     if (allowFriendly) { characterInstance.targetIds.AddRange(BattleManager.core.activeEnemyTeam); }
  //     if (allowHostile) { characterInstance.targetIds.AddRange(BattleManager.core.activePlayerTeam); }
  //     
  //     //Excluding invalid characters
  //     List<InstanceID> deadCharacters = new List<InstanceID>();
  //     for (int i = 0; i < characterInstance.targetIds.Count; i++)
  //     {
  //       var characterId = characterInstance.targetIds[i];
  //       if (!BattleManager.core.findCharacterInstanceById(characterId).isAlive)
  //       {
  //         deadCharacters.Add(characterId);
  //       }
  //     }
  //     foreach (var index in deadCharacters)
  //     {
  //       characterInstance.targetIds.Remove(index);
  //     }
  //     
  //     List<InstanceID> selected = new List<InstanceID>();
  //     while ( targetLimit > 0 && characterInstance.targetIds.Count > 0 )
  //     {
  //       int index = Random.Range( 0, characterInstance.targetIds.Count );
  //       var charId = characterInstance.targetIds[index];
  //       characterInstance.targetIds.RemoveAt( index );
  //       if(selected.Count < targetLimit) {
  //         selected.Add(charId);
  //       }
  //     }
  //     characterInstance.targetIds = selected;
  //   } else if (targetFunctionName.Equals("selectCharacter"))
  //   {
  //     characterInstance.targetIds.Clear();
  //     
  //     bool targetSameTeam = functionInfo.parametersArray[0].Equals("bool:true");
  //     int targetLimit = int.Parse(functionInfo.parametersArray[1].Substring(4));
  //
  //     var playerTeam = new List<InstanceID>(BattleManager.core.activePlayerTeam);
  //     var enemyTeam = new List<InstanceID>(BattleManager.core.activeEnemyTeam);
  //     
  //     if (targetSameTeam){
  //       foreach (var enemy in enemyTeam)
  //       {
  //         characterInstance.targetIds.Add(enemy);
  //       }
  //     } else {  
  //       foreach (var player in playerTeam)
  //       {
  //         characterInstance.targetIds.Add(player);
  //       }      
  //     }
  //
  //     List<InstanceID> selected = new List<InstanceID>();
  //     while ( targetLimit > 0 && characterInstance.targetIds.Count > 0 )
  //     {
  //       int index = UnityEngine.Random.Range( 0, characterInstance.targetIds.Count );
  //       var charId = characterInstance.targetIds[index];
  //       characterInstance.targetIds.RemoveAt( index );
  //       selected.Add(charId);
  //     }
  //     characterInstance.targetIds = selected;
  //     
  //     //Excluding invalid characters
  //     List<InstanceID> deadCharacters = new List<InstanceID>();
  //     for (int i = 0; i < characterInstance.targetIds.Count; i++)
  //     {
  //       var characterId = characterInstance.targetIds[i];
  //       if (!BattleManager.core.findCharacterInstanceById(characterId).isAlive)
  //       {
  //         deadCharacters.Add(characterId);
  //       }
  //     }
  //     foreach (var index in deadCharacters)
  //     {
  //       characterInstance.targetIds.Remove(index);
  //     }
  //
  //     //Getting random character
  //     InstanceID randIndex = AIGetRandomTarget( characterInstance.targetIds );
  //     characterInstance.targetIds.Clear();
  //     //Adding character to targets
  //     characterInstance.targetIds.Add( randIndex );
  //   }
  // }

  public skill selectAISkill(characterInfo characterInstance)
  {
    var curChar = characterInstance.characterCopy;
    //Getting skills
    var skillIds = curChar.skills;

    int nextSkill = 0;
    if (characterInstance.lastSkillUsed != -1)
    {
      nextSkill = characterInstance.lastSkillUsed + 1;
      if (nextSkill >= curChar.skills.Count)
      {
        nextSkill = 0;
      }
    }
    
    int skillIndex = FunctionDB.core.findSkillIndexById( skillIds[nextSkill] );
    if ( skillIndex == -1 )
    {
      Debug.LogError( $"Unable to find skill by id {skillIds[nextSkill]}." );
      return null;
    }

    var skill = Database.dynamic.skills[skillIndex];

    return skill;
  }
  
  /*
	This function is used to subtract turn points.
	Action Type should be 0 if the action is a skill, or 1 if it is an item.
	Action Id is the id of the action (skill or item) in the database.
	If actionId is -1 or action type is -1, the function will set remaining points to 0.
	 */
  public void subtractTurnPoints( BattleManager.BattleManagerContext context, int actionType, int actionId )
  {
    if ( actionId != -1 && actionType != -1 )
    {
      int tp = BattleManager.core.turnPoints;
      int requiredTp = 0;
      int requiredMana = 0;
      int requiredSuper = 0;

      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;

      switch (actionType)
      {
        case 0:
          int skillIndex = FunctionDB.core.findSkillIndexById( context.activeSkillId );
          requiredTp = Database.dynamic.skills[skillIndex].turnPointCost;
          requiredMana = Database.dynamic.skills[skillIndex].manaCost;
          requiredSuper = Database.dynamic.skills[skillIndex].superCost;
          break;
        case 1:
          requiredTp = Database.dynamic.items[FunctionDB.core.findItemIndexById( actionId )].turnPointCost;
          requiredMana = Database.dynamic.items[FunctionDB.core.findItemIndexById( actionId )].manaCost;
          requiredSuper = Database.dynamic.items[FunctionDB.core.findItemIndexById( actionId )].superCost;
          break;
        default:
          Debug.Log( "Invalid action type. Set 0 for skill or 1 for item." );
          break;
      }
      
            
      var index = FunctionDB.core.findAttributeIndexByName( "MP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        if ( attribute.curValue > requiredMana )
        {
          attribute.curValue -= requiredMana;
        }
        else
        {
          attribute.curValue = 0;
        }
      }
      var superIndex = FunctionDB.core.findAttributeIndexByName( "SP", character );
      if ( superIndex >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[superIndex];
        if ( attribute.curValue > requiredSuper )
        {
          attribute.curValue -= requiredSuper;
        }
        else
        {
          attribute.curValue = 0;
        }
      }

      //Do you have enought turn points?
      if ( ( tp - requiredTp ) > 0 )
      {
        BattleManager.core.turnPoints -= requiredTp;
      }
      else
      {
        //Normally this statement is non-accessible as an action cannot be accessed if the player doesn't have enough turn points to do so.
        BattleManager.core.turnPoints = 0;
      }
    }
    else BattleManager.core.turnPoints = 0;

    BattleManager.setQueueStatus( context,  "subtractTurnPoints", false );

  }

  void selectTargets(BattleManager.BattleManagerContext context, int targetLimit, bool allowFriendly, bool allowHostile )
  { 
      context.targetLimit = targetLimit;
      var targets = BattleGen.core.getValidTargets( context, allowFriendly, allowHostile, 99 );
      
      context.actionTargets.Clear();
      
      while ( context.actionTargets.Count < targetLimit && targets.Count > 0 )
      {
        //Getting random character
        InstanceID randIndex = AIGetRandomTarget( targets );

        context.actionTargets.Add( randIndex );
        targets.Remove( randIndex );
      }
      
      BattleManager.setQueueStatus( context,  "selectTargets", false );
  }
  
  void selectFirstNTargets(BattleManager.BattleManagerContext context, int targetLimit, bool allowFriendly, bool allowHostile )
  { 
      context.targetLimit = targetLimit;
      var targets = new List<InstanceID>( context.actionTargets );
      
      context.actionTargets.Clear();
      
      while ( context.actionTargets.Count < targetLimit && targets.Count > 0 )
      {
        //Getting random character
        InstanceID randIndex = targets[0];

        context.actionTargets.Add( randIndex );
        targets.Remove( randIndex );
      }
      
      BattleManager.setQueueStatus( context,  "selectFirstNTargets", false );
  }

  void preselectedTargets( BattleManager.BattleManagerContext context, int targetIndex )
  {
    //Getting character
    var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
    if (characterInstance.preplannedTargets.Count > targetIndex)
    {
      var pt = characterInstance.preplannedTargets[targetIndex];
      context.actionTargets = new List<InstanceID>( pt.targetIds );
    } 
    BattleManager.setQueueStatus( context,  "preselectedTargets", false );
  }
  
  void selectTeam( BattleManager.BattleManagerContext context, bool allowFriendly, bool allowHostile )
  {
    //Getting character
    context.targetLimit = 99;
    context.actionTargets = BattleGen.core.getValidTargets( context, allowFriendly, allowHostile, 99 );
    BattleManager.setQueueStatus( context,  "selectTeam", false );
  }

  void targetSelf(BattleManager.BattleManagerContext context)
  {
    context.actionTargets.Add(context.activeCharacterId);
    BattleManager.setQueueStatus( context,  "targetSelf", false );
  }
  
  void clearTargets( BattleManager.BattleManagerContext context )
  {
    context.actionTargets.Clear();
    BattleManager.setQueueStatus( context,  "clearTargets", false );
  }
  

  /*
	Target selection used by skills and items.
	If targetSameTeam is true, a list of allies will be displayed to the player to choose from.
	Otherwise the enemy team members will be displayed.
	Only active character will be displayed.
	TargetLimit is the maximum amount of targets the player can select. All targets are fed into the action targets list of BattleManager.
	 */

  void selectCharacter( BattleManager.BattleManagerContext context, bool targetSameTeam, int targetLimit, int preselectedIndex )
  {

    context.targetLimit = targetLimit;
    context.actionTargets.Clear();

    if ( !( BattleManager.core.autoBattle || BattleManager.core.activeTeam == 1 ) )
    {
      if ( targetSameTeam ) BattleGen.core.targetGen( context, true, false, targetLimit );
      else BattleGen.core.targetGen( context, false, true, targetLimit );

    }
    else
    {

      //Getting character info
      var instance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      if ( preselectedIndex > -1 )
      {
        if ( preselectedIndex >= instance.preplannedTargets.Count )
        {
          Debug.LogError( $"Skill {context.activeSkillId} doesn't have preplanned target ID {preselectedIndex}." );
        }
        else
        {
          context.actionTargets.AddRange( instance.preplannedTargets[preselectedIndex].targetIds );
        }

      }
      else
      {
        Debug.Log( $"Don't do this anymore. This cannot be predicted. Fix those skills." );
        context.targetLimit = 99;
        context.actionTargets = BattleGen.core.getValidTargets( context, targetSameTeam, !targetSameTeam, targetLimit );
      }
    }

    BattleManager.setQueueStatus( context,  "selectCharacter", false );
  }
  
  enum mathOperation
  {
    equal,
    equalGreaterThan,
    equalLessThan,
    greaterThan,
    lessThan,
  }

  private void GetOperationValue( string value, out mathOperation op, out float v )
  {
    
    //This function converts a string to an expression and assings the derived value to the attribute
    v = 0f;
    op = mathOperation.equal;

    switch (value.Substring( 0, 1 ))
    {
      case "=":
        op = mathOperation.equal;
        v = float.Parse( value.Substring( 1 ) );;
        break;
      case ">":
        if ( value.Substring( 0, 2 ) == ">=" )
        {
          op = mathOperation.equalGreaterThan;
          v = float.Parse( value.Substring( 2 ) );;
        }
        else
        {
          op = mathOperation.greaterThan;
          v = float.Parse( value.Substring( 1 ) );
        }

        break;
      case "<":
        if ( value.Substring( 0, 2 ) == "<=" )
        {
          op = mathOperation.equalLessThan;
          v = float.Parse( value.Substring( 2 ) );;
        }
        else
        {
          op = mathOperation.lessThan;
          v = float.Parse( value.Substring( 1 ) );
        }

        break;
      default:
        break;
    }
  }

  private bool operationResult(float attrValue, mathOperation op, float v)
  {
    bool result = false;
    switch ( op )
      {
        case mathOperation.equal:
          if ( Mathf.Approximately( attrValue, v ) )
          {
            result = true;
          }

          break;
        case mathOperation.equalGreaterThan:
          if ( attrValue >= v )
          {
            result = true;
          }
          break;
        case mathOperation.equalLessThan:
          if ( attrValue <= v )
          {
            result = true;
          }
          break;
        case mathOperation.greaterThan:
          if ( attrValue > v )
          {
            result = true;
          }
          break;
        case mathOperation.lessThan:
          if ( attrValue < v )
          {
            result = true;
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

    return result;
  }

  void filterTargets( BattleManager.BattleManagerContext context, string attrName, string value )
  {

    GetOperationValue( value, out var op, out float v );

    List<InstanceID> targets = new List<InstanceID>(context.actionTargets);

    for ( int i = targets.Count - 1; i >= 0; --i )
    {
      InstanceID charId = targets[i];
      
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      
      var index = FunctionDB.core.findAttributeIndexByName( attrName, character );
      float attrValue = 0f;
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attrValue = attribute.curValue;
      }

      bool remove = !operationResult(attrValue, op, v);
      if ( remove )
      {
         if ( context.DEBUG ) Debug.Log( $"Removing {character} from actionTargets for skill {context.activeSkillId}. Test: {attrName} {value}" );
          context.actionTargets.RemoveAt( i );
      }
      else
      {
         if ( context.DEBUG ) Debug.Log( $"Keeping {character} from actionTargets for skill {context.activeSkillId}. Test: {attrName} {value}" );
      }
    }
    
    
    BattleManager.setQueueStatus( context,  "filterTargets", false );
  }
  
  void filterTargetsIfAttributeIsNotCurrentActorId( BattleManager.BattleManagerContext context, string attrName )
  {
    
    List<InstanceID> targets = new List<InstanceID>(context.actionTargets);

    for ( int i = targets.Count - 1; i >= 0; --i )
    {
      InstanceID charId = targets[i];
      
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      
      var index = FunctionDB.core.findAttributeIndexByName( attrName, character );
      float attrValue = 0f;
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attrValue = attribute.curValue;
      }

      if ( Mathf.RoundToInt(attrValue) != context.activeCharacterId.ID )
      {
        if ( context.DEBUG ) Debug.Log( $"Removing {character} from actionTargets for skill {context.activeSkillId}" );
        context.actionTargets.RemoveAt( i );
      }
      else
      {
        if ( context.DEBUG ) Debug.Log( $"Keeping {character} from actionTargets for skill {context.activeSkillId}. {attrName} matched" );
      }
    }
    
    BattleManager.setQueueStatus( context,  "filterTargetsIfAttributeIsNotCurrentActorId", false );
  }

  void filterTargetsAttributeValueHighest(BattleManager.BattleManagerContext context, string attrName, int limit)
  {
    List<InstanceID> targets = new List<InstanceID>(context.actionTargets);
    var pass = targets
      .OrderBy( x => FunctionDB.core.findAttributeByName( x, attrName )?.curValue ?? 0f )
      .ToList();

    context.actionTargets = pass.GetRange( 0, Mathf.Min( limit, pass.Count ) );

    BattleManager.setQueueStatus( context,  "filterTargetsAttributeValueHighest", false );
  }
  
  void filterTargetsAttributeValueLowest(BattleManager.BattleManagerContext context, string attrName, int limit)
  {
    List<InstanceID> targets = new List<InstanceID>(context.actionTargets);
    var pass = targets
      .OrderByDescending( x => FunctionDB.core.findAttributeByName( x, attrName )?.curValue ?? 0f )
      .ToList();

    context.actionTargets = pass.GetRange( 0, Mathf.Min( limit, pass.Count ) );

    BattleManager.setQueueStatus( context,  "filterTargetsAttributeValueLowest", false );
  }
  
  void updateAITargeting(BattleManager.BattleManagerContext context)
  {
    BattleManager.core.replanAI();
    
    BattleManager.setQueueStatus( context,  "updateAITargeting", false );
  }

  private InstanceID AIGetRandomTarget( List<InstanceID> targets )
  {
    List<InstanceID> tauntTargets = new List<InstanceID>();
    for ( int i = 0; i < targets.Count; ++i )
    {
      //Id
      var charId = targets[i];

      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      if ( character.isTaunting )
      {
        tauntTargets.Add( charId );
      }
    }

    // Prefer taunted
    if ( tauntTargets.Count > 0 )
    {
      var charIndex = UnityEngine.Random.Range( 0, tauntTargets.Count );
      var charId = tauntTargets[charIndex];
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      if ( character.isTaunting )
      {
        character.useTauntCharge();
      }

      return charId;
    }

    return targets[UnityEngine.Random.Range( 0, targets.Count )];
  }


  /*
	This function waits until the player has finished selecting targets before marking itself as no running.
	In order for this function to take effect, please enable "waitForPrevious" on the function that follows in the function chain.
	 */

  IEnumerator waitForSelection( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 
    //Terminate current function chain if no characters were selected ?
    bool terminateOnEmpty = (bool) parms[1];
    //Has the target limit been reached
    bool limitReached = false;

    while ( true )
    {
      //Is the amount of tagrets currently selected equal to the target limit?
      limitReached = context.targetLimit ==
                     context.actionTargets.Count;

      //If the previous is true, the function is told to terminate if the player has not selected anything, and the player has indeed selected no targets -
      if ( limitReached && terminateOnEmpty && context.actionTargets.Count == 0 )
      {
        //Stop the execution of the function chain by erasing it.
        context.functionQueue.Clear();
        break;
      }
      else if ( limitReached )
      {
        //Otheriwse, if the limit has been simply reached, mark this function as not running.
        BattleManager.setQueueStatus( context,  "waitForSelection", false );
        break;
      }

      yield return new WaitForEndOfFrame();

    }

  }


  //Waiting for a fixed amount of seconds
  IEnumerator wait( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //Time to wait in seconds.
    float timeToWait = (float) parms[1];
    //Waiting
    yield return new WaitForSeconds( timeToWait );
    //Marking function as not running.
    BattleManager.setQueueStatus( context,  "wait", false );
  }


  // Provoke reaction from enemy if it can counter
  IEnumerator provokeReaction( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    List<BattleManager.BattleManagerContext> reactionSet = new List<BattleManager.BattleManagerContext>();
    
    //For each action target
    foreach ( InstanceID target in context.actionTargets )
    {

      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById(target);
      var character = characterInstance.characterCopy;

      if ( !character.isActive ) continue;

      int blockIndex = FunctionDB.core.findAttributeIndexByName( "COUNTER", character );
      if ( blockIndex < 0 ) continue;

      //Getting attribute
      characterAttribute attribute = character.characterAttributes[blockIndex];

      //Checking value
      if ( attribute.curValue > 0 )
      {
        InstanceID counterActorID = target;
        character counteringCharacter = character;
        int counterSourceIndex = FunctionDB.core.findAttributeIndexByName( "COUNTERSOURCE", character );
        if ( counterSourceIndex >= 0 )
        {
          //Getting attribute
          characterAttribute counterSource = character.characterAttributes[counterSourceIndex];
          counterActorID = new InstanceID( Mathf.FloorToInt( counterSource.curValue ) );
          counteringCharacter = BattleManager.core.findCharacterInstanceById(counterActorID).characterCopy;
        }
        
        var c = new BattleManager.BattleManagerContext();
        c.actionTargets = new List<InstanceID>() { context.activeCharacterId };
        c.Init( counterActorID, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
        c.targetLimit = 1;
        c.functionQueue = counteringCharacter.getCounter();
        reactionSet.Add( c );
      }
    }

    //Marking function as not running.
    BattleManager.setQueueStatus( context,  "provokeReaction", false );

    while( reactionSet.Count > 0 )
    {
      BattleManager.core.ReactionContext = reactionSet[0];
      StartCoroutine( BattleManager.reactionQueueCaller( reactionSet.ToArray() ) );
      reactionSet.RemoveAt( 0 );
      yield return new WaitForEndOfFrame();
    }

    //Waiting
    yield return new WaitForEndOfFrame();
  }

  //Waiting for a reaction Queue to end
  IEnumerator waitForReaction( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    while ( BattleManager.core.ReactionContext != null && BattleManager.core.ReactionContext.functionQueue.Count > 0 )
    {
      yield return new WaitForEndOfFrame();
    }

    //Marking function as not running.
    BattleManager.setQueueStatus( context,  "waitForReaction", false );
  }

  /*
	Moving currently active character to first selected target
	 */
  IEnumerator moveToTarget( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //The distance to maintain between the character and the target.
    float distance = (float) parms[1];

    //Movements speed.
    float speed = (float) parms[2];

    //Active character id and destination character id
    var sourceCharId = context.activeCharacterId;
    InstanceID destinationCharId = null;

    if ( context.actionTargets.Count > 0 )
      destinationCharId = context.actionTargets[0];
    else Debug.Log( "No targets selected" );

    if ( destinationCharId != null )
    {

      //Getting source char gameObject
      GameObject sourceCharObject = FunctionDB.core.findCharInstanceGameObjectById( sourceCharId );
      //Getting dest char gameObject
      GameObject destinationCharObject = FunctionDB.core.findCharInstanceGameObjectById( destinationCharId );

      //Getting direction and modifying distance
      var sourceX = sourceCharObject.transform.position.x;
      var destX = destinationCharObject.transform.position.x;
      float distanceMod = ( sourceX - destX ) > 0 ? distance : -distance;

      //Moving character
      var destPos = destinationCharObject.transform.position;
      var destPosNew = new Vector3( destPos.x + distanceMod, destPos.y, destPos.z );

      while ( true )
      {

        if ( sourceCharObject.transform.position == destPosNew ) break;
        sourceCharObject.transform.position =
          Vector3.MoveTowards( sourceCharObject.transform.position, destPosNew, speed );

        yield return new WaitForEndOfFrame();

      }

    }

    BattleManager.setQueueStatus( context,  "moveToTarget", false );
  }

  IEnumerator stepForward( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //The distance to move forward
    float distance = (float) parms[1];

    //Movements speed.
    float speed = (float) parms[2];

    //Active character
    var sourceCharId = context.activeCharacterId;

    //Getting source char gameObject
    GameObject sourceCharObject = FunctionDB.core.findCharInstanceGameObjectById( sourceCharId );

    //Getting direction and modifying distance
    var sourceX = sourceCharObject.transform.position.x;
    Vector3 scale = sourceCharObject.transform.lossyScale;
    //Make x adjustment relative to direction faced
    float distanceMod = distance * -Mathf.Sign(scale.x);

    //Moving character
    var destPosNew = new Vector3( sourceX + distanceMod, sourceCharObject.transform.position.y, sourceCharObject.transform.position.z );

    while ( true )
    {

      if ( sourceCharObject.transform.position == destPosNew ) break;
      sourceCharObject.transform.position =
        Vector3.MoveTowards( sourceCharObject.transform.position, destPosNew, speed );

      yield return new WaitForEndOfFrame();

    }

    BattleManager.setQueueStatus( context,  "stepForward", false );
  }
  
  //Moving battler pack to spawn point
  IEnumerator moveBack( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //Movement speed
    float speed = (float) parms[1];

    //Getting active char id
    InstanceID charId = context.activeCharacterId;

    //Getting character object
    GameObject charObject = FunctionDB.core.findCharInstanceGameObjectById( charId );
    //Getting spawn point object
    GameObject spawnPointObject = FunctionDB.core.findCharSpawnById( charId );


    //Moving character back
    var destPos = spawnPointObject.transform.position;

    while ( true )
    {

      if ( charObject.transform.position == destPos ) break;
      charObject.transform.position = Vector3.MoveTowards( charObject.transform.position, destPos, speed );

      yield return new WaitForEndOfFrame();
    }

    BattleManager.setQueueStatus( context,  "moveBack", false );

  }

  IEnumerator moveTargetHome( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //Movement speed
    float speed = (float) parms[1];

    //Getting active char id
    foreach (var target in context.actionTargets)
    {
      //Getting character object
      GameObject charObject = FunctionDB.core.findCharInstanceGameObjectById( target );
      //Getting spawn point object
      GameObject spawnPointObject = FunctionDB.core.findCharSpawnById( target );


      //Moving character back
      var destPos = spawnPointObject.transform.position;

      while ( true )
      {
        if ( charObject.transform.position == destPos ) break;
        charObject.transform.position = Vector3.MoveTowards( charObject.transform.position, destPos, speed );

        yield return new WaitForEndOfFrame();
      }
    }
    
    BattleManager.setQueueStatus( context,  "moveTargetHome", false );
  }
  
  void turnTarget(BattleManager.BattleManagerContext context )
  {
    //Getting target char ids
    foreach (var target in context.actionTargets)
    {
      //Getting character object
      GameObject charObject = FunctionDB.core.findCharInstanceGameObjectById( target );
      charObject.transform.Rotate(0, 180, 0);
    }
    
    BattleManager.setQueueStatus( context,  "turnTarget", false );
  }
  
  void swapTargetTeam( BattleManager.BattleManagerContext context)
  {
    GameObject sourceCharObject = FunctionDB.core.findCharInstanceGameObjectById(context.activeCharacterId);

    foreach (var instanceID in context.actionTargets)
    {
    
      var character = BattleManager.core.findCharacterInstanceById(instanceID).characterCopy;
      
      List<InstanceID> teammateIds = new List<InstanceID>();
      
      //Determinig which team the character is on
      if (BattleManager.core.activePlayerTeam.Contains(instanceID))
      {
        teammateIds = BattleManager.core.activeEnemyTeam;
        BattleManager.core.activePlayerTeam.Remove(instanceID);
        BattleManager.core.activeEnemyTeam.Add(instanceID);
      }
      else
      {
        teammateIds = BattleManager.core.activePlayerTeam;
        BattleManager.core.activeEnemyTeam.Remove(instanceID);
        BattleManager.core.activePlayerTeam.Add(instanceID);
      }
      
      //Set placement of new to-be spawnpoint
      Vector3 mindControllerSpawn = FunctionDB.core.findCharSpawnById(teammateIds[teammateIds.Count-2]).transform.position;
      mindControllerSpawn += new Vector3(Random.Range(-2, 2)/2f, -1, 0);

      var oldSpawn = FunctionDB.core.findCharSpawnById(instanceID);
	      
      //Create new spawnpoint, otherwise set old spawn
      if (BattleManager.core.findCharacterInstanceById(instanceID).spawnPointObject.gameObject.name.Contains("MindControlled")) 
      {  //Current spawn is a MindControlled spawn
        FunctionDB.core.setSpawn(instanceID, sourceCharObject.transform.parent.gameObject);
        Destroy(oldSpawn);
      }
      else
      {
        GameObject newSpawn = Instantiate(new GameObject($"MindControlled{character.id}SpawnPoint"), mindControllerSpawn,
          Quaternion.identity, sourceCharObject.transform.parent.transform.parent);
        FunctionDB.core.setSpawn(instanceID, newSpawn);
      }

      //Displaying debuff
        FunctionDB.core.StartCoroutine(
          FunctionDB.core.displayValue(
            FunctionDB.core.findCharInstanceGameObjectById( instanceID ),
            "Dominated!",	
            "A9A9A9",	
            string.Empty,	
            0f, 0.7f ) );
    }

    BattleManager.setQueueStatus( context,  "swapTargetTeam", false );
  }

  void setOldSpawn(BattleManager.BattleManagerContext context)
  {
    
    //Even though a character has swapped teams, its parent should still be its old spawn
    GameObject character = FunctionDB.core.findCharInstanceGameObjectById(context.activeCharacterId);
    GameObject oldSpawn = FunctionDB.core.findCharSpawnById(context.activeCharacterId);
    FunctionDB.core.setSpawn(context.activeCharacterId, character.gameObject.transform.parent.gameObject);
    if (oldSpawn != character.gameObject.transform.parent.gameObject)
    {
      Destroy(oldSpawn);
    }
    
    BattleManager.setQueueStatus( context,  "setOldSpawn", false );
  }
  
  void addOrChangeAttribute( BattleManager.BattleManagerContext context, bool self, string attributeName, string value, float maxValue, bool showText )
  {

    //This function converts a string to an expression and assings the derived value to the attribute
    float v;
    bool set = false;

    switch (value.Substring( 0, 1 ))
    {
      case "-":
        v = -float.Parse( value.Substring( 1 ) );
        break;
      case "+":
        v = float.Parse( value.Substring( 1 ) );
        break;
      case "=":
        v = float.Parse( value.Substring( 1 ) );
        set = true;
        break;
      default:
        v = float.Parse( value );
        set = true;
        break;
    }

    forEachCharacterDo( context, self,
      (instanceID, character) => {

        var index = FunctionDB.core.findAttributeIndexByName( attributeName, character );
        if ( index == -1 )
        {
          character.addDontReplaceAttribute( new characterAttribute
          {
            name = attributeName,
            curValue = v,
            maxValue = maxValue
          } );
        }
        else
        {
          characterAttribute attribute = character.characterAttributes[index];

          //Applying change
          if ( !set ) attribute.curValue = ( attribute.curValue + v ) > 0 ? ( attribute.curValue + v ) : 0;
          else attribute.curValue = v > 0 ? v : 0;
        }
        
        if ( showText )
        {
          //Displaying change	
          FunctionDB.core.StartCoroutine( FunctionDB.core.displayAttributeValue(	
            FunctionDB.core.findCharInstanceGameObjectById( instanceID ), v, index,	
            0, 1.3f ) );
        }
      } );

    BattleManager.setQueueStatus( context,  "addOrChangeAttribute", false );
  }

  void setAttributeToActiveCharacter( BattleManager.BattleManagerContext context, bool self, string attributeName )
  {
    forEachCharacterDo( context, self,
      (instanceID, character) => {

        var index = FunctionDB.core.findAttributeIndexByName( attributeName, character );
        if ( index == -1 )
        {
          character.addDontReplaceAttribute( new characterAttribute
          {
            name = attributeName,
            curValue = context.activeCharacterId.ID,
            maxValue = 1000
          } );
        }
        else
        {
          characterAttribute attribute = character.characterAttributes[index];
          attribute.curValue = context.activeCharacterId.ID;
        }
      } );

    BattleManager.setQueueStatus( context,  "setAttributeToActiveCharacter", false );
  }

  void displayAttributeText( BattleManager.BattleManagerContext context, bool self, string attributeName )
  {
    forEachCharacterDo( context, self,
      (instanceID, character) => {

        var index = FunctionDB.core.findAttributeIndexByName( attributeName, character );
        if ( index > -1 )
        {
          characterAttribute attribute = character.characterAttributes[index];
          //Displaying change	
          FunctionDB.core.StartCoroutine( FunctionDB.core.displayAttributeValue(	
            FunctionDB.core.findCharInstanceGameObjectById( instanceID ), attribute.curValue, index,	
            0, 1.3f ) );
        }
      } );

    BattleManager.setQueueStatus( context,  "displayAttributeText", false );
  }

  /*
	This function allows to change any of the active player or target attributes.
	Attribute id is the id of the attribute to change.
	The value is a string which represents the modification.
	Writing +10, for example will add 10 units to the attribute, while writing -10 will subtract 10.
	However, writing simply 10 will result into the attribute being set to 10.
	If self is true, the attribute of the currently active character will be modified. However, if self is false, the attributes (with teh specified ids) of all
	selected targets will be modified.
	*/
  void changeAttribute( BattleManager.BattleManagerContext context, bool self, int attrId, string value, bool showValue )
  {

    //This function converts a string to an expression and assings the derived value to the attribute
    float v;
    bool set = false;

    switch (value.Substring( 0, 1 ))
    {
      case "-":
        v = -float.Parse( value.Substring( 1 ) );
        break;
      case "+":
        v = float.Parse( value.Substring( 1 ) );
        break;
      case "=":
        v = float.Parse( value.Substring( 1 ) );
        set = true;
        break;
      default:
        v = float.Parse( value );
        set = true;
        break;
    }

    forEachCharacterDo( context, self, ( instanceId, character ) =>
    {
      //Displaying change
      if ( FunctionDB.core.findAttributeIndexById( attrId, character ) > -1 )
      {
        if ( showValue )
        {
          FunctionDB.core.StartCoroutine(	
            FunctionDB.core.displayAttributeValue( FunctionDB.core.findCharInstanceGameObjectById( instanceId ), v, attrId, 0, 1.3f ) );
        }

        //Getting attribute
        characterAttribute attribute =
          character.characterAttributes[FunctionDB.core.findAttributeIndexById( attrId, character )];

        //Applying change
        if ( !set ) attribute.curValue = ( attribute.curValue + v ) > 0 ? ( attribute.curValue + v ) : 0;
        else attribute.curValue = v > 0 ? v : 0;
      }
    } );

    BattleManager.setQueueStatus( context,  "changeAttribute", false );
  }

  private void forEachCharacterDo( BattleManager.BattleManagerContext context, bool selfOnly, Action<InstanceID, character> x )
  {
    if ( selfOnly )
    {
      //Active char id
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;
      
      x( context.activeCharacterId, character );
    }
    else
    {

      //For each action target
      foreach ( InstanceID target in context.actionTargets )
      {
        var characterInstance = BattleManager.core.findCharacterInstanceById( target );
        var character = characterInstance.characterCopy;
        x( target, character );
      }
    }
  }

  void generateMana( BattleManager.BattleManagerContext context, int amountToGenerate )
  {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;
    
      var index = FunctionDB.core.findAttributeIndexByName( "MP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attribute.curValue = Mathf.Min(attribute.curValue+amountToGenerate, attribute.maxValue);
      }
      
      FunctionDB.core.StartCoroutine(	
        FunctionDB.core.displayAttributeValue(	
          FunctionDB.core.findCharInstanceGameObjectById( context.activeCharacterId ),	
          amountToGenerate,	
          1,	
          0.7f, 0.7f ) );	
      
    BattleManager.setQueueStatus( context,  "generateMana", false );
}
  void generateManaForEachTargetAlive( BattleManager.BattleManagerContext context, int amountPerTarget )
  {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;

      int amountToGenerate = amountPerTarget * context.actionTargets.Count; 
      var index = FunctionDB.core.findAttributeIndexByName( "MP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attribute.curValue = Mathf.Min(attribute.curValue+amountToGenerate, attribute.maxValue);
      }
      
      FunctionDB.core.StartCoroutine(	
        FunctionDB.core.displayAttributeValue(	
          FunctionDB.core.findCharInstanceGameObjectById( context.activeCharacterId ),	
          amountToGenerate,	
          1,	
          0.7f, 0.7f ) );	
      
    BattleManager.setQueueStatus( context,  "generateManaForEachTargetAlive", false );
}
  
  void generateSuper( BattleManager.BattleManagerContext context, int amountToGenerate )
  {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;
      
      var index = FunctionDB.core.findAttributeIndexByName( "SP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attribute.curValue = Mathf.Min(attribute.curValue+amountToGenerate, attribute.maxValue);
      }
      
      FunctionDB.core.StartCoroutine(	
        FunctionDB.core.displayAttributeValue(	
          FunctionDB.core.findCharInstanceGameObjectById( context.activeCharacterId ),	
          amountToGenerate,	
          1,	
          0.7f, 0.7f ) );	
      
    BattleManager.setQueueStatus( context,  "generateSuper", false );
}
  void generateSuperForEachTargetAlive( BattleManager.BattleManagerContext context, int amountPerTarget )
  {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;
      
      int amountToGenerate = amountPerTarget * context.actionTargets.Count; 
      var index = FunctionDB.core.findAttributeIndexByName( "SP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attribute.curValue = Mathf.Min(attribute.curValue+amountToGenerate, attribute.maxValue);
      }
      
      FunctionDB.core.StartCoroutine(	
        FunctionDB.core.displayAttributeValue(	
          FunctionDB.core.findCharInstanceGameObjectById( context.activeCharacterId ),	
          amountToGenerate,	
          1,	
          0.7f, 0.7f ) );	
      
    BattleManager.setQueueStatus( context,  "generateSuperForEachTargetAlive", false );
}
  
  /*
	Removing a certain quantity of the item from a character.
	Item id is the id of the item to remove.
	Quantity is the quantity of the item to remove.
	Char id is the id of the character from which to remove the item. If Char id is set to -1, the currently active character will be targeted.
	 */
  void removeItem( BattleManager.BattleManagerContext context, int charId, int itemId, float quantity )
  {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var c = characterInstance.characterCopy;
      
      //Getting item
      var itemIndex = FunctionDB.core.findItemIndexById( itemId );

      if ( itemIndex != -1 )
      {
        characterItemInfo i = c.items[FunctionDB.core.findItemIndexById( itemId )];
        i.quantity = i.quantity - quantity >= 0 ? i.quantity - quantity : 0;
      }
      else
      {
        Debug.Log( "Invalid item id" );
      }

      //Regenarating items list
      BattleGen.core.itemGen();
  }
  
  void healTargets( BattleManager.BattleManagerContext context, bool self, int healAmount, bool showValue )
  {
    var sourceInstanceID = context.activeCharacterId;
    
    forEachCharacterDo( context, false, ( instanceID, character ) =>
    {
      var health = FunctionDB.core.findAttributeByName( instanceID, "HP" );
      float before = health.curValue;
      health.curValue = Mathf.Min( health.curValue + healAmount, health.maxValue );
      float heal = health.curValue - before;
      
      if ( showValue && heal > 0f )
      {
          FunctionDB.core.StartCoroutine(	
            FunctionDB.core.displayAttributeValue( FunctionDB.core.findCharInstanceGameObjectById( instanceID ), heal, health.id, 0, 1.3f ) );
      }
    } );

    BattleManager.setQueueStatus( context,  "healTargets", false );
  }

  void damageTargets( BattleManager.BattleManagerContext context, int damageAmount, int school, bool ignoreDefense = false )
  {
    var sourceInstanceID = context.activeCharacterId;
    
    forEachCharacterDo( context, false, ( instanceID, character ) =>
    {
      FinalizeDealDamage( sourceInstanceID, instanceID, damageAmount, school, ignoreDefense );
    } );

    BattleManager.setQueueStatus( context,  "damageTargets", false );
  }

  void damageTargetsTimesPower( BattleManager.BattleManagerContext context, int damageAmount, int school, string multiAttribute, bool ignoreDefense = false )
  {
    var sourceInstanceID = context.activeCharacterId;
    var statValue = FunctionDB.core.findAttributeByName( sourceInstanceID, multiAttribute )?.curValue ?? 1f;
    forEachCharacterDo( context, false, ( instanceID, character ) =>
    {
      FinalizeDealDamage( sourceInstanceID, instanceID, damageAmount*statValue, school, ignoreDefense );
    } );

    BattleManager.setQueueStatus( context,  "damageTargetsTimesPower", false );
  }
  void damageTargetsTimesTargetPower( BattleManager.BattleManagerContext context, int damageAmount, int school, string multiAttribute, bool ignoreDefense = false )
  {
    var sourceInstanceID = context.activeCharacterId;
    forEachCharacterDo( context, false, ( instanceID, character ) =>
    {
      var statValue = FunctionDB.core.findAttributeByName( instanceID, multiAttribute )?.curValue ?? 1f;
      FinalizeDealDamage( sourceInstanceID, instanceID, damageAmount*statValue, school, ignoreDefense );
    } );

    BattleManager.setQueueStatus( context,  "damageTargetsTimesPower", false );
  }

  private void FinalizeDealDamage(InstanceID sourceID, InstanceID victimID, float damageAmount, int school, bool ignoreDefense)
  {
      var victim = BattleManager.core.findCharacterInstanceById( victimID );
      var source = BattleManager.core.findCharacterInstanceById( sourceID );
      var wardrumValue = FunctionDB.core.findAttributeByName( sourceID, "WARDRUM" )?.curValue ?? 0f;
      var doomValue = FunctionDB.core.findAttributeByName( sourceID, "DOOM" )?.curValue ?? 0f;

      int defense = 0;

      var victimCharacter = victim.characterCopy;
      if ( !ignoreDefense )
      {
        var defCountIndex = FunctionDB.core.findAttributeIndexByName( "DEFENDROUNDS", victimCharacter );
        if ( defCountIndex > -1 )
        {
          characterAttribute defendRounds = victimCharacter.characterAttributes[defCountIndex];
          if ( defendRounds.curValue > 0 )
          {
            characterAttribute defendAmount =
              victimCharacter.characterAttributes[
                FunctionDB.core.findAttributeIndexByName( "DEFEND", victimCharacter )];
            defense = Mathf.FloorToInt( defendAmount.curValue );
          }
        }
      }

      float reduction = Mathf.Clamp( defense / 100f, 0f, 1f );
      float damagePostDefense = damageAmount - ( damageAmount * reduction );

      // Apply major status effects
      if ( wardrumValue > 0f ) damagePostDefense *= 2f;
      if ( doomValue > 0f ) damagePostDefense *= 0.5f;
      
      var index = FunctionDB.core.findAttributeIndexByName( "HP", victim.characterCopy );
      if ( index == -1 )
      {
        Debug.LogError( $"Unable to find HP for {victim}" );
      }
      else
      {
        characterAttribute hp = victim.characterCopy.characterAttributes[FunctionDB.core.findAttributeIndexById( index, victim.characterCopy )];
        hp.curValue = Mathf.Clamp( hp.curValue - damagePostDefense, 0f, hp.maxValue );

        //Displaying change
        if ( damagePostDefense == 0f )
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceGameObjectById( victimID ),
              "Blocked!",
              "A9A9A9",
              string.Empty,
              0.7f, 0.7f ) );
        }
        else
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayBattleValue(
              FunctionDB.core.findCharInstanceGameObjectById( victimID ),
              damagePostDefense,
              school,
              0.7f, 0.7f ) );
        }
      }
  }

  void setHPorKill( BattleManager.BattleManagerContext context, bool self, int hpValue, int school )
  {
    var sourceInstanceID = context.activeCharacterId;
    
    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {
      
        characterAttribute hp = FunctionDB.core.findAttributeByName(instanceID, "HP");
        var curValue = hp.curValue;
        if ( hp.curValue > hpValue )
        {
          hp.curValue = hpValue;
        }
        else
        {
          hp.curValue = 0f;
        }
        if ( curValue <= hpValue )
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceGameObjectById( instanceID ),
              "Killed!",
              "A9A9A9",
              string.Empty,
              0.7f, 0.7f ) );
        }
        else
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayBattleValue(
              FunctionDB.core.findCharInstanceGameObjectById( instanceID ),
              curValue - hpValue,
              school,
              0.7f, 0.7f ) );
        }
    } );

    BattleManager.setQueueStatus( context,  "setHPorKill", false );
  }
  
  void kill( BattleManager.BattleManagerContext context, bool self )
  {
    var sourceInstanceID = context.activeCharacterId;
    
    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {
      
        characterAttribute hp = FunctionDB.core.findAttributeByName(instanceID, "HP");
          hp.curValue = 0f;
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceGameObjectById( instanceID ),
              "Killed!",
              "A9A9A9",
              string.Empty,
              0.7f, 0.7f ) );
    } );

    BattleManager.setQueueStatus( context,  "kill", false );
  }
  
  
  void displayFloatText( BattleManager.BattleManagerContext context, bool self, string text, float xAdjust, float yAdjust, bool follow = false,  string color = "A9A9A9", string icon = "" )
  {
    var sourceInstanceID = context.activeCharacterId;
    
    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceGameObjectById( instanceID ),
              text,
              color,
              icon,
              xAdjust, yAdjust, follow ) );
    } );

    BattleManager.setQueueStatus( context,  "displayFloatText", false );
  }



/*
This function will play an animation of a selected character.
Character id is the id of the character on whom to play the animtion. If the id is set to -1, the ative character will be selected.
The condition name is the name of the Animator's parameter which will be set to active once the function is called.
 */
  void playAnimation(BattleManager.BattleManagerContext context, int charId, string conditionName)
  {
    //getting active character if a character id was not provided
    InstanceID charIdNew = charId > -1 ? BattleManager.core.findCharacterInstanceByCharacterDatabaseId(charId) : context.activeCharacterId;
    
    //Playing the animation
    FunctionDB.core.setAnimation(charIdNew, conditionName);

    BattleManager.setQueueStatus( context, "playAnimation", false);
  }


  /*
	Playing an audio once.
	The audio source index is the index of the audio source attached to the BattleManager object.
	Audio id is the id of the audioclip in the database.
	 */
  void playAudioOnce(BattleManager.BattleManagerContext context, int audioSourceIndex, int audioId)
  {

    //Calling function to play audio once
    FunctionDB.core.playAudioOnce(audioSourceIndex, audioId);

    BattleManager.setQueueStatus( context, "playAudioOnce", false);
  }


  /*
	This function rotates a character.
	Char id is the id of the character to be rotated. If the id is set to -1, the active character will be selected.
	The degree is the number of degrees to rotate the character. Use 180 each time you wish to flip the battler.
	 */
  void rotateCharacter(BattleManager.BattleManagerContext context, int charId, float degree)
  {
    
    //getting active character if a character id was not provided
    InstanceID charIdNew = charId > -1 ? BattleManager.core.findCharacterInstanceByCharacterDatabaseId(charId) : context.activeCharacterId;
    
    var charObject = FunctionDB.core.findCharInstanceGameObjectById(charIdNew);
    charObject.transform.Rotate(0, degree, 0);

    BattleManager.setQueueStatus( context, "rotateCharacter", false);
  }

  /*
	This function toggles elements of the battle ui action window.
	If state is set to true, the actions will be accessible. If false, the actions will not be interactible.
	 */
  public void toggleActions(BattleManager.BattleManagerContext context, bool state)
  {

    //Getting actions parent object
    GameObject parentObject = ObjectDB.core.battleUIActionsWindow;

    //Toggle elements
    foreach (Transform t in parentObject.transform)
    {
      //Getting button element
      Button b = t.gameObject.GetComponent<Button>();
      //Setting state
      b.interactable = state;
    }

    BattleManager.setQueueStatus( context, "toggleActions", false);

  }

  public void Bark(BattleManager.BattleManagerContext context, string Knot)
  {
      var charIdNew = context.activeCharacterId;
      var charObject = FunctionDB.core.findCharInstanceGameObjectById(charIdNew);
      var character = BattleManager.core.findCharacterInstanceById( charIdNew ).characterCopy;
      BattleGen.story.Bark(Knot, charIdNew.CharacterID.ToString());
        
      BattleManager.setQueueStatus( context, "Bark", false);
  }
  
  public void BarkVictim(BattleManager.BattleManagerContext context, string Knot)
  {
    if ( context.actionTargets.Count > 0 )
    {
      var charIdNew = context.actionTargets[0];
      var charObject = FunctionDB.core.findCharInstanceGameObjectById(charIdNew);
      var character = BattleManager.core.findCharacterInstanceById( charIdNew ).characterCopy;
      BattleGen.story.Bark( Knot, charIdNew.CharacterID.ToString() );
    }

    BattleManager.setQueueStatus( context, "BarkVictim", false);
  }

  /*
	Display FX on self or target list
	If self is true, the FX will be applied on the active character, otherwise it will be applied on all the targets.
	Time to exist is the time for the FX to exist on the scene.
	fxParameterName is the name of the parameter which is supposed to trigger the FX animation in the animator.
	The adjustments are used to modify the position of the spawned prefab by a certain value on spawn/follow.
	 */

  public void displayFX(BattleManager.BattleManagerContext context, bool self, float timeToExist, string fxName, float xAdjustment, float yAdjustment)
  {
    //Getting FXObject
    GameObject FXObject = ObjectDB.core.FXObject;
    GameObject FXPrefab = Database.staticDB.FindFXByName( fxName );

    if ( FXPrefab != null )
    {
      forEachCharacterDo( context, self, ( instanceID, character ) => {
        
        //Getting character instace
        GameObject charInstance = FunctionDB.core.findCharInstanceGameObjectById( instanceID );

        //Getting coordinates
        Vector3 coordinates = charInstance.transform.position;

        //Modifying coordinates
        Vector3 newCoordinates = new Vector3( coordinates.x + xAdjustment, coordinates.y + yAdjustment, coordinates.z );

        //Spawning FX
        GameObject g = Instantiate( FXPrefab, newCoordinates, Quaternion.identity, FXObject.transform );

        //Playing animation
        //g.GetComponent<Animator>().SetBool(fxName, true);

        //Making FX follow target
        FunctionDB.core.StartCoroutine( FunctionDB.core.follow( g, charInstance, xAdjustment, yAdjustment ) );

        //Destrying object after specified amount of time
        FunctionDB.core.StartCoroutine( FunctionDB.core.destroyAfterTime( g, timeToExist ) );

      } );
    }

    BattleManager.setQueueStatus( context, "displayFX", false);
  }

  void cleanseEffects( BattleManager.BattleManagerContext context, bool self, bool hostileEffects,
    bool friendlyEffects, int superPerEffect )
  {
    List<string> friendly = new List<string> (){ "DEFENDROUNDS", "WARDRUM", "TAUNT" };
    List<string> hostile = new List<string> (){ "POISON", "STUN", "PARALYZE", "DOOM", "DOOMSOURCE" };

    int purged = 0;
    
    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {
      if ( hostileEffects )
      {
        foreach ( var h in hostile )
        {
          var attr = FunctionDB.core.findAttributeByName(  instanceID, h );
          if ( attr != null )
          {
            if ( attr.curValue > 0 ) purged += Mathf.FloorToInt( attr.curValue );
            attr.curValue = 0;
          }
        }
      }
      if ( friendlyEffects )
      {
        foreach ( var h in friendly )
        {
          var attr = FunctionDB.core.findAttributeByName(  instanceID, h );
          if ( attr != null )
          {
            if ( attr.curValue > 0 ) purged += Mathf.FloorToInt( attr.curValue );
            attr.curValue = 0;
          }
        }
      }
    } );

    
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
      var character = characterInstance.characterCopy;

      int amountToGenerate = superPerEffect * purged; 
      var index = FunctionDB.core.findAttributeIndexByName( "SP", character );
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute = character.characterAttributes[index];
        attribute.curValue = Mathf.Min(attribute.curValue+amountToGenerate, attribute.maxValue);
      }
      
      FunctionDB.core.StartCoroutine(	
        FunctionDB.core.displayAttributeValue(	
          FunctionDB.core.findCharInstanceGameObjectById( context.activeCharacterId ),	
          amountToGenerate,	
          1,	
          0.7f, 0.7f ) );	
    
      BattleManager.setQueueStatus( context, "cleanseEffects", false);
  }

  /*
	Checking active player attribute.
	By default, if min value not met, stopping skill chain.
	If self is true, the attribute of the currently active character will be checked. Otherwise the attributes of all selected characters will be checked.
	Attribute id is the id of the attribute to check (of the latter character).
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will be jump.
	 */
  void checkAttribute( BattleManager.BattleManagerContext context, bool self, int attrId, float minValue, string jumpTo )
  {
    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {

      //Getting attribute
      characterAttribute attribute = character.characterAttributes[FunctionDB.core.findAttributeIndexById( attrId, character )];

      //Checking value
      if ( attribute.curValue < minValue )
      {
        //Stopping skill chain and displaying warning
        int jumpIndex = FunctionDB.core.findFunctionQueueJumpIndexByName( context, jumpTo );
        if ( jumpIndex == -1 )
        {
          Debug.LogWarning( $"Unable to find jump index {jumpTo} in skill {context.activeSkillId}" );
        }
        else
        {
          if ( context.DEBUG ) Debug.Log( $"Jumping to {jumpIndex} {jumpTo}." );
          context.runningFunctionIndex = jumpIndex;
        }
        BattleManager.core.startWarningRoutine( "Not enough " + attribute.name, 2f );
      }

    } );

    BattleManager.setQueueStatus( context,  "checkAttribute", false );
  }

  /*
	Checking active player attribute.
	By default, if min value not met, stopping skill chain.
	If self is true, the attribute of the currently active character will be checked. Otherwise the attributes of all selected characters will be checked.
	Attribute id is the id of the attribute to check (of the latter character).
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will jump.
	 */
  void testAttributeJump( BattleManager.BattleManagerContext context, bool self, string attrName, float minValue, bool warn, string jumpTo )
  {

    forEachCharacterDo( context, self, ( instanceID, character ) =>
    {
      bool pass = true;
      var index = FunctionDB.core.findAttributeIndexByName( attrName, character );
      
      //Getting attribute
      if ( index >= 0 )
      {
        characterAttribute attribute =
          character.characterAttributes[index];

        //Checking value
        if ( attribute.curValue < minValue )
        {
          pass = false;
        }
      }
      else
      {
        pass = false;
      }

      if ( pass )
      {
        //Stopping skill chain and displaying warning
        int jumpIndex = FunctionDB.core.findFunctionQueueJumpIndexByName( context, jumpTo );
        if ( jumpIndex == -1 )
        {
          Debug.LogWarning( $"Unable to find jump index {jumpTo} in skill {context.activeSkillId}" );
        }
        else
        {
          if(context.DEBUG) Debug.Log( $"TestJump to {jumpTo}({jumpIndex}) in skill {context.activeSkillId}." );
          context.runningFunctionIndex = jumpIndex;
        }

        if ( warn )
        {
          BattleManager.core.startWarningRoutine( "Not enough " + attrName, 2f );
        }
      }

    } );

    BattleManager.setQueueStatus( context,  "testAttributeJump", false );
  }
  
  /*
   * Jump to the command labelled
   * :jumpTo
	 */
  void jump( BattleManager.BattleManagerContext context,  string jumpTo )
  {
    //Stopping skill chain and displaying warning
    int jumpIndex = FunctionDB.core.findFunctionQueueJumpIndexByName( context, jumpTo );
    if ( jumpIndex == -1 )
    {
      Debug.LogWarning( $"Unable to find jump index {jumpTo} in skill {context.activeSkillId}" );
    }
    else
    {
      if(context.DEBUG)Debug.Log( $"Jump to {jumpTo} ({jumpIndex}) in skill {context.activeSkillId}." );
      context.runningFunctionIndex = jumpIndex;
    }
    
    BattleManager.setQueueStatus( context,  "jump", false );
  }

  void jumpIfTargetCountTest( BattleManager.BattleManagerContext context, string test, string jumpTo )
  {
    GetOperationValue( test, out var op, out float v );
    bool pass = operationResult( context.actionTargets.Count, op, v );

    if ( pass )
    {
      int jumpIndex = FunctionDB.core.findFunctionQueueJumpIndexByName( context, jumpTo );
      if ( jumpIndex == -1 )
      {
        Debug.LogWarning( $"Unable to find jump index {jumpTo} in skill {context.activeSkillId}" );
      }
      else
      {
        if(context.DEBUG)Debug.Log( $"Jump to {jumpTo} ({jumpIndex}) in skill {context.activeSkillId}." );
        context.runningFunctionIndex = jumpIndex;
      }
    }

    BattleManager.setQueueStatus( context,  "jumpIfTargetCountTest", false );
  }


  void Awake() { if (core == null) core = this; }
}


//(c) Cination - Tsenkilidis Alexandros