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
  void sampleAi()
  {

    //Getting character
    var character =
      Database.dynamic.characters[
        FunctionDB.core.findCharacterIndexById( BattleManager.core.CurrentContext.activeCharacterId )];
    //Getting skills
    var skillIds = character.skills;

    foreach ( int skillId in skillIds )
    {

      var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById( skillId )];
      //Getting functions to call
      var functionsToCall = skill.functionsToCall;

      //Storing the functions list to functionQueue
      BattleManager.core.CurrentContext.functionQueue = functionsToCall;

      BattleManager.core.StartCoroutine( BattleManager.functionQueueCaller( BattleManager.core.CurrentContext ) );

      break;
    }

  }

  /*
	This function is used to subtract turn points.
	Action Type should be 0 if the action is a skill, or 1 if it is an item.
	Action Id is the id of the action (skill or item) in the database.
	If actionId is -1 or action type is -1, the function will set remaining points to 0.
	 */
  public void subtractTurnPoints( int actionType, int actionId )
  {

    if ( actionId != -1 && actionType != -1 )
    {

      int tp = BattleManager.core.turnPoints;
      int requiredTp = 0;

      if ( actionType == 0 )
      {
        requiredTp = Database.dynamic.skills[FunctionDB.core.findSkillIndexById( actionId )].turnPointCost;
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



    BattleManager.setQueueStatus( "subtractTurnPoints", false );

  }


  /*
	Target selection used by skills and items.
	If targetSameTeam is true, a list of allies will be displayed to the player to choose from.
	Otherwise the enemy team members will be displayed.
	Only active character will be displayed.
	TargetLimit is the maximum amount of targets the player can select. All targets are fed into the action targets list of BattleManager.
	 */

  void selectCharacter( bool targetSameTeam, int targetLimit )
  {

    BattleManager.core.RunningContext.targetLimit = targetLimit;
    BattleManager.core.RunningContext.actionTargets.Clear();

    if ( !( BattleManager.core.autoBattle || BattleManager.core.activeTeam == 1 ) )
    {

      if ( targetSameTeam ) BattleGen.core.targetGen( BattleManager.core.RunningContext, true, false, targetLimit );
      else BattleGen.core.targetGen( BattleManager.core.RunningContext, false, true, targetLimit );

    }
    else
    {

      //Getting target list
      int targetInt = targetSameTeam ? 0 : 1;
      List<int> targets = BattleManager.core.activeTeam == targetInt
        ? BattleManager.core.RunningContext.attackerTeam
        : BattleManager.core.RunningContext.defenderTeam;

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
      BattleManager.core.RunningContext.actionTargets.Add( randIndex );
    }

    BattleManager.setQueueStatus( "selectCharacter", false );

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

    //Terminate current function chain if no characters were selected ?
    bool terminateOnEmpty = (bool) parms[0];
    //Has the target limit been reached
    bool limitReached = false;

    while ( true )
    {
      //Is the amount of tagrets currently selected equal to the target limit?
      limitReached = BattleManager.core.RunningContext.targetLimit ==
                     BattleManager.core.RunningContext.actionTargets.Count;

      //If the previous is true, the function is told to terminate if the player has not selected anything, and the player has indeed selected no targets -
      if ( limitReached && terminateOnEmpty && BattleManager.core.RunningContext.actionTargets.Count == 0 )
      {
        //Stop the execution of the function chain by erasing it.
        BattleManager.core.RunningContext.functionQueue.Clear();
        break;
      }
      else if ( limitReached )
      {
        //Otheriwse, if the limit has been simply reached, mark this function as not running.
        BattleManager.setQueueStatus( "waitForSelection", false );
        break;
      }

      yield return new WaitForEndOfFrame();

    }

  }

  //Waiting for a reaction Queue to end
  IEnumerator waitForReaction( object[] parms )
  {
    while ( BattleManager.core.ReactionContext != null && BattleManager.core.ReactionContext.functionQueue.Count > 0 )
    {
      yield return new WaitForEndOfFrame();
    }

    //Marking function as not running.
    BattleManager.setQueueStatus( "waitForReaction", false );
  }


  //Waiting for a fixed amount of seconds
  IEnumerator wait( object[] parms )
  {
    //Time to wait in seconds.
    float timeToWait = (float) parms[0];
    //Waiting
    yield return new WaitForSeconds( timeToWait );
    //Marking function as not running.
    BattleManager.setQueueStatus( "wait", false );
  }


  // Provoke reaction from enemy if it can counter
  IEnumerator provokeReaction( object[] parms )
  {
    //For each action target
    foreach ( int target in BattleManager.core.RunningContext.actionTargets )
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
        BattleManager.core.ReactionContext = new BattleManager.BattleManagerContext();
        BattleManager.core.ReactionContext.actionTargets = new List<int>()
          { BattleManager.core.RunningContext.activeCharacterId };
        BattleManager.core.ReactionContext.Init( victimId, BattleManager.core.activePlayerTeam,
          BattleManager.core.activeEnemyTeam );
        BattleManager.core.ReactionContext.targetLimit = 1;
        BattleManager.core.ReactionContext.functionQueue = character.getCounter();
        StartCoroutine( BattleManager.reactionQueueCaller( BattleManager.core.ReactionContext ) );
      }
    }

    //Marking function as not running.
    BattleManager.core.RunningContext = BattleManager.core.CurrentContext;
    BattleManager.setQueueStatus( "provokeReaction", false );

    //Waiting
    yield return new WaitForEndOfFrame();
  }

  /*
	Moving currently active character to first selected target
	 */
  IEnumerator moveToTarget( object[] parms )
  {

    //The distance to maintain between the character and the target.
    float distance = (float) parms[0];

    //Movements speed.
    float speed = (float) parms[1];

    //Active character id and destination character id
    var sourceCharId = BattleManager.core.RunningContext.activeCharacterId;
    var destinationCharId = -1;

    if ( BattleManager.core.RunningContext.actionTargets.Count > 0 )
      destinationCharId = BattleManager.core.RunningContext.actionTargets[0];
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

    BattleManager.setQueueStatus( "moveToTarget", false );
  }

  //Moving battler pack to spawn point
  IEnumerator moveBack( object[] parms )
  {

    //Movement speed
    float speed = (float) parms[0];

    //Getting active char id
    int charId = BattleManager.core.RunningContext.activeCharacterId;

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

    BattleManager.setQueueStatus( "moveBack", false );

  }

  /*
	Checking active player attribute.
	By default, if min value not met, stopping skill chain.
	If self is true, the attribute of the currently active character will be checked. Otherwise the attributes of all selected characters will be checked.
	Attribute id is the id of the attribute to check (of the latter character).
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will be stopped.
	 */
  void checkAttribute( bool self, int attrId, float minValue )
  {
    forEachCharacterDo( self, ( character ) =>
    {

      //Getting attribute
      characterAttribute attribute =
        character.characterAttributes[FunctionDB.core.findAttributeIndexById( attrId, character )];

      //Checking value
      if ( attribute.curValue < minValue )
      {
        //Stopping skill chain and displaying warning
        BattleManager.core.RunningContext.functionQueue.Clear();
        BattleManager.core.startWarningRoutine( "Not enough " + attribute.name, 2f );
      }

    } );

    BattleManager.setQueueStatus( "checkAttribute", false );
  }

  /*
	Checking active player attribute.
	By default, if min value not met, stopping skill chain.
	If self is true, the attribute of the currently active character will be checked. Otherwise the attributes of all selected characters will be checked.
	Attribute id is the id of the attribute to check (of the latter character).
	Min Value is the minimum value that the attribute can have. If the value of the attribute is below the minimum value, the skill chain will be stopped.
	 */
  void checkAttributeByName( bool self, string attrName, float minValue, bool warn )
  {

    forEachCharacterDo( self, ( character ) =>
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
        BattleManager.core.RunningContext.functionQueue.Clear();
        if ( warn )
        {
          BattleManager.core.startWarningRoutine( "Not enough " + attrName, 2f );
        }
      }

    } );

    BattleManager.setQueueStatus( "checkAttribute", false );
  }

  void addOrChangeAttribute( bool self, string attributeName, string value, float maxValue )
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

    forEachCharacterDo( self,
      (character) => { 
        
        //Displaying change
        FunctionDB.core.StartCoroutine( FunctionDB.core.displayValue( FunctionDB.core.findCharInstanceById( character.id ), v,
          0, 0.3f ) );

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
          characterAttribute attribute =
            character.characterAttributes[FunctionDB.core.findAttributeIndexById( index, character )];

          //Applying change
          if ( !set ) attribute.curValue = ( attribute.curValue + v ) > 0 ? ( attribute.curValue + v ) : 0;
          else attribute.curValue = v > 0 ? v : 0;
        }
      } );

    BattleManager.setQueueStatus( "addOrChangeAttribute", false );
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
  void changeAttribute( bool self, int attrId, string value )
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

    forEachCharacterDo( self, ( character ) =>
    {
      //Displaying change
      if ( FunctionDB.core.findAttributeIndexById( attrId, character ) > -1 )
      {
        FunctionDB.core.StartCoroutine(
          FunctionDB.core.displayValue( FunctionDB.core.findCharInstanceById( character.id ), v, 0, 0.3f ) );
        //Getting attribute
        characterAttribute attribute =
          character.characterAttributes[FunctionDB.core.findAttributeIndexById( attrId, character )];

        //Applying change
        if ( !set ) attribute.curValue = ( attribute.curValue + v ) > 0 ? ( attribute.curValue + v ) : 0;
        else attribute.curValue = v > 0 ? v : 0;
      }
    } );

    BattleManager.setQueueStatus( "changeAttribute", false );
  }

  private void forEachCharacterDo( bool selfOnly, Action<character> x )
  {

    if ( selfOnly )
    {
      //Active char id
      var activeCharId = BattleManager.core.RunningContext.activeCharacterId;

      //Getting character
      character character = Database.dynamic.characters[FunctionDB.core.findCharacterIndexById( activeCharId )];

      x( character );
    }
    else
    {

      //For each action target
      foreach ( int target in BattleManager.core.RunningContext.actionTargets )
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
  void removeItem( int charId, int itemId, float quantity )
  {

    //Character id
    var characterId = charId > -1 ? charId : BattleManager.core.RunningContext.activeCharacterId;
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

  void damageTargets( int damageAmount, int school )
  {
    forEachCharacterDo( false, ( character ) =>
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
        hp.curValue = Mathf.Clamp( hp.curValue + damagePostDefense, 0f, hp.maxValue );

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

    BattleManager.setQueueStatus( "damageTargets", false );
  }

/*
This function will play an animation of a selected character.
Character id is the id of the character on whom to play the animtion. If the id is set to -1, the ative character will be selected.
The condition name is the name of the Animator's parameter which will be set to active once the function is called.
 */
  void playAnimation(int charId, string conditionName)
  {

    //getting active character if a character id was not provided
    var charIdNew = charId > -1 ? charId : BattleManager.core.RunningContext.activeCharacterId;
    //Playing the animation
    FunctionDB.core.setAnimation(charIdNew, conditionName);

    BattleManager.setQueueStatus("playAnimation", false);
  }


  /*
	Playing an audio once.
	The audio source index is the index of the audio source attached to the BattleManager object.
	Audio id is the id of the audioclip in the database.
	 */
  void playAudioOnce(int audioSourceIndex, int audioId)
  {

    //Calling function to play audio once
    FunctionDB.core.playAudioOnce(audioSourceIndex, audioId);

    BattleManager.setQueueStatus("playAudioOnce", false);
  }


  /*
	This function rotates a character.
	Char id is the id of the character to be rotated. If the id is set to -1, the active character will be selected.
	The degree is the number of degrees to rotate the character. Use 180 each time you wish to flip the battler.
	 */
  void rotateCharacter(int charId, float degree)
  {
    ;
    var charIdNew = charId > -1 ? charId : BattleManager.core.RunningContext.activeCharacterId;
    var charObject = FunctionDB.core.findCharInstanceById(charIdNew);
    charObject.transform.Rotate(0, degree, 0);

    BattleManager.setQueueStatus("rotateCharacter", false);
  }

  /*
	This function toggles elements of the battle ui action window.
	If state is set to true, the actions will be accessible. If false, the actions will not be interactible.
	 */
  public void toggleActions(bool state)
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

    BattleManager.setQueueStatus("toggleActions", false);

  }

  /*
	Display FX on self or target list
	If self is true, the FX will be applied on the active character, otherwise it will be applied on all the targets.
	Time to exist is the time for the FX to exist on the scene.
	fxParameterName is the name of the parameter which is supposed to trigger the FX animation in the animator.
	The adjustments are used to modify the position of the spawned prefab by a certain value on spawn/follow.
	 */

  public void displayFX(bool self, float timeToExist, string fxParameterName, float xAdjustment, float yAdjustment)
  {

    //Getting prefab
    GameObject FXPrefab = ObjectDB.core.FXPrefab;

    //Getting FXObject
    GameObject FXObject = ObjectDB.core.FXObject;

    if (self)
    {

      //Active char id
      var activeCharId = BattleManager.core.RunningContext.activeCharacterId;

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
      foreach (int target in BattleManager.core.RunningContext.actionTargets)
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

    BattleManager.setQueueStatus("displayFX", false);
  }

  void Awake() { if (core == null) core = this; }
}


//(c) Cination - Tsenkilidis Alexandros