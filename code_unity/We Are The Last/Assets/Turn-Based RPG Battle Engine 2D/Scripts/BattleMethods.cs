using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;
using ClassDB;

//A class containing custom methods
public class BattleMethods : MonoBehaviour
{
  public static BattleMethods core;

  //This is a sample AI function that simply runs the first skill available to the AI on a random target
  void sampleAi(BattleManager.BattleManagerContext context)
  {

    //Getting character
    var character =
      Database.dynamic.characters[
        FunctionDB.core.findCharacterIndexById( context.activeCharacterId )];
    //Getting skills
    var skillIds = character.skills;

    foreach ( int skillId in skillIds )
    {

      var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById( skillId )];
      //Getting functions to call
      var functionsToCall = skill.functionsToCall;

      //Storing the functions list to functionQueue
      context.activeSkillId = skillId;
      context.functionQueue = functionsToCall;
      BattleManager.core.StartCoroutine( BattleManager.functionQueueCaller( context ) );

      break;
    }

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

      if ( actionType == 0 )
      {
        requiredTp = Database.dynamic.skills[context.activeSkillId].turnPointCost;
      }
      else if ( actionType == 1 )
      {
        requiredTp = Database.dynamic.items[FunctionDB.core.findItemIndexById( actionId )].turnPointCost;
      }
      else
      {
        Debug.Log( "Invalid action type. Set 0 for skill or 1 for item." );
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

  void autoSelectTargets( BattleManager.BattleManagerContext context, int targetLimit, bool allowFriendly, bool allowHostile )
  {
    context.targetLimit = targetLimit;
    context.actionTargets = BattleGen.core.getValidTargets( context, allowFriendly, allowHostile, targetLimit );
    BattleManager.setQueueStatus( context,  "autoSelectTargets", false );
  }

  /*
	Target selection used by skills and items.
	If targetSameTeam is true, a list of allies will be displayed to the player to choose from.
	Otherwise the enemy team members will be displayed.
	Only active character will be displayed.
	TargetLimit is the maximum amount of targets the player can select. All targets are fed into the action targets list of BattleManager.
	 */

  void selectCharacter( BattleManager.BattleManagerContext context, bool targetSameTeam, int targetLimit )
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

      //Getting target list
      int targetInt = targetSameTeam ? 0 : 1;
      List<int> targets = BattleManager.core.activeTeam == targetInt
        ? context.attackerTeam
        : context.defenderTeam;

      //Excluding invalid characters
      for ( int i = 0; i < targets.Count; i++ )
      {
        //Id
        var charId = targets[i];
        //Getting character's health
        character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( charId )];

        //If health is 0, exclude character
        if ( !character.isActive )
        {
          targets.RemoveAt( i );
        }
      }

      //Getting random character
      int randIndex = AIGetRandomTarget( targets );

      //Adding character to targets
      context.actionTargets.Add( randIndex );
    }

    BattleManager.setQueueStatus( context,  "selectCharacter", false );

  }

  private int AIGetRandomTarget( List<int> targets )
  {
    List<int> tauntTargets = new List<int>();
    for ( int i = 0; i < targets.Count; ++i )
    {
      //Id
      var charId = targets[i];
      //Getting character's health
      character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( charId )];
      if ( character.isTaunting )
      {
        tauntTargets.Add( charId );
      }
    }

    // Prefer taunted
    if ( tauntTargets.Count > 0 )
    {
      var charId = tauntTargets[UnityEngine.Random.Range( 0, tauntTargets.Count )];
      character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( charId )];
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
    foreach ( int target in context.actionTargets )
    {

      //Getting character
      int victimId = FunctionDB.core.findCharacterIndexById( target );
      character character = Database.dynamic.characters[victimId];

      if ( !character.isActive ) continue;

      int blockIndex = FunctionDB.core.findAttributeIndexByName( "COUNTER", character );
      if ( blockIndex < 0 ) continue;

      //Getting attribute
      characterAttribute attribute = character.characterAttributes[blockIndex];

      //Checking value
      if ( attribute.curValue > 0 )
      {
        int counterActorID = victimId;
        character counteringCharacter = character;
        int counterSourceIndex = FunctionDB.core.findAttributeIndexByName( "COUNTERSOURCE", character );
        if ( counterSourceIndex >= 0 )
        {
          //Getting attribute
          characterAttribute counterSource = character.characterAttributes[counterSourceIndex];
          counterActorID = Mathf.FloorToInt( counterSource.curValue );
          counteringCharacter = Database.dynamic.characters[counterActorID];
        }
        
        var c = new BattleManager.BattleManagerContext();
        c.actionTargets = new List<int>() { context.activeCharacterId };
        c.Init( counterActorID, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
        c.targetLimit = 1;
        c.functionQueue = counteringCharacter.getCounter();
        reactionSet.Add( c );
      }
    }

    //Marking function as not running.
    context = BattleManager.core.CurrentContext;
    BattleManager.setQueueStatus( context,  "provokeReaction", false );

    if ( reactionSet.Count > 0 )
    {
      BattleManager.core.ReactionContext = reactionSet[0];
    }

    StartCoroutine( BattleManager.reactionQueueCaller( reactionSet.ToArray() ) );

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
    var destinationCharId = -1;

    if ( context.actionTargets.Count > 0 )
      destinationCharId = context.actionTargets[0];
    else Debug.Log( "No targets selected" );

    if ( destinationCharId != -1 )
    {

      //Getting source char gameObject
      GameObject sourceCharObject = FunctionDB.core.findCharInstanceById( sourceCharId );
      //Getting dest char gameObject
      GameObject destinationCharObject = FunctionDB.core.findCharInstanceById( destinationCharId );

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

  //Moving battler pack to spawn point
  IEnumerator moveBack( object[] parms )
  {
    BattleManager.BattleManagerContext context = parms[0] as BattleManager.BattleManagerContext; 

    //Movement speed
    float speed = (float) parms[1];

    //Getting active char id
    int charId = context.activeCharacterId;

    //Getting character object
    GameObject charObject = FunctionDB.core.findCharInstanceById( charId );
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

  /*
	Checking active player attribute.
	By default, if min value not met, stopping skill chain.
	If self is true, the attribute of the currently active character will be checked. Otherwise the attributes of all selected characters will be checked.
	Attribute id is the id of the attribute to check (of the latter character).
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will be stopped.
	 */
  void checkAttribute( BattleManager.BattleManagerContext context, bool self, int attrId, float minValue )
  {
    forEachCharacterDo( context, self, ( character ) =>
    {

      //Getting attribute
      characterAttribute attribute =
        character.characterAttributes[FunctionDB.core.findAttributeIndexById( attrId, character )];

      //Checking value
      if ( attribute.curValue < minValue )
      {
        //Stopping skill chain and displaying warning
        context.functionQueue.Clear();
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
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will be stopped.
	 */
  void checkAttributeByName( BattleManager.BattleManagerContext context, bool self, string attrName, float minValue, bool warn )
  {

    forEachCharacterDo( context, self, ( character ) =>
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

      if ( !pass )
      {
        //Stopping skill chain and displaying warning
        context.functionQueue.Clear();
        if ( warn )
        {
          BattleManager.core.startWarningRoutine( "Not enough " + attrName, 2f );
        }
      }

    } );

    BattleManager.setQueueStatus( context,  "checkAttribute", false );
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
      (character) => {

        if ( showText )
        {
          //Displaying change
          FunctionDB.core.StartCoroutine( FunctionDB.core.displayValue(
            FunctionDB.core.findCharInstanceById( character.id ), v,
            0, 1.3f ) );
        }

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
      } );

    BattleManager.setQueueStatus( context,  "addOrChangeAttribute", false );
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

    forEachCharacterDo( context, self, ( character ) =>
    {
      //Displaying change
      if ( FunctionDB.core.findAttributeIndexById( attrId, character ) > -1 )
      {
        if ( showValue )
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue( FunctionDB.core.findCharInstanceById( character.id ), v, 0, 1.3f ) );
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

  private void forEachCharacterDo( BattleManager.BattleManagerContext context, bool selfOnly, Action<character> x )
  {
    if ( selfOnly )
    {
      //Active char id
      var activeCharId = context.activeCharacterId;

      //Getting character
      character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( activeCharId )];

      x( character );
    }
    else
    {

      //For each action target
      foreach ( int target in context.actionTargets )
      {
        //Getting character
        character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( target )];
        x( character );
      }
    }
  }

  /*
	Removing a certain quantity of the item from a character.
	Item id is the id of the item to remove.
	Quantity is the quantity of the item to remove.
	Char id is the id of the character from which to remove the item. If Char id is set to -1, the currently active character will be targeted.
	 */
  void removeItem( BattleManager.BattleManagerContext context, int charId, int itemId, float quantity )
  {

    //Character id
    var characterId = charId > -1 ? charId : context.activeCharacterId;
    //Getting character
    character c = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( characterId )];
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

  void damageTargets( BattleManager.BattleManagerContext context, int damageAmount, int school )
  {
    forEachCharacterDo( context, false, ( character ) =>
    {
      var index = FunctionDB.core.findAttributeIndexByName( "HP", character );
      int defense = 0;
      if ( index == -1 )
      {
        Debug.LogError( $"Unable to find HP for {character}" );
      }
      else
      {
        characterAttribute hp =
          character.characterAttributes[FunctionDB.core.findAttributeIndexById( index, character )];

        var defCountIndex = FunctionDB.core.findAttributeIndexByName( "DEFENDROUNDS", character );
        if ( defCountIndex > -1 )
        {
          characterAttribute defendRounds = character.characterAttributes[defCountIndex];
          if ( defendRounds.curValue > 0 )
          {
            characterAttribute defendAmount =
              character.characterAttributes[FunctionDB.core.findAttributeIndexByName( "DEFEND", character )];
            defense = Mathf.FloorToInt( defendAmount.curValue );
          }
        }

        float reduction = Mathf.Clamp( defense / 100f, 0f, 1f );
        float damagePostDefense = damageAmount - ( damageAmount * reduction );
        hp.curValue = Mathf.Clamp( hp.curValue - damagePostDefense, 0f, hp.maxValue );

        //Displaying change
        if ( damagePostDefense == 0f )
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceById( character.id ),
              "Blocked!",
              0, 0.3f ) );
        }
        else
        {
          FunctionDB.core.StartCoroutine(
            FunctionDB.core.displayValue(
              FunctionDB.core.findCharInstanceById( character.id ),
              damagePostDefense,
              0, 0.3f ) );
        }
      }
    } );

    BattleManager.setQueueStatus( context,  "damageTargets", false );
  }

/*
This function will play an animation of a selected character.
Character id is the id of the character on whom to play the animtion. If the id is set to -1, the ative character will be selected.
The condition name is the name of the Animator's parameter which will be set to active once the function is called.
 */
  void playAnimation(BattleManager.BattleManagerContext context, int charId, string conditionName)
  {

    //getting active character if a character id was not provided
    var charIdNew = charId > -1 ? charId : context.activeCharacterId;
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
    ;
    var charIdNew = charId > -1 ? charId : context.activeCharacterId;
    var charObject = FunctionDB.core.findCharInstanceById(charIdNew);
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
        var charObject = FunctionDB.core.findCharInstanceById(charIdNew);
        BattleGen.story.Bark(Knot, charObject.name);
    }

  /*
	Display FX on self or target list
	If self is true, the FX will be applied on the active character, otherwise it will be applied on all the targets.
	Time to exist is the time for the FX to exist on the scene.
	fxParameterName is the name of the parameter which is supposed to trigger the FX animation in the animator.
	The adjustments are used to modify the position of the spawned prefab by a certain value on spawn/follow.
	 */

  public void displayFX(BattleManager.BattleManagerContext context, bool self, float timeToExist, string fxParameterName, float xAdjustment, float yAdjustment)
  {

    //Getting prefab
    GameObject FXPrefab = ObjectDB.core.FXPrefab;

    //Getting FXObject
    GameObject FXObject = ObjectDB.core.FXObject;

    if (self)
    {

      //Active char id
      var activeCharId = context.activeCharacterId;

      //Getting character instace
      GameObject charInstance = FunctionDB.core.findCharInstanceById(activeCharId);

      //Getting coordinates
      Vector3 coordinates = charInstance.transform.position;

      //Modifying coordinates
      Vector3 newCoordinates = new Vector3(coordinates.x + xAdjustment, coordinates.y + yAdjustment, coordinates.z);

      //Spawning FX
      GameObject g = Instantiate(FXPrefab, newCoordinates, Quaternion.identity, FXObject.transform);

      //Playing animation
      g.GetComponent<Animator>().SetBool(fxParameterName, true);

      //Making FX follow target
      FunctionDB.core.StartCoroutine(FunctionDB.core.follow(g, charInstance, xAdjustment, yAdjustment));

      //Destrying object after specified amount of time
      FunctionDB.core.StartCoroutine(FunctionDB.core.destroyAfterTime(g, timeToExist));


    }
    else
    {

      //For each action target
      foreach (int target in context.actionTargets)
      {

        //Getting character instace
        GameObject charInstance = FunctionDB.core.findCharInstanceById(target);

        //Getting coordinates
        Vector3 coordinates = charInstance.transform.position;

        //Modifying coordinates
        Vector3 newCoordinates = new Vector3(coordinates.x + xAdjustment, coordinates.y + yAdjustment, coordinates.z);

        //Spawning FX
        GameObject g = Instantiate(FXPrefab, newCoordinates, Quaternion.identity, FXObject.transform);

        //Playing animation
        g.GetComponent<Animator>().SetBool(fxParameterName, true);

        //Making FX follow target
        FunctionDB.core.StartCoroutine(FunctionDB.core.follow(g, charInstance, xAdjustment, yAdjustment));

        //Destrying object after specified amount of time
        FunctionDB.core.StartCoroutine(FunctionDB.core.destroyAfterTime(g, timeToExist));

      }
    }

    BattleManager.setQueueStatus( context, "displayFX", false);
  }

  void Awake() { if (core == null) core = this; }
}


//(c) Cination - Tsenkilidis Alexandros