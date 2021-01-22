using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ClassDB;
using Sirenix.OdinInspector.Editor.Drawers;
using TMPro;
using UnityEngine.Serialization;

public class InstanceID
{
  public static InstanceID InvalidID = new InstanceID(-1) { ID = -1 };
  private static int NextID = 0;
  public int ID = NextID++;
  public int CharacterID;
  public int SkillID = -1;

  public InstanceID( int characterId )
  {
    CharacterID = characterId;
  }

  public override bool Equals( object obj )
  {
    if ( obj is InstanceID otherId )
      return otherId.ID == ID;

    return false;
  }

  public override int GetHashCode()
  {
    return ID.GetHashCode() + CharacterID.GetHashCode() + SkillID.GetHashCode();
  }

  public override string ToString()
  {
    return $"InstanceID ({ID}) - Character: {CharacterID} - Skill: {SkillID}";
  }
}

public class BattleManager : MonoBehaviour
{

  public static BattleManager core;

  //Note: Elements marked "[HideInInspector]" will not be displayed.


  [Tooltip( "Starting character." )] public int startingCharacter;

  [Tooltip( "Starting battle music id (from ObjectDB.cs)." )]
  public int startingMusicId;

  [Tooltip( "Index of main audio source." )]
  public int musicSourceIndex;


  [Tooltip( "The id of the background sprite (from ObjectDB.cs)." )]
  public int backgroundSpriteId;

  [Tooltip( "Enable character AI." )] public bool autoBattle;

  [Tooltip( "Maximum turn points available." )]
  public int maxTurnPoints;

  public class BattleManagerContext
  {
    public BattleManagerContext()
    {
    }

    public BattleManagerContext( BattleManagerContext original )
    {
      activeSkillId = original.activeSkillId;
      activeCharacterId = original.activeCharacterId;
      runningFunctionIndex = original.runningFunctionIndex;
      actionTargets = new List<InstanceID>(original.actionTargets);
      attackerTeam = new List<InstanceID>(original.attackerTeam);
      defenderTeam = new List<InstanceID>(original.defenderTeam);
      functionQueue = new List<callInfo>(original.functionQueue);
      targetLimit = original.targetLimit;
      activeSkillId = original.activeSkillId;
    }

    private static int NextID = 0;
    public int ContextID = NextID++;

    [Tooltip( "The id of the currently active skill." )]
    public int activeSkillId = -1;

    //Hidden variables
    [Tooltip( "The id of the currently active character." )]
    public InstanceID activeCharacterId = InstanceID.InvalidID;

    [Tooltip( "A list of targets selected for a specific action." )]
    public List<InstanceID> actionTargets = new List<InstanceID>();

    [Tooltip( "Selection limit for action targets." )]
    public int targetLimit;

    [Tooltip( "Current attacker's team." )]
    public List<InstanceID> attackerTeam;

    [Tooltip( "Current defender team." )] public List<InstanceID> defenderTeam;

    [Tooltip( "Current function chain." )] [HideInInspector]
    public List<callInfo> functionQueue = new List<callInfo>();

    public int runningFunctionIndex = -1;

    public void Init( InstanceID activeCharacterID, List<InstanceID> playerTeam, List<InstanceID> enemyTeam )
    {
      this.activeCharacterId = activeCharacterID;
      //Is the character in the enemy or player team?
      if ( enemyTeam.FindIndex( x => x == activeCharacterID ) != -1 )
      {
        attackerTeam = enemyTeam;
        defenderTeam = playerTeam;
      }
      else if ( playerTeam.FindIndex( x => x == activeCharacterID ) != -1 )
      {
        defenderTeam = enemyTeam;
        attackerTeam = playerTeam;
      }
    }

    public override string ToString()
    {
      return $"Context ({ContextID} - Actor {activeCharacterId} - Targets {actionTargets.Count}";
    }
  }

  //Public variables
  [Tooltip( "Current player's team." )] public List<InstanceID> activePlayerTeam = new List<InstanceID>();

  [Tooltip( "Current enemy team." )] public List<InstanceID> activeEnemyTeam = new List<InstanceID>();

  //Public variables
  [Tooltip( "Current player's team." )] public List<int> initialPlayerTeam = new List<int>();

  [Tooltip( "Current enemy team." )] public List<int> initialEnemyTeam = new List<int>();

  // The previous context
  public BattleManagerContext CurrentContext;

  // Used for interruptions
  public BattleManagerContext ReactionContext;
  public List<BattleManagerContext> ReactionQueue = new List<BattleManagerContext>();

  //Hidden variables
  [Tooltip( "Currently active music id." )] [HideInInspector]
  public int activeMusicId = -1;

  [Tooltip( "Active team." )] [HideInInspector]
  //0 - Player team, 1 - Enemy team, -1 default value aka no team active
  public int activeTeam = -1;

  [Tooltip( "Current turn points." )] [HideInInspector]
  public int turnPoints;


  [Tooltip( "This variable can be triggered to skip a turn." )] [HideInInspector]
  bool skipChar;

  [Tooltip( "Currently displayed actions." )] [HideInInspector]
  public List<curActionInfo> curActions = new List<curActionInfo>();

  [FormerlySerializedAs( "characters" )] [Tooltip( "List of active characters." )] [HideInInspector]
  public List<characterInfo> characterInstances = new List<characterInfo>();

  public int findCharacterInstanceIndexById ( InstanceID instanceID )
  {
    return characterInstances.FindIndex( x => x.characterInstanceId == instanceID );
  }

  public characterInfo findCharacterInstanceById ( InstanceID instanceID )
  {
    return characterInstances.Find( x => x.characterInstanceId == instanceID );
  }

  public InstanceID findCharacterInstanceByCharacterDatabaseId ( int instanceID )
  {
    var found = characterInstances.Find( x => x.characterInstanceId.CharacterID == instanceID );
    return found.characterInstanceId;
  }

  //Coroutines
  Coroutine warningRoutine;
  Coroutine musicRoutine;

  void Start()
  {
    //Initiate battle on start only if variable initiator is absent from the scene.
    //Otherwise, we essentially let variable initiator to initiate all the variables and only then initiate battle.
    if ( VariableInitiator.core == null )
    {
      initiateBattle();
    }

  }

  //This function is responsible for initiating combat
  public void initiateBattle()
  {

    //Stopping all coroutines
    StopAllCoroutines();
    BattleMethods.core.StopAllCoroutines();

    //Clearing lists
    characterInstances.Clear();
    SpawnInitialTeams();


    CurrentContext = new BattleManagerContext();

    //Disabling outcome window
    GameObject outcomeScreen = ObjectDB.core.outcomeWidow;
    outcomeScreen.SetActive( false );

    //Setting turn points
    turnPoints = maxTurnPoints;

    //Clearing body
    foreach ( Transform t in ObjectDB.core.battleUIBody.transform )
    {
      Destroy( t.gameObject );
    }

    InstanceID startingCharacterID = null;
    //First, let's make sure both teams have more than 0 characters in them
    if ( activeEnemyTeam.Count > 0 && activePlayerTeam.Count > 0 )
    {

      //Initiate active team
      activeTeam = -1;

      //If a starting character has been set
      if ( startingCharacter != -1 )
      {
        //Is the character in the enemy or player team?
        int enemyIndex = activeEnemyTeam.FindIndex( x => x.CharacterID == startingCharacter );
        int allyIndex = activePlayerTeam.FindIndex( x => x.CharacterID == startingCharacter ) ;
        if ( enemyIndex != -1 )
        {
          startingCharacterID = activeEnemyTeam[enemyIndex];
          activeTeam = 1;
        }
        else if ( allyIndex != -1 )
        {
          startingCharacterID = activePlayerTeam[allyIndex];
          activeTeam = 0;
        }
      }
      else
      {
        //If a starting character has not been set, the first character of the player team will be considered as starting character
        startingCharacterID = activePlayerTeam[0];
        //Setting starting team to player team
        activeTeam = 0;
      }

      //Setting starting character
      CurrentContext.Init( startingCharacterID, activePlayerTeam, activeEnemyTeam );

      if ( activeTeam == -1 )
      {

        Debug.Log( "Something went wrong. Please make sure you have at least 1 character in each team and starting character is set to a valid value." );

      }
      else
      {

        //Setting background
        ObjectDB.core.backgroundSpriteObject.GetComponent<SpriteRenderer>().sprite =
          FunctionDB.core.findSpriteById( backgroundSpriteId );

        //Generating options
        BattleGen.core.mainGen();

        //Generating characters
        charGenInitiator();

        //Starting Coroutines
        StartCoroutine( characterInfoManager() );
        StartCoroutine( turnManager() );
        StartCoroutine( healthManager() );
        StartCoroutine( autoBattleManager() );
        StartCoroutine( animationManager() );
        StartCoroutine( turnDisplayManager() );
        StartCoroutine( navManager() );
        StartCoroutine( musicManager() );
        StartCoroutine( selectionManager() );
        StartCoroutine( actionTargetDisplayManager() );

        //Displaying health above each character
        List<InstanceID> characters = new List<InstanceID>();
        characters.AddRange( activeEnemyTeam );
        characters.AddRange( activePlayerTeam );

        foreach ( InstanceID i in characters )
        {

          //Starting coroutine to display health.
          StartCoroutine( battleAreahealthManager( i, 1.1f ) );
        }
      }


    }

  }

  public void SpawnInitialTeams()
  {
    activePlayerTeam = new List<InstanceID>( );
    activeEnemyTeam = new List<InstanceID>( );

    for ( int i = 0; i  < initialPlayerTeam.Count; ++i )
    {
      int charID = initialPlayerTeam[i];
      var instanceID = new InstanceID( charID );
      activePlayerTeam.Add( instanceID );
    }

    for ( int i = 0; i  < initialEnemyTeam.Count; ++i )
    {
      int charID = initialEnemyTeam[i];
      var instanceID = new InstanceID( charID );
      activeEnemyTeam.Add( instanceID );
    }
  }

  //Character battler generation
  public void charGenInitiator()
  {

    //Getting enemy and player team spawn parent object
    var playerSpawns = ObjectDB.core.playerTeamSpawns;
    var enemySpawns = ObjectDB.core.enemyTeamSpawns;

    //Creating lists of spawn objects
    var playerTeamSpawns = FunctionDB.core.childObjects( playerSpawns.transform );
    var enemyTeamSpawns = FunctionDB.core.childObjects( enemySpawns.transform );

    //Getting info window
    var charInfoWindow = ObjectDB.core.battleUICharacterInfoWindow;

    //Clearing window
    FunctionDB.core.emptyWindow( charInfoWindow );

    //Emptying battle manager's info objects list
    BattleManager.core.characterInstances.Clear();

    //Foreach player and enemy in the respective teams, spawn a battler while there are still spawns available
    BattleGen.core.charGen( playerTeamSpawns, activePlayerTeam, 0 );
    BattleGen.core.charGen( enemyTeamSpawns, activeEnemyTeam, 1 );

  }

  //This function allows invoking methods by names and applying parameters from a parameter array
  public IEnumerator functionQueueCaller( BattleManagerContext context )
  {
    //We need to create a copy of the current list to avoid errors in the senarios were the list is modified during runtime
    var lastFunctionQueue = new List<callInfo>( context.functionQueue );
    var originalContext = new BattleManagerContext( context );;

    for (int i = 0; i < lastFunctionQueue.Count; ++i )
    {
      var ftc = lastFunctionQueue [i];
      if ( ftc.isComment ) continue;

      // if ( !context.functionQueue.Contains( ftc ) )
      // {
      //   break;
      // }

      originalContext.runningFunctionIndex = i;

      //Active char id
      yield return core.call ( originalContext, ftc );
    }

    yield return new WaitForEndOfFrame();
  }

  //This function allows invoking methods by names and applying parameters from a parameter array
  public static IEnumerator reactionQueueCaller( params BattleManagerContext[] contexts )
  {
    foreach ( var context in contexts )
    {
      //We need to create a copy of the current list to avoid errors in the senarios were the list is modified during runtime
      var lastFunctionQueue = new List<callInfo>( context.functionQueue );

      for ( int i = 0; i < lastFunctionQueue.Count; i++ ) {
        callInfo ftc = lastFunctionQueue[i];
        if ( ftc.isComment ) continue;
        if ( !context.functionQueue.Contains( ftc ) )
        {
          break;
        }

        context.runningFunctionIndex = i;

        //Active char id
        yield return core.call ( context, ftc );
      }

      yield return new WaitForEndOfFrame();
    }

    BattleManager.core.ReactionContext = null;
  }

  private IEnumerator call( BattleManagerContext context, callInfo ftc )
  {
    var method = ftc.functionName;

    object[] parametersArray =
      ftc.parametersArray.Select( x => sudoParameterDecoder( x ) ).Prepend( context ).ToArray();

    //Getting current element index
    int queueIndex = FunctionDB.core.findFunctionQueueIndexByCallInfo( context, ftc );

    if ( queueIndex != context.runningFunctionIndex )
    {
      Debug.Log( $"WTF {queueIndex} vs {context.runningFunctionIndex}" );
    }

    //Is our function supposed to wait for previous functions to complete?
    while ( context.functionQueue.Contains( ftc ) && ftc.waitForPreviousFunction )
    {

      //Set is running to false
      var isRunning = false;

      //Is the previous function still running?
      int prevIndex = queueIndex - 1;
      while ( prevIndex != -1 )
      {
        var prev = context.functionQueue[prevIndex];
        if ( !prev.isComment ) break; 
        prevIndex--;
      }

      if ( prevIndex >= 0 )
      {

        //Capture element running status
        isRunning = context.functionQueue[prevIndex].isRunning;

        if ( !isRunning )
        {
          break;
        }

      }
      else
      {
        //No previous element
        break;
      }

      yield return new WaitForEndOfFrame();
    }

    if ( context.functionQueue.Contains( ftc ) )
    {

        //We need to get the method info in order to properly invoke the method
        System.Type type = BattleMethods.core.GetType();
        MethodInfo mi = type.GetMethod( method,
          BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

        //Setting current function as running in the queue
        setQueueStatus( context, method, true );

        //There are two different approaches when it comes to running ordinary methods and coroutines.
        if ( !ftc.isCoroutine )
        {

          //Invoking function.
          try
          {
            //Debug.Log( $"{method} - {queueIndex}" );
            mi.Invoke( BattleMethods.core, parametersArray );
          }
          catch ( Exception e )
          {
            Debug.Log( e );
            var index = FunctionDB.core.findSkillIndexById( context.activeSkillId );
            var skill = Database.dynamic.skills[index];
            Debug.Log( $"Please check {method}. {ftc} - index: [{queueIndex}] of {skill}" );
          }
        }
        else
        {

          //Start Coroutine
          try
          {
            //Debug.Log( $"CR: {method} - {queueIndex}" );
            BattleMethods.core.StartCoroutine( method, parametersArray );
          }
          catch ( Exception e )
          {
            Debug.Log( e );
            Debug.Log( "Please check " + method + "." );
          }
        }
    }
  }

  //This function is used to decode parameter strings by the method caller
  //For example, a string "int:5" will essentially create an object of type int with value 5.
  public static object sudoParameterDecoder( string sp )
  {

    var colonIndex = sp.IndexOf( ":" );
    if ( colonIndex == -1 )
    {
      Debug.Log( "It seems that you have not properly defined your parameters." );
      Debug.Log(
        "You parameters can be in any of the following forms: bool:true or false, string:yourString, float:yourFloat or int:anyInt" );
      return sp;
    }

    var type = sp.Substring( 0, colonIndex );
    var remainder = sp.Substring( colonIndex + 1 );

    switch (type)
    {
      case "string":
        return remainder as object;
      case "int":
        return int.Parse( remainder ) as object;
      case "float":
        return float.Parse( remainder ) as object;
      case "bool":
        return remainder == "true" ? true : false;
      default:
        Debug.Log( "It seems that you have not properly defined your parameters." );
        Debug.Log(
          "You parameters can be in any of the following forms: bool:true or false, string:yourString, float:yourFloat or int:anyInt" );
        return null;
    }

  }


  //This function runs character AI functions
  void runAI( BattleManager.BattleManagerContext context )
  {
    //Getting current character
    var characterInstance = BattleManager.core.findCharacterInstanceById( context.activeCharacterId );
    var curChar = characterInstance.characterCopy;

    //Getting functions to call
    var functionsToCall = curChar.aiFunctions;

    //Storing the functions list to functionQueue
    context.functionQueue = functionsToCall;

    StartCoroutine( functionQueueCaller( context ) );
  }

  //This function manages keyboard navigation
  IEnumerator navManager()
  {

    var curObject = EventSystem.current.currentSelectedGameObject;

    while ( true )
    {

      if ( EventSystem.current.currentSelectedGameObject != null )
      {
        curObject = EventSystem.current.currentSelectedGameObject;
      }
      else
      {
        EventSystem.current.SetSelectedGameObject( curObject );
      }

      yield return new WaitForEndOfFrame();

    }
  }

  //This function manages selected navigation items
  IEnumerator selectionManager()
  {

    GameObject actionCostObject = ObjectDB.core.actionCostObject;
    TextMeshProUGUI acoText = actionCostObject.transform.GetChild( 0 ).gameObject.GetComponent<TextMeshProUGUI>();

    while ( true )
    {

      foreach ( curActionInfo a in curActions )
      {

        if ( a.actionObject == EventSystem.current.currentSelectedGameObject )
        {

          ObjectDB.core.battleUIActionDescriptionObject.GetComponent<TextMeshProUGUI>().text = a.description;

          if ( a.turnPointCost != -1 )
          {
            actionCostObject.SetActive( true );
            acoText.text = " (-" + a.turnPointCost.ToString() + ")";
          }
          else
          {
            actionCostObject.SetActive( false );
          }

          break;
        }

      }

      yield return new WaitForEndOfFrame();
    }
  }

  //This function displays the number of action targets currently selected
  IEnumerator actionTargetDisplayManager()
  {
    GameObject actionTargetsObject = ObjectDB.core.actionTargetsObject;
    TextMeshProUGUI t = actionTargetsObject.transform.GetChild( 0 ).gameObject.GetComponent<TextMeshProUGUI>();

    var context = BattleManager.core.CurrentContext;

    while ( true )
    {
      if ( context.targetLimit > 1 && context.actionTargets.Count > 0 )
      {
        t.text = context.actionTargets.Count.ToString() + " Targets Selected";
        actionTargetsObject.SetActive( true );
      }
      else if ( context.targetLimit == context.actionTargets.Count || context.actionTargets.Count == 0 )
      {
        actionTargetsObject.SetActive( false );
      }

      yield return new WaitForEndOfFrame();
    }

  }

  //This function manages animation playback
  IEnumerator animationManager()
  {
    while ( true )
    {
      foreach ( characterInfo info in characterInstances )
      {
        var instance = info.instanceObject;
        Animator charAnimator = instance.GetComponent<Animator>();

        foreach ( AnimatorControllerParameter animcp in charAnimator.parameters )
        {
          if ( animcp.name == info.currentAnimation )
          {
            charAnimator.SetBool( animcp.name, true );
          }
          else
          {
            charAnimator.SetBool( animcp.name, false );
          }
        }
      }

      yield return new WaitForEndOfFrame();
    }
  }

  //Battle music manager
  IEnumerator musicManager()
  {

    var prevMusicId = -1;

    while ( true )
    {

      if ( activeMusicId == -1 )
      {
        activeMusicId = startingMusicId;
      }

      //If active music id has been changed
      if ( prevMusicId != activeMusicId )
      {

        prevMusicId = activeMusicId;
        FunctionDB.core.changeAudio( activeMusicId, musicSourceIndex );

      }
      else
      {
        //For the background audio we will use changeAudio to smoothly transition to itself when the music has ended, creating a proper loop

        //Getting audio source
        var audio = GetComponents<AudioSource>();

        if ( !audio[musicSourceIndex].isPlaying && !FunctionDB.core.audioTransitioning )
        {
          FunctionDB.core.changeAudio( activeMusicId, musicSourceIndex );
        }
      }

      yield return new WaitForEndOfFrame();
    }
  }

  //This function manages character status
  IEnumerator healthManager()
  {

    while ( true )
    {

      foreach ( characterInfo info in characterInstances )
      {

        InstanceID charId = info.characterInstanceId;

        //Getting currently active character
        character activeCharacter = info.characterCopy;
        //Getting attribute in question
        var charAttribute =
          activeCharacter.characterAttributes[FunctionDB.core.findAttributeIndexById( 0, activeCharacter )];

        //current value
        var curValue = charAttribute.curValue;

        //If health is at 0, make character innactive
        if ( curValue <= 0 )
        {

          info.isAlive = false;
          activeCharacter.isActive = false;

          var onDeath = activeCharacter.getOnDeath();
          if ( onDeath.Count == 0 )
          {
            FunctionDB.core.setAnimation( charId, "death" );
          }
          else
          {
            var c = new BattleManager.BattleManagerContext();
            c.actionTargets = new List<InstanceID>() { };
            c.Init( charId, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
            c.targetLimit = 1;
            c.functionQueue = onDeath;
            StartCoroutine( BattleManager.reactionQueueCaller( c  ) );
          }

          //If the character in question is the active character, skip to the next character
          if ( charId == this.CurrentContext.activeCharacterId )
          {
            skipChar = true;
          }

        }
        else
        {
          info.isAlive = true;
          info.characterCopy.isActive = true;

          //Is the death animation playing ?
          if ( FunctionDB.core.checkAnimation( charId, "death" ) )
          {
            FunctionDB.core.checkAnimation( charId, "idle" );
          }

        }

      }


      yield return new WaitForEndOfFrame();
    }

  }

  //This function displays and continuously update attributes in the battle area
  IEnumerator battleAreahealthManager( InstanceID charId, float yOffset )
  {

    //Getting character instance by id
    GameObject charInstanceGameObjectById = FunctionDB.core.findCharInstanceGameObjectById( charId );

    characterInfo characterInstance = BattleManager.core.findCharacterInstanceById( charId );

    //Getting character by id
    character character = characterInstance.characterCopy;

    //Getting attribute index
    int charAttrIndx = FunctionDB.core.findAttributeIndexById( 0, character );
    //Getting prefab
    GameObject valuePrefab = ObjectDB.core.battleUIValuePrefab;
    //Getting UI area
    GameObject spawnArea = ObjectDB.core.battleUIBody;

    //Adjusted position
    Vector3 pos = charInstanceGameObjectById.transform.position;
    Vector3 newPos = new Vector3( pos.x, pos.y + yOffset, pos.z );

    //Spawning
    GameObject t = Instantiate( valuePrefab, newPos, Quaternion.identity, spawnArea.transform );

    //Displaying info
    while ( true )
    {

      //Remove object?
      if ( charInstanceGameObjectById == null )
      {
        Destroy( t );
        break;
      }

      //Adjusting coordinates
      pos = charInstanceGameObjectById.transform.position;
      newPos = new Vector3( pos.x, pos.y + yOffset, pos.z );
      FunctionDB.uiCoordinateCheck( t, newPos );

      //Updating text
      characterAttribute attr = character.characterAttributes[charAttrIndx];
      t.GetComponentInChildren<TextMeshProUGUI>().text = attr.curValue.ToString() + "/" + attr.maxValue.ToString();
      yield return new WaitForEndOfFrame();
    }

  }

  //This function manages turns
  IEnumerator turnManager()
  {
    bool plan = true;
    while ( true )
    {

      var context = BattleManager.core.CurrentContext;

      //Preplan AI action if not planned
      if ( plan )
      {
        clearThreatArrows();
        preplanAI();
        plan = false;
      }

      if ( turnPoints <= 0 || skipChar )
      {

        var tempRunAI = false;
        BattleMethods.core.toggleActions( context, true );

        if ( activeTeam != -1 )
        {

          var counter = 0;

          while ( true )
          {

            //Getting player and enemy team count
            int playerCount = CurrentContext.attackerTeam.Where( x => core.findCharacterInstanceById( x ).isAlive )
              .ToArray().Count();
            int enemyCount = CurrentContext.defenderTeam.Where( x => core.findCharacterInstanceById( x ).isAlive )
              .ToArray().Count();

            //Since there are only 2 teams, a value above 1 means a third loop.
            //This means that something went wrong (and one of the teams probably won).
            //However, to make sure that we don't continue the battle on second loop if a team has won, we need to make sure that both teams have more than 1 active members.
            if ( counter > 1 || playerCount == 0 || enemyCount == 0 )
            {
              yield return new WaitForSeconds( 2 );
              int victor = playerCount > enemyCount ? 0 : 1;

              EndGame( context, victor );

              //Please add any follow up actions here.
              break;
            }

            //Getting active team.
            List<InstanceID> team = activeTeam == 0 ? CurrentContext.attackerTeam : CurrentContext.defenderTeam;

            // TODO: REPLACE THIS LOGIC ENTIRELY
            //Getting team's char index
            var teamCharIndex = team.FindIndex( x => x == CurrentContext.activeCharacterId );
            if ( teamCharIndex == -1 )
            {
              teamCharIndex = 0;
            }

            //Checking list length
            if (  (team.Count - 1 ) >= teamCharIndex )
            {
              var inc = counter == 1 ? 0 : 1;
              var aCharIndex = FunctionDB.core.activeCharacter( team, teamCharIndex, inc );

              //Does the character exist
              if ( aCharIndex != -1 )
              {
                CurrentContext.activeCharacterId = team[aCharIndex];

                //Set new character id
                //Generate menu
                BattleGen.core.mainGen();

                //If the character's team is the enemy team, active A.I.
                //Likewise, if auto battle is on, activate A.I.
                if ( activeTeam == 1 || autoBattle )
                {
                  BattleMethods.core.toggleActions( context, false );
                  tempRunAI = true;
                }

                break;
              }
            }

            //If function fails to break loop, it means that it failed to find an active player in the given list
            //Swap teams and try again
            swapTeam( CurrentContext );
            counter++;

            // The enemies finished going.
            if ( activeTeam == 0 )
            {
              foreach ( var character in characterInstances )
              {
                character.targetIds.Clear();
                
                //Decrementing Betrayal
                var attr = FunctionDB.core.findAttributeByName(  character.characterInstanceId, "BETRAYAL" );
                if (attr != null && attr.curValue > 0)
                {
                  attr.curValue--;
                }
              }

              EndRound( BattleManager.core.CurrentContext );
              plan = true;
            }

          }

        }

        //Reset turn points
        turnPoints = maxTurnPoints;

        //Reset skip char
        skipChar = false;

        if ( tempRunAI ) runAI( BattleManager.core.CurrentContext );

      }

      yield return new WaitForEndOfFrame();
    }

  }

  private void EndRound( BattleManager.BattleManagerContext context )
  {
    foreach ( InstanceID charId in activePlayerTeam )
    {
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      if ( !characterInstance.isAlive  )
      {
        continue;
      }

      context.activeCharacterId = charId;
      StartCoroutine( character.endRound( charId, context ) );
    }

    foreach ( InstanceID charId in activeEnemyTeam )
    {
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      if ( !characterInstance.isAlive  )
      {
        continue;
      }

      context.activeCharacterId = charId;
      StartCoroutine( character.endRound( charId, context ) );
    }
  }

  private void EndGame( BattleManagerContext context, int victor )
  {
    //Outcome


    //Getting outcome screen
    GameObject outcomeScreen = ObjectDB.core.outcomeWidow;

    ObjectDB.core.story._inkStory.ChoosePathString( "FinalScreenTiff" );
    string tiffLine = ObjectDB.core.story._inkStory.Continue();

    //Getting text object
    Text txt = outcomeScreen.transform.GetChild( 0 ).gameObject.GetComponent<Text>();
    txt.text = tiffLine; // "Team " + victor.ToString() + " wins!" + " Try again, enjoy multiple realities of shit outcomes. There are so many universes where you screwed up even more, such a joy to watch when I have my morning shit. ";

    //Displaying outcome
    outcomeScreen.SetActive( true );

    //toggling actions
    BattleMethods.core.toggleActions( context, false );
  }

  //This method simply swaps the team
  private void swapTeam( BattleManagerContext context )
  {
    //Swapping team
    if ( activeTeam == 1 )
    {
      //Set player team as team
      activeTeam = 0;
      //Setting new active char id
      context.activeCharacterId = context.attackerTeam[0];

    }
    else
    {
      //Set enemy team as team
      activeTeam = 1;
      //Setting new active char id
      context.activeCharacterId = context.defenderTeam[0];
    }

  }

  //Displaying turn info on screen
  IEnumerator turnDisplayManager()
  {

    //Getting turn object
    GameObject turnObject = ObjectDB.core.turnObject;
    //Getting child object
    GameObject child = turnObject.transform.GetChild( 0 ).gameObject;

    //Updating label
    while ( true )
    {
      child.GetComponent<TextMeshProUGUI>().text = turnPoints.ToString() + " / " + maxTurnPoints.ToString();
      yield return new WaitForEndOfFrame();
    }
  }

  //This function manages auto battle
  IEnumerator autoBattleManager()
  {
    BattleManagerContext context = BattleManager.core.CurrentContext;
    bool state = false;
    while ( true )
    {
      if ( autoBattle && !state )
      {
        state = true;
        if ( activeTeam != -1 && context.activeCharacterId != InstanceID.InvalidID ) runAI( context );

      }
      else if ( state && !autoBattle ) state = false;

      yield return new WaitForEndOfFrame();
    }
  }

  //This function is responsible for monitoring character info changes
  IEnumerator characterInfoManager()
  {

    while ( true )
    {

      foreach ( characterInfo info in characterInstances )
      {

        if ( info.uiObject != null )
        {

          //Getting character
          var characterInstance = BattleManager.core.findCharacterInstanceById( info.characterInstanceId );
          var character = characterInstance.characterCopy;

          //Attributes list
          List<characterAttribute> attributes = character.characterAttributes;

          //Checking attribute validity
          foreach ( characterAttribute ca in attributes )
          {
            //Boundaries
            if ( ca.curValue > ca.maxValue )
            {
              ca.curValue = ca.maxValue;
            }

            if ( ca.curValue < 0 )
            {
              ca.curValue = 0;
            }
          }

          //Getting status bar
          GameObject statusBar = info.uiObject.transform.GetChild( 5 ).gameObject;

          //Should the status bar be active ?
          if ( info.characterInstanceId == CurrentContext.activeCharacterId && !statusBar.activeSelf )
            statusBar.SetActive( true );
          else if ( info.characterInstanceId != CurrentContext.activeCharacterId && statusBar.activeSelf )
            statusBar.SetActive( false );

          info.uiObject.transform.GetChild( 6 ).gameObject.SetActive( !character.isActive );


          //Getting attribute slots
          GameObject attributeSlot1 = info.uiObject.transform.GetChild( 2 ).gameObject;
          GameObject attributeSlot2 = info.uiObject.transform.GetChild( 3 ).gameObject;
          GameObject attributeSlot3 = info.uiObject.transform.GetChild( 4 ).gameObject;

          //Setting attributes
          if ( attributes.Count >= 2 )
          {
            attributeSlot1.GetComponent<TextMeshProUGUI>().text = attributes[0].name + " " +
                                                                  attributes[0].curValue.ToString() + " / " +
                                                                  attributes[0].maxValue.ToString();
            attributeSlot2.GetComponent<TextMeshProUGUI>().text = attributes[1].name + " " +
                                                                  attributes[1].curValue.ToString() + " / " +
                                                                  attributes[1].maxValue.ToString();
            attributeSlot3.GetComponent<TextMeshProUGUI>().text = attributes[2].name + " " +
                                                                  attributes[2].curValue.ToString() + " / " +
                                                                  attributes[2].maxValue.ToString();
          }
          else
          {
            Debug.Log(
              "The default configuration requires at least 2 attributes per character. Please add more attributes or change the configuration." );
          }

        }


      }

      yield return new WaitForEndOfFrame();
    }

  }

  //This function displays a warning to the player for a specified amount of time
  IEnumerator displayWarning( string warning, float time )
  {

    //Getting warning object and child text object
    GameObject warningObject = ObjectDB.core.warningObject;
    Transform warningChild = warningObject.transform.GetChild( 0 );

    //setting text
    warningChild.gameObject.GetComponent<TextMeshProUGUI>().text = warning;

    //displaying object
    warningObject.SetActive( true );

    //waiting
    yield return new WaitForSeconds( time );

    //Hiding object
    warningObject.SetActive( false );
  }


  //This function is used to change a function's queue status.
  public static void setQueueStatus( BattleManagerContext context, string functionName, bool status )
  {
    int runningFunctionIndex = context.runningFunctionIndex;
    if ( ( runningFunctionIndex > -1 ) && runningFunctionIndex < context.functionQueue.Count )
    {
      if ( context.functionQueue[runningFunctionIndex].functionName == functionName )
      {
         context.functionQueue[runningFunctionIndex].isRunning = status;
      }
      else
      {
        int qIndex = FunctionDB.core.findFunctionQueueIndexByName( context, functionName );
        //Debug.Log( $"{functionName} - Had to find by name {runningFunctionIndex} -> {qIndex}" );
        if ( qIndex >= 0 && qIndex < context.functionQueue.Count )
        {
          context.functionQueue[qIndex].isRunning = status;
        }
      }
    }

  }

  //Initiating warning coroutine
  public void startWarningRoutine( string warning, float time )
  {
    if ( warningRoutine != null ) StopCoroutine( warningRoutine );
    warningRoutine = StartCoroutine( displayWarning( warning, time ) );
  }


  //This function toggle auto battle
  public void toggleAuto()
  {

    //Get button gameObject
    GameObject autoButtonObject = ObjectDB.core.autoBattleButtonObject;
    //Getting text
    TextMeshProUGUI autoText = autoButtonObject.transform.GetChild( 0 ).gameObject.GetComponent<TextMeshProUGUI>();

    if ( autoBattle )
    {
      autoBattle = false;
      autoText.text = "Auto Off";
    }
    else
    {
      autoBattle = true;
      autoText.text = "Auto On";
    }

  }

  void preplanAI()
  {
    // TODO: Fix this to use instance data instead of dynamic
    foreach ( var charInstanceID in activeEnemyTeam )
    {
      int characterIndex = findCharacterInstanceIndexById( charInstanceID );
      if ( characterInstances[characterIndex].targetIds.Count == 0 )
      {
        BattleMethods.core.preplanAI( charInstanceID );
        setThreatArrows( charInstanceID );
      }
    }
  }

  public void replanAI()
  {
    clearThreatArrows();
    foreach ( var characterId in activeEnemyTeam )
    {
      int characterIndex = 0;
      for ( int i = 0; i < characterInstances.Count; i++ )
      {
        if ( characterInstances[i].characterInstanceId == characterId )
        {
          characterIndex = i;
        }
      }

      BattleMethods.core.preplanAI( characterId );
      setThreatArrows( characterId );
    }
  }

  public void setThreatArrows( InstanceID characterId )
  {
    characterInfo charInfo = findCharacterInstanceById( characterId );
    
    var attrBetrayal = FunctionDB.core.findAttributeByName(  characterId, "BETRAYAL" );
    var attrHP = FunctionDB.core.findAttributeByName(  characterId, "HP" );
    if (attrBetrayal != null && attrBetrayal.curValue > 0 || attrHP.curValue <= 0)
    {
      return; 
    }
    
    for ( int i = 0; i < charInfo.targetIds.Count; i++ )
    {
      var threatSpawn = FunctionDB.core.findCharSpawnById( charInfo.targetIds[i] );
      var threatArrow = Instantiate( ObjectDB.core.ThreatArrowPrefab, charInfo.spawnPointObject.transform.position,
        Quaternion.identity );
      threatArrow.transform.LookAt( threatSpawn.transform.position );
      charInfo.threatArrows.Add( threatArrow );
    }
  }

  public void clearThreatArrows()
  {
    foreach ( var charInfo in characterInstances )
    {
      if ( charInfo.threatArrows.Count > 0 )
      {
        foreach ( var target in charInfo.threatArrows )
        {
          Destroy( target );
        }

        charInfo.threatArrows.Clear();
      }
    }
  }

  void Awake()
  {
    if ( core == null ) core = this;
  }
}


//(c) Cination - Tsenkilidis Alexandros