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
      functionQueue = new List<callInfo>(original.functionQueue.ToArray());
      targetLimit = original.targetLimit;
      activeSkillId = original.activeSkillId;
      DEBUG = original.DEBUG;
    }

    private static int NextID = 0;
    public int ContextID = NextID++;

    public bool DEBUG = false;
    
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
  
  public List<InstanceID> turnOrder = new List<InstanceID>();

  //Public variables
  [Tooltip( "Initial player's team." )] public List<int> initialPlayerTeam = new List<int>();
  
  [Tooltip( "Initial enemy team." )]
  public WaveAsset InitialWave;

  public int WaveIndex = -1;

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
    UpdateInkVariables();
    initiateBattle();
    StartStory();
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
        
        // Remove dud references used for spacing
        BattleManager.core.activePlayerTeam.RemoveAll( x => x.CharacterID == -1 );
        BattleManager.core.activeEnemyTeam.RemoveAll( x => x.CharacterID == -1 );

        //Starting Coroutines
        StartCoroutine( characterInfoManager() );
        StartCoroutine( newTurnManager() );
        //StartCoroutine( turnManager() );
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

    if ( InitialWave != null )
    {
      WaveIndex = Database.dynamic.waves.IndexOf( InitialWave.wave );

      var nextWave = Database.dynamic.waves[WaveIndex];
      for ( int i = 0; i  < nextWave.Creatures.Count; ++i )
      {
        int charID = nextWave.Creatures[i];
        var instanceID = new InstanceID( charID );
        activeEnemyTeam.Add( instanceID );
      }

    }
  }

  public bool WavesDone()
  {
    return WaveIndex >= Database.dynamic.waves.Count-1;
  }

  public void NextWave()
  {
    WaveIndex++;
    SpawnEnemyTeamWave();
    
    // Remove empty character reference
    BattleManager.core.activePlayerTeam.RemoveAll( x => x.CharacterID == -1 );
    BattleManager.core.activeEnemyTeam.RemoveAll( x => x.CharacterID == -1 );
  }

  public void SpawnEnemyTeamWave()
  {
    activeEnemyTeam.Clear();
    Wave nextWave = Database.dynamic.waves[WaveIndex];
    for ( int i = 0; i  < nextWave.Creatures.Count; ++i )
    {
      int charID = nextWave.Creatures[i];
      if ( charID > -1 )
      {
        var instanceID = new InstanceID( charID );
        activeEnemyTeam.Add( instanceID );
      }
      else
      {
        activeEnemyTeam.Add( null );
      }
    }
    
    clearThreatArrows();
    charGenNextWave();

    StartCoroutine( newRound( nextWave ) );
  }

  public IEnumerator newRound( Wave nextWave  )
  {
    
    for ( int i = 0; i < activeEnemyTeam.Count && i < nextWave.onSpawn.Count; ++i )
    {
      var instanceId = activeEnemyTeam[i];
      var onSpawnList = new List<callInfo>( nextWave.onSpawn[i].actions );

      if ( onSpawnList.Count > 0 )
      {
				    BattleManager.BattleManagerContext c = new BattleManager.BattleManagerContext();
				    c.Init( instanceId, BattleManager.core.activePlayerTeam, BattleManager.core.activeEnemyTeam );
				    c.functionQueue = onSpawnList;
				    c.activeSkillId = -1;
				    c.actionTargets.Clear();
				    yield return BattleManager.core.functionQueueCaller( c );
      }
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

  public void charGenNextWave()
  {
    var enemySpawns = ObjectDB.core.enemyTeamSpawns;
    var enemyTeamSpawns = FunctionDB.core.childObjects( enemySpawns.transform );
    
    //Getting info window
    //var charInfoWindow = ObjectDB.core.battleUICharacterInfoWindow;

    //Clearing window
    //FunctionDB.core.emptyWindow( charInfoWindow );

    //Emptying battle manager's info objects list
    for ( int i = BattleManager.core.characterInstances.Count-1; i >=0; --i)
    {
      var instance = BattleManager.core.characterInstances[i];
      // Cheap and dirty
      if ( instance.characterInstanceId.CharacterID > 10 && instance.isAlive == false )
      {
        Debug.Log( $"Removing {instance.characterCopy.name}." );
        BattleManager.core.characterInstances.RemoveAt( i );
      }
    }

    BattleGen.core.charGen( enemyTeamSpawns, activeEnemyTeam, 1 );
  }

  //This function allows invoking methods by names and applying parameters from a parameter array
  public IEnumerator functionQueueCaller( BattleManagerContext context )
  {
    //We need to create a copy of the current list to avoid errors in the senarios were the list is modified during runtime
    var lastFunctionQueue = new List<callInfo>( context.functionQueue );
    var originalContext = new BattleManagerContext( context );;

    for (originalContext.runningFunctionIndex = 0; originalContext.runningFunctionIndex < lastFunctionQueue.Count; ++originalContext.runningFunctionIndex )
    {
      var ftc = lastFunctionQueue [originalContext.runningFunctionIndex];
      if ( ftc.isComment ) continue;

      if (originalContext.DEBUG) Debug.Log( $"{originalContext.runningFunctionIndex} - {ftc}" );
      
      //Active char id
      yield return core.call ( originalContext, ftc );
    }

    //Debug.Log( $"Escaped {context.ContextID}" );
    yield return new WaitForEndOfFrame();
  }

  //This function allows invoking methods by names and applying parameters from a parameter array
  public static IEnumerator reactionQueueCaller( params BattleManagerContext[] contexts )
  {
    foreach ( var context in contexts )
    {
      //We need to create a copy of the current list to avoid errors in the senarios were the list is modified during runtime
      var lastFunctionQueue = new List<callInfo>( context.functionQueue );

      for ( context.runningFunctionIndex = 0; context.runningFunctionIndex < lastFunctionQueue.Count; context.runningFunctionIndex++ ) {
        callInfo ftc = lastFunctionQueue[context.runningFunctionIndex];
        
        if (context.DEBUG) Debug.Log( $"Reaction: {context.runningFunctionIndex} - {ftc}" );
        if ( ftc.isComment ) continue;
        if ( !context.functionQueue.Contains( ftc ) )
        {
          break;
        }

        //Active char id
        yield return core.call ( context, ftc );
      }
      
      if (context.DEBUG) Debug.Log( $"Reaction Escaped {context.ContextID}" );
      yield return new WaitForEndOfFrame();
    }

    BattleManager.core.ReactionContext = null;
  }

  private IEnumerator call( BattleManagerContext context, callInfo ftc )
  {
    //Getting current element index
    int queueIndex = FunctionDB.core.findFunctionQueueIndexByCallInfo( context, ftc );
    if ( queueIndex != context.runningFunctionIndex )
    {
      Debug.Log( $"WTF {queueIndex} vs {context.runningFunctionIndex}" );
    }

    //Is our function supposed to wait for previous functions to complete?
    while ( context.runningFunctionIndex <= queueIndex && ftc.waitForPreviousFunction )
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
    
    if ( queueIndex != context.runningFunctionIndex )
    {
      Debug.Log( $"Jump happened: {queueIndex} vs {context.runningFunctionIndex}" );
    }

    callFtc( context, ftc, queueIndex );
  }

  public void callFtc(BattleManagerContext context, callInfo ftc, int queueIndex)
  {
    var method = ftc.functionName;

    object[] parametersArray =
      ftc.parametersArray.Select( x => sudoParameterDecoder( x ) ).Prepend( context ).ToArray();
    
    if ( context.functionQueue.Contains( ftc ) && context.runningFunctionIndex == queueIndex )
    {

        //We need to get the method info in order to properly invoke the method
        System.Type type = BattleMethods.core.GetType();
        MethodInfo mi = type.GetMethod( method,
          BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );

        if ( mi == null )
        {
          Debug.LogError( $"Invalid method {method}. in {context.activeSkillId}." );
        }

        // Default parameters support 
        var parametersFinal = mi.GetParameters().Select(param => param.HasDefaultValue ? param.DefaultValue : null).ToArray();
        for ( int i = 0; i < parametersArray.Length && i < parametersFinal.Length; ++i )
        {
          parametersFinal[i] = parametersArray[i];
        }

        //Setting current function as running in the queue
        setQueueStatus( context, method, true );

        //There are two different approaches when it comes to running ordinary methods and coroutines.
        if ( !ftc.isCoroutine )
        {

          //Invoking function.
          try
          {
            //Debug.Log( $"{method} - {queueIndex}" );
            mi.Invoke( BattleMethods.core, parametersFinal );
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
      Debug.Log( $"It seems that you have not properly defined your parameters. ({sp})" );
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
        var view = instance.GetComponent<Character>();

        if ( view != null )
        {
          info.currentAnimationTime += Time.deltaTime;
          if (info.currentAnimation != null && (info.currentAnimationAsset == null || info.currentAnimationAsset?.Name != info.currentAnimation) )
          {
            info.currentAnimationFrame = 0;
            info.currentAnimationTime = 0f;
            info.currentAnimationAsset = info.GetAnimationAsset( info.currentAnimation );
          }

          if ( info.currentAnimationAsset != null )
          {
            // Advance frames
            float frameRate = 1f / info.currentAnimationAsset.FPS;
            int frame = Mathf.RoundToInt( info.currentAnimationTime / frameRate );
            if ( info.currentAnimationAsset.Looping )
            {
              frame = frame % info.currentAnimationAsset.sprites.Length;
            }
            else
            {
              frame = Math.Min( info.currentAnimationAsset.sprites.Length-1, frame );
            }

            info.currentAnimationFrame = frame;

            Sprite anim = info.currentAnimationAsset.sprites[frame];
            view.spriteRenderer.sprite = anim;
          }
        }

        Animator charAnimator = instance.GetComponent<Animator>();
        if ( charAnimator )
        {
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
        if ( curValue <= 0 && info.isAlive )
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
        else if ( curValue > 0 ) 
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

  public void StartHealthManagerForCharacter( InstanceID charID, Vector3 offset )
  {
    
    StartCoroutine( battleAreahealthManager( charID, offset ) );
  }

  //This function displays and continuously update attributes in the battle area
  IEnumerator battleAreahealthManager( InstanceID charId, Vector3 offset )
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
    Vector3 newPos = pos + offset;

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
      newPos = pos + offset;
      FunctionDB.uiCoordinateCheck( t, newPos );

      //Updating text
      characterAttribute attr = character.characterAttributes[charAttrIndx];
      t.GetComponentInChildren<TextMeshProUGUI>().text = attr.curValue.ToString() + "/" + attr.maxValue.ToString();
      yield return new WaitForEndOfFrame();
    }

  }

  private void StartOfRound()
  {
    preplanTurnOrder();
    clearThreatArrows();
    preplanAI();
  }

  private void preplanTurnOrder()
  {
    BattleManager.core.turnOrder.Clear();
    BattleManager.core.turnOrder.AddRange( BattleManager.core.activePlayerTeam );
    BattleManager.core.turnOrder.AddRange( BattleManager.core.activeEnemyTeam );
    BattleManager.core.turnOrder.RemoveAll( x => x.CharacterID == -1 );
    BattleManager.core.turnOrder.Sort( turnComparison );
  }

  private int turnComparison( InstanceID a, InstanceID b )
  {
    var cA = BattleManager.core.findCharacterInstanceById( a );
    var cB = BattleManager.core.findCharacterInstanceById( b );

    // Highest speed should go first, otherwise don't change order
    return cB.characterCopy.speed.CompareTo( cA.characterCopy.speed );
  }

  private void EndOfWaveCheck( out bool waveEnded, out bool playerDefeat )
  {
      //Getting player and enemy team count
      int playerCount = CurrentContext.attackerTeam.Where( x => core.findCharacterInstanceById( x ).isAlive )
        .ToArray().Count();
      int enemyCount = CurrentContext.defenderTeam.Where( x => core.findCharacterInstanceById( x ).isAlive )
        .ToArray().Count();

      waveEnded = ( playerCount == 0 || enemyCount == 0 );
      playerDefeat = playerCount == 0;
  }

  private void StartOfTurn(BattleManagerContext context)
  {
    turnPoints = maxTurnPoints;
    BattleMethods.core.toggleActions( context, true );
    if ( DEBUG) Debug.Log( $"Turn started" );
  }

  private bool DEBUG = false;
  IEnumerator newTurnManager()
  {
    bool gameOver = false;
    while ( !gameOver )
    {

      var context = BattleManager.core.CurrentContext;

      StartOfRound();
      if (DEBUG) Debug.Log( $"Round started." );

      for ( int i = 0; i < core.turnOrder.Count; ++i )
      {
        skipChar = false;
        
        EndOfWaveCheck( out bool waveEnded, out bool playersDefeated );

        if ( waveEnded )
        {
          if ( playersDefeated )
          {
            if (DEBUG) Debug.Log( $"Players defeated." );

            yield return new WaitForSeconds( 2 );
            EndGame( context, 1 );
            gameOver = true;
            break;
          }

          if ( WavesDone() )
          {
            if (DEBUG) Debug.Log( $"Players win." );
            
            yield return new WaitForSeconds( 2 );
            EndGame( context, 0 );
            
          }
          else
          {
            if (DEBUG) Debug.Log( $"Next wave." );

            yield return new WaitForSeconds( 2 );
            NextWave();
            break;
          }
        }

        var instanceId = core.turnOrder[i];
        var characterInstance = BattleManager.core.findCharacterInstanceById( instanceId );
        bool isPlayerTeam = core.activePlayerTeam.Contains( instanceId );
        BattleManager.core.activeTeam  = isPlayerTeam ? 0 : 1;
        
        if (DEBUG) Debug.Log( $"Checking if {characterInstance.characterCopy.name} wasn't alive" );

        // Don't run actions for dead people ^_^
        if ( !characterInstance.isAlive ) continue;
        
        // Skip turns for the stunned:
        var stun = FunctionDB.core.findAttributeByName( instanceId , "STUN" );
        if ( stun != null && stun.curValue > 0 )
        {
	          continue;
        }

        context.activeCharacterId = instanceId;

        StartOfTurn(context);
        
        //Set new character id
        //Generate menu
        if (DEBUG) Debug.Log( $"Generating menu for {characterInstance.characterCopy.name}" );

        BattleGen.core.mainGen();
        if ( !isPlayerTeam || autoBattle )
        {
          BattleMethods.core.toggleActions( context, false );
          runAI( context ); 
          if (DEBUG) Debug.Log( $"AI run {characterInstance.characterCopy.name}" );
        }
        
        while ( turnPoints > 0 && !skipChar )
        {
          yield return new WaitForEndOfFrame();
        }
        BattleMethods.core.toggleActions( context, false );

        if (DEBUG) Debug.Log( $"Turn finished {characterInstance.characterCopy.name}" );
      }
      
      EndRound( context );
      
      yield return new WaitForEndOfFrame();
    }
  }

  private void EndRound( BattleManager.BattleManagerContext context )
  {
    foreach ( var character in characterInstances )
    {
      character.preplannedTargets.Clear();
      
      //Decrementing Betrayal
      var attr = FunctionDB.core.findAttributeByName(  character.characterInstanceId, "BETRAYAL" );
      if (attr != null && attr.curValue > 0)
      {
        attr.curValue--;
      }
    }
    
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

  private void UpdateInkVariables()
  {
    int deathCount = PlayerPrefs.GetInt( "FAILURES", 0 );
    int resets = deathCount / 11;
    int deathVariable = deathCount % 11;
    ObjectDB.core.story._inkStory.variablesState["Resets"] = resets;
    ObjectDB.core.story._inkStory.variablesState["Deaths"] = deathVariable;
  }

  private void StartStory()
  {
    ObjectDB.core.story.Bark( "Start", "0" );
  }

  private void EndGame( BattleManagerContext context, int victor )
  {
    //Outcome
    ObjectDB.core.story._inkStory.variablesState["Victory"] = (victor == 0);
  
    // Right now you can only lose. 
    int deathCount = PlayerPrefs.GetInt( "FAILURES", 0 );
    deathCount++;
    PlayerPrefs.SetInt( "FAILURES", deathCount );
    PlayerPrefs.Save();

    //Getting outcome screen
    GameObject outcomeScreen = ObjectDB.core.outcomeWidow;
    UpdateInkVariables();
  
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

          var charId = info.characterInstanceId;
          //Getting character
          var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
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


          //Setting data
          var slot = info.uiObject.GetComponent<BattleCharacterSlot>();

          //Icon
          slot.PortraitIcon.sprite = character.icon;

          //Name
          slot.TextName.text = character.name;

          if ( attributes.Count >= 2 )
          {
            slot.TextHealth.text = attributes[0].name + " " +
                                                                  attributes[0].curValue.ToString() + " / " +
                                                                  attributes[0].maxValue.ToString();
            slot.TextMana.text = attributes[1].name + " " +
                                                                  attributes[1].curValue.ToString() + " / " +
                                                                  attributes[1].maxValue.ToString();
            slot.TextSpecial.text = attributes[2].name + " " +
                                                                  attributes[2].curValue.ToString() + " / " +
                                                                  attributes[2].maxValue.ToString();
          }
          else
          {
            Debug.Log(
              "The default configuration requires at least 2 attributes per character. Please add more attributes or change the configuration." );
          }
          
          // Status Effects
          var stun = FunctionDB.core.findAttributeByName(  charId, "STUN" )?.curValue ?? 0f;
          var paralysis = FunctionDB.core.findAttributeByName(  charId, "PARALYSIS" )?.curValue ?? 0f;
          var counter = FunctionDB.core.findAttributeByName(  charId, "COUNTER" )?.curValue ?? 0f;
          var defend = FunctionDB.core.findAttributeByName(  charId, "DEFENDROUNDS" )?.curValue ?? 0f;
          var regenerate = FunctionDB.core.findAttributeByName(  charId, "REGENERATE" )?.curValue ?? 0f;
          var warsong = FunctionDB.core.findAttributeByName(  charId, "WARSONG" )?.curValue ?? 0f;
          var poison = FunctionDB.core.findAttributeByName(  charId, "POISON" )?.curValue ?? 0f;
          var stress = FunctionDB.core.findAttributeByName(  charId, "STRESS" )?.curValue ?? 0f;
          var stressPower = FunctionDB.core.findAttributeByName(  charId, "STRESSPOWER" )?.curValue ?? 0f;
          var enrage = FunctionDB.core.findAttributeByName(  charId, "ENRAGE" )?.curValue ?? 0f;
          var doom = FunctionDB.core.findAttributeByName(  charId, "DOOM" )?.curValue ?? 0f;
          var despair = FunctionDB.core.findAttributeByName(  charId, "DESPAIR" )?.curValue ?? 0f;

          Dictionary<GameObject, float> mappings = new Dictionary<GameObject, float>();
          mappings[slot.CounterIcon.gameObject] = counter;
          mappings[slot.ParalysisIcon.gameObject] = paralysis;
          mappings[slot.StunIcon.gameObject] = stun;
          mappings[slot.DefendedIcon.gameObject] = defend;
          mappings[slot.RegenerateIcon.gameObject] = regenerate;
          mappings[slot.WarsongIcon.gameObject] = warsong;
          mappings[slot.PoisonIcon.gameObject] = poison;
          mappings[slot.StressIcon.gameObject] = stress;
          mappings[slot.EnrageIcon.gameObject] = enrage;
          mappings[slot.DoomIcon.gameObject] = doom;
          mappings[slot.DespairIcon.gameObject] = despair;

          foreach ( var kvp in mappings )
          {
            if ( kvp.Value > 0 && !kvp.Key.activeSelf ) 
              kvp.Key.SetActive( true );
            else if ( kvp.Key.activeSelf && kvp.Value <= 0f ) 
              kvp.Key.SetActive( false );
          }

          slot.StressCount.text = $"{stressPower:0}";
          slot.DoomCount.text = $"{doom:0}";
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
    foreach ( var charInstanceID in activeEnemyTeam )
    {
      if ( charInstanceID.CharacterID > -1 ) 
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
      BattleMethods.core.preplanAI( characterId );
      setThreatArrows( characterId );
    }
  }

  public void setThreatArrows( InstanceID characterId )
  {
    characterInfo charInfo = findCharacterInstanceById( characterId );
    
    var attrBetrayal = FunctionDB.core.findAttributeByName(  characterId, "BETRAYAL" );
    var attrHP = FunctionDB.core.findAttributeByName(  characterId, "HP" );
    var attrStun = FunctionDB.core.findAttributeByName(  characterId, "STUN" );
    var attrParalyze = FunctionDB.core.findAttributeByName(  characterId, "PARALYZE" );
    if ( attrHP.curValue <= 0
      || (attrBetrayal?.curValue ?? 0f) > 0 
      || (attrStun?.curValue ?? 0f) > 0 
      || (attrParalyze?.curValue ?? 0f) > 0 )
    {
      return; 
    }
    
    for ( int i = 0; i < charInfo.preplannedTargets.Count; i++ )
    {
      var pt = charInfo.preplannedTargets[i];
      GameObject prefab = null;
      switch (pt.type)
      {
        case targetProvider.ArrowType.NormalHostile:
          prefab = ObjectDB.core.NormalThreatArrowPrefab;
          break;
        case targetProvider.ArrowType.SpecialHostile:
          prefab = ObjectDB.core.SpecialThreatArrowPrefab;
          break;
        case targetProvider.ArrowType.NormalFriendly:
          prefab = ObjectDB.core.FriendlyArrowPrefab;
          break;
        case targetProvider.ArrowType.SpecialFriendly:
          prefab = ObjectDB.core.SpecialFriendlyArrowPrefab;
          break;
      }
      
      for ( int j = 0; j < pt.targetIds.Count; ++j )
      {
        var threatSpawn = FunctionDB.core.findCharSpawnById( pt.targetIds[j] );
        var threatArrow = Instantiate( prefab, charInfo.spawnPointObject.transform.position,
          Quaternion.identity );
        threatArrow.transform.LookAt( threatSpawn.transform.position );
        threatArrow.transform.parent = ObjectDB.core.threatArrowSpawn.transform;
        charInfo.threatArrows.Add( threatArrow );
      }
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

    foreach (Transform arrow in ObjectDB.core.threatArrowSpawn.transform)
    {
      Destroy( arrow.gameObject );
    }
  }

  void Awake()
  {
    if ( core == null ) core = this;
  }
}


//(c) Cination - Tsenkilidis Alexandros