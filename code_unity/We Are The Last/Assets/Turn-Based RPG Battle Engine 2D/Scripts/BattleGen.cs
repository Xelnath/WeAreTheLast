using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using ClassDB;
using TMPro;

//This script generates various lists
public class BattleGen : MonoBehaviour
{

  //Reference used by other scripts to access the active instance of BattleGen
  public static BattleGen core;

  GameObject actionsWindow;
  GameObject optionPrefab;

  public static StoryControl story;

  //The list of main options
  public List<actionInfo> mainActions = new List<actionInfo>();

  //Generates main options menu
  public void mainGen()
  {

    //Getting UI
    actionsWindow = ObjectDB.core.battleUIActionsWindow;
    optionPrefab = ObjectDB.core.battleUIOptionPrefab;

        story = ObjectDB.core.story;

    //Emptying actions window
    FunctionDB.core.emptyWindow(actionsWindow);

    //Used for setting focus
    bool focusSet = false;

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    //Spawning options and assigning listeners
    foreach (actionInfo ai in mainActions)
    {

      //Creating object
      GameObject g = Instantiate(optionPrefab, actionsWindow.transform);

      //Managing button
      Button b = g.GetComponent<Button>();

      //Setting focus
      if (!focusSet)
      {
        EventSystem.current.SetSelectedGameObject(g);
        focusSet = true;
      }

      //Getting functions list
      var fl = ai.functions;

      foreach (string f in fl)
      {
        //Listeners
        b.onClick.AddListener(delegate { this.Invoke(f, 0f); });
      }

      //Setting button text
      g.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = ai.name;

      //Adding element to current action list
      curActionInfo cai = new curActionInfo();
      cai.actionObject = g;
      cai.description = ai.description;
      cai.turnPointCost = -1;

      BattleManager.core.curActions.Add(cai);

    }

  }


  //Spawning characters
  public void charGen(List<GameObject> teamSpawns, List<InstanceID> charTeam, int team)
  {

    //Character info prefab and window
    var charInfoPrefab = ObjectDB.core.battleUICharacterInfoPrefab;
    var charInfoWindow = ObjectDB.core.battleUICharacterInfoWindow;

    //Removing children before respawning
    foreach (GameObject spawn in teamSpawns)
    {
      foreach (Transform t in spawn.transform)
      {
        Destroy(t.gameObject);
      }
    }

    //A counter is kept in order to ensure that there are available spawn points
    var counter = 0;

    foreach (InstanceID charId in charTeam)
    {
      //Getting character
      int charIndex = FunctionDB.core.findCharacterTemplateIndexByCharacterID( charId.CharacterID );
      if ( charIndex == -1 )
      {
        Debug.LogError( $"Missing character record for ID: {charId}." );
        continue;
      }

      var character = Database.dynamic.characters[charIndex];

      //Instantiating character prefab if there is a slot available
      if (teamSpawns.Count > counter)
      {
        GameObject instance = Instantiate(character.prefab, teamSpawns[counter].transform);
        
        //Setting charId
        instance.GetComponent<Character>().id = character.id.ToString();
        
        //Setting GameObject's name
        instance.name = character.name;

        //Setting sprite direction
        var spriteRotation = team == 1 ? 180 : 0;
        instance.transform.Rotate(0, spriteRotation, 0);

        story.Actors.Add(instance.GetComponent<Character>());
        
        //Adding element to instances
        characterInfo info = new characterInfo(character);
        info.characterInstanceId = charId;
        info.instanceObject = instance;
        info.spawnPointObject = teamSpawns[counter];
        info.targetIds = new List<InstanceID>();
        info.threatArrows = new List<GameObject>();

        BattleManager.core.characterInstances.Add(info);

        //Incrementing character
        counter++;

        //Spawning UI
        //We only need to spawn UI for the player team
        if (team == 0)
        {

          //Instatiating object
          GameObject g = Instantiate(charInfoPrefab, charInfoWindow.transform);

          var characterInstance = BattleManager.core.findCharacterInstanceById(charId);
          characterInstance.uiObject = g;

          //Setting data
          Transform gt = g.transform;
          GameObject iconObject = gt.GetChild(0).gameObject;
          GameObject nameObject = gt.GetChild(1).gameObject;
          GameObject attributeSlot1 = gt.GetChild(2).gameObject;
          GameObject attributeSlot2 = gt.GetChild(3).gameObject;
          GameObject attributeSlot3 = gt.GetChild(4).gameObject;

          //Icon
          iconObject.GetComponent<Image>().sprite = character.icon;

          //Name
          nameObject.GetComponent<TextMeshProUGUI>().text = character.name;

          //Attribute1
          var attributes = character.characterAttributes;

          if (attributes.Count >= 2)
          {
            attributeSlot1.GetComponent<TextMeshProUGUI>().text = attributes[0].name + " " + attributes[0].curValue.ToString() + " / " + attributes[0].maxValue.ToString();
            attributeSlot2.GetComponent<TextMeshProUGUI>().text = attributes[1].name + " " + attributes[1].curValue.ToString() + " / " + attributes[1].maxValue.ToString();
            attributeSlot3.GetComponent<TextMeshProUGUI>().text = attributes[2].name + " " + attributes[2].curValue.ToString() + " / " + attributes[2].maxValue.ToString();
          }
          else
          {
            Debug.Log("The default configuration requires at least 2 attributes per character. Please add more attributes or change the configuration.");
          }

        }

      }
      else
      {
        Debug.Log("Character with id" + charId.ToString() + " was not spawned due to the lack of spawn points.");
      }
    }
  }

  //Generating skills list
  void skillGen()
  {

    //Emptying list
    FunctionDB.core.emptyWindow(actionsWindow);

    //Getting character id
    var instanceID = BattleManager.core.CurrentContext.activeCharacterId;

    //Getting skill list
    var characterInstance = BattleManager.core.findCharacterInstanceById( instanceID );
    var character = characterInstance.characterCopy;
    var skillList = character.skills;

    //Used to set focus on the first element instantiated
    bool focusSet = false;

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    foreach (int s in skillList)
    {

      //Getting skill data
      var skillIndex = FunctionDB.core.findSkillIndexById(s);
      var skill = Database.dynamic.skills[skillIndex];

      var paralyze = FunctionDB.core.findAttributeByName( instanceID, "PARALYZE" );
      bool attacksDisabled = paralyze != null && paralyze.curValue > 0f; 

      if (skill.activeSkill && !(skill.isAttack && attacksDisabled) )
      {
        //Getting function data
        var functionsToCall = skill.functionsToCall;

        //Instantiating gameObject
        GameObject t = Instantiate(optionPrefab, actionsWindow.transform);

        //Set skill text
        t.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = skill.name;

        //Getting button component
        Button b = t.GetComponent<Button>();

        //Setting focus
        if (!focusSet)
        {
          EventSystem.current.SetSelectedGameObject(t);
          focusSet = true;
        }

        //Set new listeners for each function
        //current turn points
        var curTp = BattleManager.core.turnPoints;

        //Setting listeners
        b.onClick.AddListener(delegate
        {
          var index = FunctionDB.core.findAttributeIndexByName( "MP", character );
          float currentMana = 9999f;
          //Getting attribute
          if ( index >= 0 )
          {
            characterAttribute attribute = character.characterAttributes[index];
            currentMana = attribute.curValue;
          }
          var superIndex = FunctionDB.core.findAttributeIndexByName( "SP", character );
          float superPower = 0f;
          //Getting attribute
          if ( superIndex >= 0 )
          {
            characterAttribute attribute = character.characterAttributes[superIndex];
            superPower = attribute.curValue;
          }

          if ( currentMana < skill.manaCost )
          { 
            BattleManager.core.startWarningRoutine("Insufficient mana", 2f);
          }
          else if ( superPower < skill.superCost )
          { 
            BattleManager.core.startWarningRoutine("Super skill not charged", 2f);
          }
          else if ( ( curTp - skill.turnPointCost ) < 0 )
          { 
            BattleManager.core.startWarningRoutine( "Insufficient turn points", 2f );
          }
          else
          {
              BattleManager.core.CurrentContext.functionQueue = new List<callInfo>( functionsToCall );
              BattleManager.core.CurrentContext.activeSkillId = skill.id;
              BattleManager.core.StartCoroutine(
                BattleManager.core.functionQueueCaller( BattleManager.core.CurrentContext ) );
          }
        });

        //Adding element to current action list
        curActionInfo cai = new curActionInfo();
        cai.actionObject = t;
        cai.description = skill.description;
        cai.turnPointCost = skill.turnPointCost;
        cai.manaPointCost = skill.manaCost;
        cai.superCost = skill.superCost;

        BattleManager.core.curActions.Add(cai);
      }


    }

    //Instantiating back button
    instantiateBackButton(focusSet);

  }

  public void itemGen()
  {

    //Emptying list
    FunctionDB.core.emptyWindow(actionsWindow);

    //Getting character id
    var instanceID = BattleManager.core.CurrentContext.activeCharacterId;

    //Getting character
    var characterInstance = BattleManager.core.findCharacterInstanceById( instanceID );
    var character = characterInstance.characterCopy;

    //Used for setting focus
    bool focusSet = false;

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    //For each item that the player possesses
    foreach (characterItemInfo itemInfo in character.items)
    {

      //If player has at least some of quantity of the item
      if (itemInfo.quantity > 0)
      {

        //Spawn Item

        //Item id
        var itemId = itemInfo.id;

        //Getting item
        var itemIndex = FunctionDB.core.findItemIndexById(itemId);
        var item = Database.dynamic.items[itemIndex];

        //functions to call list
        var functionsToCall = item.functionsToCall;

        //Spawning option
        GameObject t = Instantiate(optionPrefab, actionsWindow.transform);

        //Setting item text
        t.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = item.name;

        //Getting button component
        Button b = t.GetComponent<Button>();

        //Setting focus
        if (!focusSet)
        {
          EventSystem.current.SetSelectedGameObject(t);
          focusSet = true;
        }

        //current turn points
        var curTp = BattleManager.core.turnPoints;

        //Setting listeners
        b.onClick.AddListener(delegate
        {

          if ((curTp - item.turnPointCost) >= 0)
          {
            BattleManager.core.CurrentContext.functionQueue = new List<callInfo>(functionsToCall);
            BattleManager.core.StartCoroutine(BattleManager.core.functionQueueCaller(BattleManager.core.CurrentContext));
          }
          else
          {
            BattleManager.core.startWarningRoutine("Insufficient turn points", 2f);
          }

        });

        //Adding element to current action list
        curActionInfo cai = new curActionInfo();
        cai.actionObject = t;
        cai.description = item.description;
        cai.turnPointCost = item.turnPointCost;

        BattleManager.core.curActions.Add(cai);
      }


    }

    //Instantiating back button
    instantiateBackButton(focusSet);

  }

  public List<InstanceID> getValidTargets(BattleManager.BattleManagerContext context, bool genPlayerTeam, bool genEnemyTeam, int targetLimit)
  {
    
    //Creating a list of ids to generate
    List<InstanceID> toGen = new List<InstanceID>();

    //Adding players team
    if (genPlayerTeam)
    {
      toGen.AddRange(context.attackerTeam);
    }

    //Adding enemy team
    if (genEnemyTeam)
    {
      toGen.AddRange(context.defenderTeam);
    }

    //Excluding invalid characters
    for (int i = 0; i < toGen.Count; i++)
    {
      //Getting character
      var characterInstanceById = BattleManager.core.findCharacterInstanceById( toGen[i] );

      //If the character is not active, remove from list
      if (!characterInstanceById.isAlive)
      {
        toGen.RemoveAt(i);
      }
    }

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    List<InstanceID> selected = new List<InstanceID>();
    while ( targetLimit > 0 && toGen.Count > 0 )
    {
      int index = UnityEngine.Random.Range( 0, toGen.Count );
      InstanceID charId = toGen[index];
      toGen.RemoveAt( index );
      selected.Add( charId );
    }

    return selected;
  }

  //This function generates a target list
  //Note: This function does not include complex filter, please feel free to add your own if necessary
  public void targetGen(BattleManager.BattleManagerContext context, bool genPlayerTeam, bool genEnemyTeam, int targetLimit)
  {

    //Emptying list
    FunctionDB.core.emptyWindow(actionsWindow);

    //Used for setting focus
    bool focusSet = false;

    //Setting targetLimit
    context.targetLimit = targetLimit;

    //Creating a list of ids to generate
    List<InstanceID> toGen = new List<InstanceID>();

    //Adding players team
    if (genPlayerTeam)
    {
      toGen.AddRange(context.attackerTeam);
    }

    //Adding enemy team
    if (genEnemyTeam)
    {
      toGen.AddRange(context.defenderTeam);
    }

    //Excluding invalid characters
    for (int i = 0; i < toGen.Count; i++)
    {
      //Getting character
      var instanceData = BattleManager.core.findCharacterInstanceById( toGen[i] );
      //If the character is not active, remove from list
      if (!instanceData.isAlive)
      {
        toGen.RemoveAt(i);
      }
    }

    //Cleaning action target List
    context.actionTargets.Clear();

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    foreach (InstanceID charId in toGen)
    {
      //Getting character
      var characterInstance = BattleManager.core.findCharacterInstanceById( charId );
      var character = characterInstance.characterCopy;
      
      var instanceData = BattleManager.core.findCharacterInstanceById( charId );

      //Spawning option
      GameObject t = Instantiate(optionPrefab, actionsWindow.transform);

      //Setting char name
      t.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = character.name;

      //Setting focus
      if (!focusSet)
      {
        EventSystem.current.SetSelectedGameObject(t);
        focusSet = true;
      }

      //Getting button component
      Button b = t.GetComponent<Button>();

      b.onClick.AddListener(delegate
      {

        //Making sure that the character is not already in the target list
        var selectMore = context.targetLimit > context.actionTargets.Count;
        if (!context.actionTargets.Exists(x => x == charId) && selectMore)
        {
          context.actionTargets.Add(charId);
          selectMore = context.targetLimit > context.actionTargets.Count;
        }

        if (!selectMore)
        {
          mainGen();
        }

      });

      //Adding element to current action list
      curActionInfo cai = new curActionInfo();
      cai.actionObject = t;
      cai.description = character.description;
      cai.turnPointCost = -1;

      BattleManager.core.curActions.Add(cai);

    }

    instantiateFinishSelectionButton();

  }

  //Terminates character selection
  void instantiateFinishSelectionButton()
  {

    //Instatiating back button
    GameObject backButtonObject = Instantiate(optionPrefab, actionsWindow.transform);
    backButtonObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Finish selection";

    //Getting Button
    Button backButton = backButtonObject.GetComponent<Button>();

    //Setting listeners
    backButton.onClick.AddListener(delegate
    {

      //Returning to main menu
      mainGen();

      //Terminating selection process
      BattleManager.core.CurrentContext.targetLimit = BattleManager.core.CurrentContext.actionTargets.Count;

    });

    //Adding element to current action list
    curActionInfo cai = new curActionInfo();
    cai.actionObject = backButtonObject;
    cai.description = "Finish selection";
    cai.turnPointCost = -1;

    BattleManager.core.curActions.Add(cai);

  }

  //This function instantiates the back button
  void instantiateBackButton(bool focusSet)
  {

    //Instatiating back button
    GameObject backButtonObject = Instantiate(optionPrefab, actionsWindow.transform);
    backButtonObject.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = "Back";

    //Getting Button
    Button backButton = backButtonObject.GetComponent<Button>();

    //Setting listeners
    backButton.onClick.AddListener(delegate { mainGen(); });

    //Adding element to current action list
    curActionInfo cai = new curActionInfo();
    cai.actionObject = backButtonObject;
    cai.description = "Back";
    cai.turnPointCost = -1;

    BattleManager.core.curActions.Add(cai);

    //Setting focus
    if (!focusSet)
    {
      EventSystem.current.SetSelectedGameObject(backButtonObject);
      focusSet = true;
    }
  }
  
  //This function populates a list of skills to be downgraded
  public void skillConsumeGen()
  {
    BattleManager.BattleManagerContext context = BattleManager.core.CurrentContext;
    //Emptying list
    FunctionDB.core.emptyWindow(actionsWindow);

    //Used for setting focus
    bool focusSet = false;

    //Setting targetLimit
    context.targetLimit = 1;

    //Creating a list of ids to generate
    List<int> toGen = new List<int>();
 
    //Getting character id
    var characterId = BattleManager.core.CurrentContext.activeCharacterId;

    //Getting character
    var characterInstance = BattleManager.core.findCharacterInstanceById( characterId );
    var character = characterInstance.characterCopy;    toGen.AddRange( character.skills );

    //Excluding invalid characters
    for (int i = 0; i < toGen.Count; i++)
    {
      //Getting character
      var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(toGen[i])];

      // Allow passive skills
      if (!skill.activeSkill)
      {
         toGen.RemoveAt(i);
      }
    }

    //Cleaning action target List
    context.actionTargets.Clear();

    //Clearing current actions window
    BattleManager.core.curActions.Clear();

    foreach (int skillId in toGen)
    {

      //Getting character
      var skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById(skillId)];

      //Spawning option
      GameObject t = Instantiate(optionPrefab, actionsWindow.transform);

      //Setting char name
      t.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = skill.name;

      //Setting focus
      if (!focusSet)
      {
        EventSystem.current.SetSelectedGameObject(t);
        focusSet = true;
      }

      //Getting button component
      Button b = t.GetComponent<Button>();
      
      b.onClick.AddListener(delegate
      {

        //Making sure that the character is not already in the target list
        var selectMore = context.targetLimit > context.actionTargets.Count;
        if (!context.actionTargets.Exists(x => x.SkillID== skillId) && selectMore)
        {
          context.actionTargets.Add(new InstanceID(-1) { SkillID = skillId });
          selectMore = context.targetLimit > context.actionTargets.Count;
        }

        if (!selectMore)
        {
          runSacrifice();
        }

      });

      //Adding element to current action list
      curActionInfo cai = new curActionInfo();
      cai.actionObject = t;
      cai.description = skill.description;
      cai.turnPointCost = -1;

      BattleManager.core.curActions.Add(cai);

    }

    instantiateBackButton( focusSet );

  }

  void runSacrifice()
  {

    BattleManager.BattleManagerContext context = BattleManager.core.CurrentContext;

    //Getting character id
    var characterId = BattleManager.core.CurrentContext.activeCharacterId;

    //Getting character
    var characterInstance = BattleManager.core.findCharacterInstanceById( characterId );
    var character = characterInstance.characterCopy;
    
    List<callInfo> functionsToCall = new List<callInfo>();
    foreach ( var instanceID in context.actionTargets )
    {
      int skillId = instanceID.SkillID;
      //Getting character
      skill skill = Database.dynamic.skills[FunctionDB.core.findSkillIndexById( skillId )];
      context.activeSkillId = skill.id;

      for ( int i = 0; i < character.skills.Count; ++i )
      {
        if ( character.skills[i] == skillId )
        {
          character.skills[i] = skill.sacrificeReplacementId;
          functionsToCall.AddRange( skill.sacrificeActions );
        }
      }

    }
    character.skills.RemoveAll( x => x == -1 );

    context.actionTargets.Clear();
    context.actionTargets.Add( characterId );
    
    BattleManager.core.CurrentContext.functionQueue = new List<callInfo>( functionsToCall );
    BattleManager.core.StartCoroutine(
      BattleManager.core.functionQueueCaller( BattleManager.core.CurrentContext ) );

    BattleManager.core.turnPoints = 0;
  }


  //This function nullifies turn points, ending the turn
  void endTurn()
  {
    BattleMethods.core.subtractTurnPoints( BattleManager.core.CurrentContext, -1, -1);
  }

  void Awake() { if (core == null) { core = this; } }
}


//(c) Cination - Tsenkilidis Alexandros