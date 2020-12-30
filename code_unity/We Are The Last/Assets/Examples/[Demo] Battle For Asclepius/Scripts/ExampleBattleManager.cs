/* An example of a manager script to set up and manage
 * a battle. It is entirely possible to split this script
 * up into multiple ones.
 */

using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Ares.ActorComponents;
using Ares.UI;

namespace Ares.Examples {
	public class ExampleBattleManager : MonoBehaviour {
		[SerializeField, Header("Battle")] BattleRules rules;
		[SerializeField] List<Actor> friendlyActors;
		[SerializeField] List<Actor> enemyActors;
		[SerializeField] BattleDelayElement[] battleDelayElements;
		[SerializeField, Header("Dynamic Actor")] bool spawnErebus;
		[SerializeField] GameObject erebusPrefab;
		[SerializeField] AbilityData[] erebusAbilityData;
		[SerializeField] AudioClip erebusHitSound;
		[SerializeField, Header("UI References")] Text[] eventTexts;
		[SerializeField] AbilityButton[] abilityButtons;
		[SerializeField] ItemButton[] itemButtons;
		[SerializeField] TargetButton[] targetButtons;
		[SerializeField] Button itemButton;
		[SerializeField] Button backButton;
		[SerializeField] Image abilitiesMask;
		[SerializeField] Image targetsMask;
		[SerializeField] Image itemsMask;
		[SerializeField] RectTransform actorNamePanel;
		[SerializeField] Image endBattleOverlay;
		[SerializeField] Text endBattleText;
		[SerializeField, Header("UI Controls")] float eventTextScrollSpeed;
		[SerializeField] float eventTextHoldTime;
		[SerializeField] float endBattleFadeTime;
		[SerializeField] float endBattleTextGrowTime;
		[SerializeField, Header("UI Appearance")] Gradient abilityButtonTargetColors;
		[SerializeField] Color endBattleFadeColor;
		[SerializeField] Color abilityButtonAttackColor;
		[SerializeField] Color abilityButtonHealColor;
		[SerializeField] Color abilityButtonBuffColor;
		[SerializeField] Color abilityButtonEnvironmentColor;
		[SerializeField] Sprite abilityButtonAttackIcon;
		[SerializeField] Sprite abilityButtonHealIcon;
		[SerializeField] Sprite abilityButtonBuffIcon;
		[SerializeField] Sprite abilityButtonEnvironmentIcon;
		[SerializeField, Header("Audio")] AudioClip uiSuccess;
		[SerializeField] AudioClip uiFail;
		[SerializeField, Header("Environment")] Transform cloudsParent;
		[SerializeField] Light sunLight;
		[SerializeField] WindZone wind;

		AudioSource audioSource;
		float textOffsetBottom;
		float textOffsetDisplay;
		float textOffsetTop;
		int currentEventText;
		int currentEventTextMargin;
		int currentEventTextsActive;
		Text actorNameText;
		bool switchFromBackButton = false;
		bool isRotatingNamePanel = false;

		Battle battle;

		void Awake(){
			audioSource = GetComponent<AudioSource>();

			EnvironmentVariableCallbacks.RegisterCallback("Darkness", OnDarknessSet, null, OnDarknessProcess);

			float textHeight = eventTexts[0].GetPixelAdjustedRect().height;

			textOffsetBottom = -Camera.main.pixelHeight - textHeight;
			textOffsetDisplay = eventTexts[0].rectTransform.anchorMin.y;
			textOffsetTop = textHeight + (1f - textOffsetDisplay) * Camera.main.pixelHeight;

			foreach(Text eventText in eventTexts){
				eventText.rectTransform.offsetMin = new Vector2(eventText.rectTransform.offsetMin.x, textOffsetBottom);
				eventText.rectTransform.offsetMax = new Vector2(eventText.rectTransform.offsetMax.x, textOffsetBottom);
			}

			actorNameText = actorNamePanel.GetComponentInChildren<Text>();

			if(spawnErebus){
				SpawnErebus();
			}
		}

		void Start(){
			//Set up battle
			battle = new Battle(rules);

			//Set up battle delegates and events
			battle.OnActorNeedsActionInput.AddListener(ShowActionInput);
			battle.OnActorNeedsSingleTargetInput.AddListener(ShowTargetInput);
			battle.OnActorNeedsActorsTargetInput.AddListener(ShowTargetInput);
			battle.OnActorNeedsGroupTargetInput.AddListener(ShowTargetInput);

			SetUpStatusTextCallbacks(battle);
			battle.OnActorHasGivenAllNeededInput.AddListener(OnActorHasGivenAllNeededInput);

			//Set up groups, actors and actor event listeners
			BattleGroup playerGroup = battle.AddGroup("The Invaders", GetComponent<InventoryBuilder>().GenerateInventory());
			BattleGroup enemyGroup = battle.AddGroup("The Protectors");

			playerGroup.OnDefeat.AddListener(() => EndBattle(false));
			enemyGroup.OnDefeat.AddListener(() => EndBattle(true));

//			battle.OnBattleEnd.AddListener();

			foreach(Actor actor in friendlyActors){
				playerGroup.AddActor(actor, true);
				SetUpStatusTextCallbacks(actor);
			}

			foreach(Actor actor in enemyActors){
				enemyGroup.AddActor(actor, true);
				SetUpStatusTextCallbacks(actor);
			}

			//Set up possible delay elements. This can also be set from their end, by finding the
			//correct Battle instance somehow
			foreach(BattleDelayElement delayer in battleDelayElements) {
				delayer.LinkToBattle(battle);
			}

			//Finally! Let's start the battle and get it initialized
			
			battle.Start(true);
			//If we'd started the battle with `progressAutomatically = false`, we could wait a while here to open menus etc. before manually progressing
			//to the first round by calling `battle.ProgressBattle()`.
		}

		void ShowActionInput(Actor actor, ActionInput actionInput){
			VerboseLogger.Log(string.Format("[Manager] Showing action input for {0}", actor));

			StartCoroutine(CRShowActorNamePanel(actor.DisplayName));

			for(int i=0; i<actor.Abilities.Length; i++){
				Ability ability = actor.Abilities[i];
				ChainEvaluator.ActionType actionType = ability.Data.Actions[0].Action;

				if(actionType == ChainEvaluator.ActionType.Damage || actionType == ChainEvaluator.ActionType.Afflict){
					abilityButtons[i].SetAppearance(abilityButtonAttackColor, abilityButtonAttackIcon, ability.Data.DisplayName);
				}
				else if(actionType == ChainEvaluator.ActionType.Buff){
					abilityButtons[i].SetAppearance(abilityButtonBuffColor, abilityButtonBuffIcon, ability.Data.DisplayName);
				}
				else if(ability.Data.Actions[0].Action == ChainEvaluator.ActionType.Environment){
					abilityButtons[i].SetAppearance(abilityButtonEnvironmentColor, abilityButtonEnvironmentIcon, ability.Data.DisplayName);
				}
				else{
					abilityButtons[i].SetAppearance(abilityButtonHealColor, abilityButtonHealIcon, ability.Data.DisplayName);
				}

				abilityButtons[i].interactable = actionInput.ValidAbilities.Contains(ability);
				abilityButtons[i].onClick.RemoveAllListeners();
				abilityButtons[i].onClick.AddListener(() => {SelectAbility(actor, ability, actionInput.AbilitySelectCallback);});
			}

			for(int i=actor.Abilities.Length; i<abilityButtons.Length; i++){
				abilityButtons[i].SetAppearance(Color.white, null, "");
				abilityButtons[i].interactable = false;
			}

			if(!(actor.Group.Inventory is StackedInventory)){
				Debug.LogError("The example Battle Manager only works with Stacked Inventories. Please change the inventory type" +
					"on the manager object's InventoryBuilder component.");
				return;
			}

			StackedItem[] items = null;

			switch(rules.ItemComsumptionMoment){
				case BattleRules.RoundStartModeItemComsumptionMoment.OnRoundStart:
					items = ((StackedInventory)actor.Group.Inventory).GetFilteredItems(Inventory.Filter.All);
					break;
				case BattleRules.RoundStartModeItemComsumptionMoment.OnTurn:
					items = ((StackedInventory)actor.Group.Inventory).GetFilteredItems(Inventory.Filter.All);
					break;
				case BattleRules.RoundStartModeItemComsumptionMoment.OnTurnButMarkPendingOnSelect:
					items = ((StackedInventory)actor.Group.Inventory).GetFilteredItems(Inventory.Filter.ExcludePending);
					break;
			}

			itemButton.interactable = items.Length > 0;
			itemButton.onClick.RemoveAllListeners();
			itemButton.onClick.AddListener(() => {
				ShowItemInput(actor, items, actionInput);

				audioSource.clip = uiSuccess;
				audioSource.Play();
			});

			backButton.onClick.RemoveAllListeners();
			backButton.onClick.AddListener(() => {
				switchFromBackButton = true;
				StartCoroutine(CRRotateRectTransformY(backButton.transform as RectTransform, 90f, 0.2f));
				StartCoroutine(CRRotateRectTransformY(itemButton.transform as RectTransform, 0f, 0.2f, 0.2f));
				StartCoroutine(CRSwitchToActionInput(() => {ShowActionInput(actor, actionInput);}));

				audioSource.clip = uiSuccess;
				audioSource.Play();
			});

			targetsMask.gameObject.SetActive(false);
			itemsMask.gameObject.SetActive(false);
			StartCoroutine(CRTweenImageFill(abilitiesMask, 0f, 1f, 0.2f));

			if(!switchFromBackButton){
				StartCoroutine(CRRotateRectTransformY(itemButton.transform as RectTransform, 0f, 0.2f));
				StartCoroutine(CRRotateRectTransformY(actorNamePanel, 0f, 0.2f));
			}

			switchFromBackButton = false;
		}
		
		void ShowItemInput(Actor actor, StackedItem[] items, ActionInput actionInput){
			VerboseLogger.Log(string.Format("[Manager] Showing item input for {0}", actor));

			for(int i = 0; i < items.Length; i++){
				Item item = items[i].Item;

				itemButtons[i].SetLabels(items[i]);
				itemButtons[i].interactable = items[i].GetFilteredAmount(Inventory.Filter.ExcludePending) > 0;
				itemButtons[i].onClick.RemoveAllListeners();
				itemButtons[i].onClick.AddListener(() => {SelectItem(actor, item, actionInput.ItemSelectCallback);});
			}
			
			for(int i = items.Length; i < itemButtons.Length; i++){
				itemButtons[i].SetLabels((StackedItem)null);
				itemButtons[i].interactable = false;
			}

			itemButton.onClick.RemoveAllListeners();

			itemsMask.gameObject.SetActive(true);
			StartCoroutine(CRSwitchToItemInput());
		}

		void ShowTargetInput(Actor actor, TargetInputSingleActor targetInput){
			VerboseLogger.Log(string.Format("[Manager] Showing single target input for {0}", actor));

			Actor[] sortedValidTargets = SortActorsByPosition(targetInput.ValidTargets);

			for(int i = 0; i < sortedValidTargets.Length; i++){
				Actor target = sortedValidTargets[i];

				targetButtons[i].SetAppearance(abilityButtonTargetColors.Evaluate((float)target.HP / target.MaxHP), target.DisplayName);
				targetButtons[i].interactable = true;
				targetButtons[i].onClick.RemoveAllListeners();
				targetButtons[i].onClick.AddListener(delegate{SelectTarget(actor, target, targetInput.TargetSelectCallback);});
			}
			
			for(int i = sortedValidTargets.Length; i < targetButtons.Length; i++){
				targetButtons[i].SetAppearance(Color.white, "");
				targetButtons[i].interactable = false;
			}

			targetsMask.gameObject.SetActive(true);
			StartCoroutine(CRSwitchToTargetInput());
		}

		void ShowTargetInput(Actor actor, TargetInputNumActors targetInput){
			VerboseLogger.Log(string.Format("[Manager] Showing multi-target input for {0}", actor));

			int actorsToChoose = Mathf.Min(targetInput.TargetsRequired, targetInput.ValidTargets.Length);
			Actor[] chosenTargets = new Actor[actorsToChoose];
			int chooseTargetIndex = 0;

			Actor[] sortedValidTargets = SortActorsByPosition(targetInput.ValidTargets);
			
			for(int i = 0; i < sortedValidTargets.Length; i++){
				Actor target = sortedValidTargets[i];

				targetButtons[i].SetAppearance(abilityButtonTargetColors.Evaluate((float)target.HP / target.MaxHP), target.DisplayName);
				targetButtons[i].interactable = true;
				targetButtons[i].onClick.RemoveAllListeners();

				int cachedI = i;
				targetButtons[i].onClick.AddListener(() => {
					chosenTargets[chooseTargetIndex] = target;
					chooseTargetIndex++;

					targetButtons[cachedI].interactable = false;

					if(chooseTargetIndex == actorsToChoose){
						SelectTargets(actor, chosenTargets, targetInput.TargetSelectCallback);
					}
				});
				
				for(int j = sortedValidTargets.Length; j < targetButtons.Length; j++){
					targetButtons[j].SetAppearance(Color.white, "");
					targetButtons[j].interactable = false;
				}
				
				targetsMask.gameObject.SetActive(true);
				StartCoroutine(CRSwitchToTargetInput());
			}
		}

		void ShowTargetInput(Actor actor, TargetInputGroup targetInput){
			VerboseLogger.Log(string.Format("[Manager] Showing group target input for {0}", actor));

			for(int i = 0; i < targetInput.ValidTargets.Length; i++){
				BattleGroup target = targetInput.ValidTargets[i];

				float averageHealthPercentage = target.Actors.Sum(a => a.HP) / (float)target.Actors.Sum(a => a.MaxHP);

				targetButtons[i].SetAppearance(abilityButtonTargetColors.Evaluate(averageHealthPercentage), target.Name);
				targetButtons[i].interactable = true;
				targetButtons[i].onClick.RemoveAllListeners();
				targetButtons[i].onClick.AddListener(delegate{SelectTarget(actor, target, targetInput.TargetSelectCallback);});
			}

			for(int i = targetInput.ValidTargets.Length; i < targetButtons.Length; i++){
				targetButtons[i].SetAppearance(Color.white, "");
				targetButtons[i].interactable = false;
			}

			targetsMask.gameObject.SetActive(true);
			StartCoroutine(CRSwitchToTargetInput());
		}

		void SelectAbility(Actor actor, Ability ability, System.Func<Ability, bool> selectCallback){
			if(selectCallback(ability)){
				audioSource.clip = uiSuccess;

				for(int i=0; i<abilityButtons.Length; i++){
					abilityButtons[i].onClick.RemoveAllListeners();
					itemButton.onClick.RemoveAllListeners();
					abilityButtons[i].interactable = false;
					itemButton.interactable = false;
				}
					
				StartCoroutine(CRTweenImageFill(abilitiesMask, 1f, 0f, 0.2f));
			}
			else{
				audioSource.clip = uiFail;
			}

			audioSource.Play();
		}

		void SelectItem(Actor actor, Item item, System.Func<Item, bool> selectCallback){
			if(selectCallback(item)){
				audioSource.clip = uiSuccess;

				for(int i=0; i<itemButtons.Length; i++){
					itemButtons[i].onClick.RemoveAllListeners();
					itemButtons[i].interactable = false;
				}
					
				StartCoroutine(CRTweenImageFill(itemsMask, 1f, 0f, 0.2f));
			}
			else{
				audioSource.clip = uiFail;
			}

			audioSource.Play();
		}

		void SelectTarget(Actor actor, Actor target, System.Func<Actor, bool> selectCallback){
			Debug.Log(string.Format("Selected target actor {0}", target));

			HandleTargetSelectUI(selectCallback(target));
		}

		void SelectTargets(Actor actor, Actor[] targets, System.Func<Actor[], bool> selectCallback){
			Debug.Log(string.Format("Selected target actors {0}", string.Join(", ", targets.Select(a => a.DisplayName).ToArray())));

			HandleTargetSelectUI(selectCallback(targets));
		}

		void SelectTarget(Actor actor, BattleGroup target, System.Func<BattleGroup, bool> selectCallback){
			Debug.Log(string.Format("Selected target group {0}", target));

			HandleTargetSelectUI(selectCallback(target));
		}

		Actor[] SortActorsByPosition(Actor[] actors){
			System.Array.Sort(actors, (a, b) => (int)(a.transform.position.x * 10000f) - (int)(b.transform.position.x * 10000f));

			return actors;
		}

		void HandleTargetSelectUI(bool success){
			if(success){
				audioSource.clip = uiSuccess;

				for(int i=0; i<targetButtons.Length; i++){
					targetButtons[i].onClick.RemoveAllListeners();
					targetButtons[i].interactable = false;
				}

				StartCoroutine(CRTweenImageFill(targetsMask, 1f, 0f, 0.2f));
			}
			else{
				audioSource.clip = uiFail;
			}

			audioSource.Play();
		}

		void EndBattle(bool playerWon){
			battle.EndBattle(Battle.EndReason.WinLoseConditionMet);

			StartCoroutine(CREndBattle(playerWon ? "You won!\n\nHermes' staff is yours!" : "You lost!\n\nThe realm now belongs to Athena."));
		}

		void OnBattleEnd(Battle.EndReason endReason){
			if(endReason == Battle.EndReason.OutOfTurns){
				StartCoroutine(CREndBattle("The battle was a tie!\n\nYou were equally matched."));
			}
		}

		void OnActorHasGivenAllNeededInput(Actor actor){
			if(friendlyActors.Contains(actor)){
				StartCoroutine(CRHideActionBar());
			}
		}

		IEnumerator CRHideActionBar(){
			StartCoroutine(CRRotateRectTransformY(itemButton.transform as RectTransform, 90f, 0.2f));
			StartCoroutine(CRRotateRectTransformY(backButton.transform as RectTransform, 90f, 0.2f, 0.2f));
			StartCoroutine(CRTweenImageFill(abilitiesMask, abilitiesMask.fillAmount, 0f, 0.2f));
			StartCoroutine(CRTweenImageFill(targetsMask, targetsMask.fillAmount, 0f, 0.2f));

			yield return StartCoroutine(CRRotateRectTransformY(actorNamePanel, -90f, 0.2f));

			actorNameText.text = "";
		}

		IEnumerator CRShowActorNamePanel(string text){
			while(isRotatingNamePanel){
				yield return null;
			}

			isRotatingNamePanel = true;
			actorNameText.text = text;

			yield return StartCoroutine(CRRotateRectTransformY(actorNamePanel, 0f, 0.2f));

			isRotatingNamePanel = false;

		}

		IEnumerator CRSwitchToTargetInput(){
			StartCoroutine(CRTweenImageFill(abilitiesMask, abilitiesMask.fillAmount, 0f, 0.2f));
			StartCoroutine(CRTweenImageFill(itemsMask, itemsMask.fillAmount, 0f, 0.2f));
			yield return new WaitForSeconds(0.2f);
			StartCoroutine(CRTweenImageFill(targetsMask, targetsMask.fillAmount, 1f, 0.2f));
			StartCoroutine(CRRotateRectTransformY(itemButton.transform as RectTransform, 90f, 0.2f));
			StartCoroutine(CRRotateRectTransformY(backButton.transform as RectTransform, 0f, 0.2f, 0.2f));
		}

		IEnumerator CRSwitchToItemInput(){
			StartCoroutine(CRTweenImageFill(abilitiesMask, 1f, 0f, 0.2f));
			yield return new WaitForSeconds(0.2f);
			StartCoroutine(CRTweenImageFill(itemsMask, 0f, 1f, 0.2f));
			StartCoroutine(CRRotateRectTransformY(itemButton.transform as RectTransform, 90f, 0.2f));
			StartCoroutine(CRRotateRectTransformY(backButton.transform as RectTransform, 0f, 0.2f, 0.2f));
		}

		IEnumerator CRSwitchToActionInput(System.Action onComplete){
			StartCoroutine(CRTweenImageFill(itemsMask, itemsMask.fillAmount, 0f, 0.2f));
			StartCoroutine(CRTweenImageFill(targetsMask, 1f, 0f, 0.2f));
			yield return new WaitForSeconds(0.2f);
			onComplete();
		}

		IEnumerator CRTweenImageFill(Image image, float start, float end, float time){
			float startTime = Time.time;

			while(Time.time - startTime < time){
				image.fillAmount = start + (end - start) * (Time.time - startTime) / time;
				yield return null;
			}

			image.fillAmount = end;
		}

		IEnumerator CRRotateRectTransformY(RectTransform rectTransform, float end, float time, float delay = 0f){
			if(delay > 0f){
				yield return new WaitForSeconds(delay);
			}

			float startTime = Time.time;
			Quaternion startRotation = rectTransform.localRotation;
			Quaternion endRotation = Quaternion.Euler(new Vector3(startRotation.eulerAngles.x, end, startRotation.eulerAngles.z));

			while(Time.time - startTime < time){
				rectTransform.localRotation = Quaternion.Lerp(startRotation, endRotation, (Time.time - startTime) / time);
				yield return null;
			}

			rectTransform.localRotation = endRotation;
		}

		IEnumerator CREndBattle(string endText){
			endBattleText.text = endText;
			float startTime = Time.time;
			float endTime = startTime + Mathf.Max(endBattleTextGrowTime, endBattleFadeTime);
			float timeElapsed = 0f;
			float tGrow = 0f;
			float tFade = 0f;
			Color fadeStartColor = new Color(endBattleFadeColor.r, endBattleFadeColor.g, endBattleFadeColor.b, 0f);

			while(Time.time < endTime){
				timeElapsed += Time.deltaTime;
				tGrow = Mathf.SmoothStep(0f, 1f, timeElapsed / endBattleTextGrowTime);
				tFade = Mathf.Pow(timeElapsed / endBattleFadeTime, 2f);

				endBattleText.rectTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, tGrow);
				endBattleOverlay.color = Color.Lerp(fadeStartColor, endBattleFadeColor, tFade);

				yield return null;
			}
		}

		//Event text callbacks and setup
		void SetUpStatusTextCallbacks(Actor actor){
			actor.OnItemStart.AddListener((item, targets) => ShowEventText(string.Format("{0} used {1} on {2}!", actor.DisplayName, WithIndefiniteArticle(item.Data.DisplayName), string.Join(", ", targets.Select(t => t.DisplayName).ToArray()))));
			actor.OnAbilityActionMiss.AddListener((target, abilityInfo, action) => ShowEventText(string.Format("{0} attack missed!", AsPossessive(actor.DisplayName))));
			actor.OnAfflictionObtain.AddListener((affliction) => ShowEventText(string.Format("{0} has been afflicted with {1}!", actor.DisplayName, WithIndefiniteArticle(affliction.Data.DisplayName))));
			actor.OnAfflictionActionProcess.AddListener((affliction, afflictionAction) => ShowEventText(string.Format("{0} was affected by their {1}!", actor.DisplayName, affliction.Data.DisplayName)));
			actor.OnStatBuff.AddListener((stat, stage) => ShowEventText(string.Format("{0} {1} has increased!", AsPossessive(actor.DisplayName), stat.Data.DisplayName)));
			actor.OnHPDeplete.AddListener(() => ShowEventText(string.Format("{0} has been defeated!", actor.DisplayName)));
			actor.OnAbilityPreparationStart.AddListener((ability, message) => ShowEventText(message));
			actor.OnAbilityPreparationUpdate.AddListener((ability, turnsRemaining, message) => ShowEventText(message));
			actor.OnAbilityPreparationEnd.AddListener((ability, interrupted) => {if(interrupted){ShowEventText(string.Format("{0} was interrupted!!", ability.Data.DisplayName));}});
//			actor.OnAbilityRecoveryStart.AddListener((ability, message) => ShowEventText(message));
			actor.OnAbilityRecoveryUpdate.AddListener((ability, turnsRemaining, message) => ShowEventText(message));
			actor.OnAbilityRecoveryEnd.AddListener((ability, interrupted) => {if(interrupted){ShowEventText(string.Format("{0} was interrupted!!", ability.Data.DisplayName));}});
			actor.OnItemPreparationStart.AddListener((item, message) => ShowEventText(message));
			actor.OnItemPreparationUpdate.AddListener((item, turnsRemaining, message) => ShowEventText(message));
			actor.OnItemPreparationEnd.AddListener((item, interrupted) => {if(interrupted){ShowEventText(string.Format("{0} was interrupted!!", item.Data.DisplayName));}});
//			actor.OnItemRecoveryStart.AddListener((item, message) => ShowEventText(message));
			actor.OnItemRecoveryUpdate.AddListener((item, turnsRemaining, message) => ShowEventText(message));
			actor.OnItemRecoveryEnd.AddListener((item, interrupted) => {if(interrupted){ShowEventText(string.Format("{0} was interrupted!!", item.Data.DisplayName));}});

			actor.OnHPChange.AddListener((newHP) => ShowEventText(
				newHP < actor.HP ?
				string.Format("{0} took {1} damage!", actor.DisplayName, actor.HP - newHP) :
				newHP > actor.HP ?
				string.Format("{0} restored {1} health!", actor.DisplayName, newHP - actor.HP) :
				string.Format("{0} was unaffected!", actor.DisplayName)
			));
		}

		void SetUpStatusTextCallbacks(Battle battle){
			battle.OnTurnSkip.AddListener((actor, voluntarySkip) => ShowEventText(string.Format("{0} {1} their turn!", actor.DisplayName, voluntarySkip ? "skipped" : "was forced to skip")));

			foreach(BattleGroup group in battle.Groups){
				group.OnDefeat.AddListener(() => ShowEventText(string.Format("{0} were defeated!", group.Name)));
			}

			battle.OnEnvironmentVariableSet.AddListener((envVar) =>{
				switch(envVar.Data.name.ToLower()){
					case "darkness":		ShowEventText("Darkness has befallen the land!");							return;
					case "neutral zone":	ShowEventText("A neutral zone has been established!");						return;
					default:				ShowEventText(string.Format("{0} has begun!", envVar.Data.DisplayName));	return;
				}
			});

			battle.OnEnvironmentVariableUnset.AddListener((envVar) =>{
				switch(envVar.Data.name.ToLower()){
					case "darkness":		ShowEventText("Light has returned!");										return;
					case "neutral zone":	ShowEventText("The neutral zone has been lifted!");							return;
					default:				ShowEventText(string.Format("{0} has ended!", envVar.Data.DisplayName));	return;
				}
			});
		}

		void ShowEventText(string text){
			StartCoroutine(CRShowEventText(text));
		}

		IEnumerator CRShowEventText(string text){
			Text eventText = eventTexts[currentEventText];
			float goalOffset = textOffsetDisplay - eventText.GetPixelAdjustedRect().height * 1.2f * currentEventTextMargin;
			float offset;
			float speedMultiplier;

			eventText.text = text;
			eventText.rectTransform.offsetMin = new Vector2(eventText.rectTransform.offsetMin.x, textOffsetBottom);
			eventText.rectTransform.offsetMax = new Vector2(eventText.rectTransform.offsetMax.x, textOffsetBottom);

			currentEventText = (currentEventText + 1) % eventTexts.Length;
			currentEventTextMargin++;
			currentEventTextsActive++;

			while(goalOffset - eventText.rectTransform.offsetMin.y > .02f ){
				speedMultiplier = Mathf.Lerp(.03f, 1f, Mathf.Pow(Mathf.Min(0f, eventText.rectTransform.offsetMin.y - goalOffset) / textOffsetBottom, .7f));
				offset = Mathf.MoveTowards(eventText.rectTransform.offsetMin.y, goalOffset, eventTextScrollSpeed * Camera.main.pixelHeight * speedMultiplier * Time.deltaTime);

				eventText.rectTransform.offsetMin = new Vector2(eventText.rectTransform.offsetMin.x, offset);
				eventText.rectTransform.offsetMax = new Vector2(eventText.rectTransform.offsetMax.x, offset);

				yield return null;
			}

			yield return new WaitForSeconds(eventTextHoldTime);

			currentEventTextsActive--;

			if(currentEventTextsActive == 0){
				currentEventTextMargin = 0;
			}

			while(textOffsetTop - eventText.rectTransform.offsetMin.y > .02f){
				speedMultiplier = Mathf.Lerp(.03f, 1f, Mathf.Pow(Mathf.Min(0f, eventText.rectTransform.offsetMin.y - textOffsetTop) / textOffsetBottom, .7f));
				offset = Mathf.MoveTowards(eventText.rectTransform.offsetMin.y, textOffsetTop, eventTextScrollSpeed * Camera.main.pixelHeight * speedMultiplier	 * Time.deltaTime);
				eventText.rectTransform.offsetMin = new Vector2(eventText.rectTransform.offsetMin.x, offset);
				eventText.rectTransform.offsetMax = new Vector2(eventText.rectTransform.offsetMax.x, offset);

				yield return null;
			}
		}

		void SpawnErebus(){
			GameObject erebus = Instantiate(erebusPrefab);
			Actor erebusActor = erebus.AddComponent<PlayerActor>();

			//Create stats and ability arrays and initialize the Actor component first
			Ability[] erebusAbilities = erebusAbilityData.Select(a => new Ability(a)).ToArray();
			Dictionary<string, int> erebusStats = new Dictionary<string, int>(){
				{"attack", 100},
				{"defense", 100},
				{"speed", 80}
			};

			erebusActor.Init("Erebus", 100, 100, erebusStats, erebusAbilities, null, null, null);

			//Add the necessary components and set them up
			ActorAnimation erebusAnimation = erebus.AddComponent<ActorAnimation>();
			ActorAudio erebusAudio = erebus.AddComponent<ActorAudio>();

			//Initialize the component before accessing its callbacks and setting their effects
			erebusAnimation.Init(true, true, true, true);

			ActorAnimationEventElement animationCallbackTakeDamage = erebusAnimation.GetEventCallback(EventCallbackType.TakeDamage);
			ActorAnimationEventElement animationCallbackHeal = erebusAnimation.GetEventCallback(EventCallbackType.Heal);
			ActorAnimationEventElement animationCallbackDie = erebusAnimation.GetEventCallback(EventCallbackType.Die);

			animationCallbackTakeDamage.Effect.SetAsTrigger("Hit");
			animationCallbackTakeDamage.Effect.SetAsAnimationDelay(0);
			animationCallbackTakeDamage.Enabled = true;

			animationCallbackHeal.Effect.SetAsTrigger("Heal");
			animationCallbackHeal.Effect.SetAsAnimationDelay(0);
			animationCallbackHeal.ignoreEvents = EventIgnoreFlags.Abilities | EventIgnoreFlags.Items;
			animationCallbackHeal.Enabled = true;

			animationCallbackDie.Effect.SetAsTrigger("Collapse");
			animationCallbackDie.Effect.SetAsNoDelay();
			animationCallbackDie.Enabled = true;

			//Repeat for all needed components
			erebusAudio.Init(true, ActorAbilityCallbackMode.AlongsideDefault, true, ActorAbilityCallbackMode.AlongsideDefault, true, ActorAbilityCallbackMode.AlongsideDefault);

			ActorAudioEventElement audioCallbackTakeDamage = erebusAudio.GetEventCallback(EventCallbackType.TakeDamage);
			audioCallbackTakeDamage.Effect.Set(erebusHitSound, AudioPlayPosition.Target, 1f, 0f);
			audioCallbackTakeDamage.Enabled = true;

			//Almost there, let's hook up the HP bar and unparent it
			HPBar hpBar = erebus.GetComponentInChildren<HPBar>();
			Transform erebusCanvas = hpBar.transform.parent;
			Vector3 pos = erebusCanvas.position;

			hpBar.Init(erebusActor);
			erebusCanvas.SetParent(null);
			erebusCanvas.position = pos;

			//And finally, add Erebus to our friendly Actor list
			friendlyActors.Add(erebusActor);
		}
		
		//Environment callbacks
		// We are handling these here since the env. var has no knowledge of the world around it.
		// Different scenes may require different effects to trigger in this case.

		void OnDarknessSet(Battle battle, EnvironmentVariable envVar){
			ParticleSystem[] particleSystems = cloudsParent.GetComponentsInChildren<ParticleSystem>();
			foreach(ParticleSystem particleSystem in particleSystems){
				var main = particleSystem.main;
				main.startColor = new ParticleSystem.MinMaxGradient(Color.red, new Color(.63f, .415f, .415f));
			}

			StartCoroutine(CRSetDarknessColors());
			StartCoroutine(CRSetDarknessSunRotation());
		}

		IEnumerator CRSetDarknessColors(){
			Color newRendererColor = new Color(.655f, .169f, .298f, .545f);
			Color newSkyColor = new Color(.1f, .01f, .1f);
			Color oldSkyColor = Camera.main.backgroundColor;
			Color newSunColor = new Color(.455f, .192f, .572f);
			Color oldSunColor = sunLight.color;
			float oldWindRadius = wind.radius;
			Color tempColor;
			float lerpTime = 4f;
			float timeElapsed = 0f;
			float t = 0f;

			yield return new WaitForSeconds(2.5f);

			MeshRenderer[] renderers = cloudsParent.GetComponentsInChildren<MeshRenderer>();
			Color oldRendererColor = renderers[0].material.color;

			while(timeElapsed < lerpTime){
				timeElapsed += Time.deltaTime;
				t = timeElapsed / lerpTime;
				tempColor = Color.Lerp(oldRendererColor, newRendererColor, t);

				foreach(MeshRenderer renderer in renderers){
					renderer.material.color = tempColor;
				}

				Camera.main.backgroundColor = Color.Lerp(oldSkyColor, newSkyColor, t);
				sunLight.color = Color.Lerp(oldSunColor, newSunColor, t);
				wind.radius = Mathf.Lerp(oldWindRadius, 0f, t);

				yield return null;
			}
		}

		IEnumerator CRSetDarknessSunRotation(){
			Quaternion oldSunRotation = sunLight.transform.rotation;
			Quaternion newSunRotation = Quaternion.Euler(2.07f, 22.81f, 14.71f);

			float lerpTime = 3f;
			float timeElapsed = 0f;
			float t = 0f;

			yield return new WaitForSeconds(1f);

			while(timeElapsed < lerpTime){
				timeElapsed += Time.deltaTime;
				t = Mathf.SmoothStep(0f, 1f, timeElapsed / lerpTime);
				t = Mathf.Pow(t, Mathf.SmoothStep(1f, 1.2f, t));

				sunLight.transform.rotation = Quaternion.Slerp(oldSunRotation, newSunRotation, t);

				yield return null;
			}
		}
		
		void OnDarknessProcess(Battle battle, EnvironmentVariable envVar){
			StartCoroutine(CRDarknessProcess(battle, envVar));
		}

		IEnumerator CRDarknessProcess(Battle battle, EnvironmentVariable envVar){
			float lerpTime = .3f;
			float timeElapsed = 0f;
			float t = 0f;
			float flashBrightness = 13f;

			while(timeElapsed < lerpTime){
				timeElapsed += Time.deltaTime;

				sunLight.intensity = Mathf.Lerp(1f, flashBrightness, timeElapsed / lerpTime);

				yield return null;
			}

			timeElapsed = 0f;
			lerpTime = 2f;

			while(timeElapsed < lerpTime){
				timeElapsed += Time.deltaTime;
				t = timeElapsed / lerpTime;
				t = Mathf.Pow(t, 4f);

				sunLight.intensity = Mathf.Lerp(flashBrightness, 1f, timeElapsed / lerpTime);

				yield return null;
			}
		}
		
		//Utility methods
		string AsPossessive(string name){
			return name + (name[name.Length - 1].ToString().ToLower() == "s" ? "'" : "'s");
		}

		string WithIndefiniteArticle(string subject){
			return ("aeiou".IndexOf(subject[0].ToString().ToLower()) > -1 ? "an " : "a ") + subject;
		}
	}
}