using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ares.Extensions;
using Ares.Development;

namespace Ares {
	[System.Flags] public enum CleanupProperty {HelperMonoBehaviour = 1, Actors = 2, BattleEffects = 4, AudioEffectSources = 8}
	public enum DelayRequestReason {UIEvent, AnimationEvent, AbilityEvent}

	public class Battle{
		public enum EndReason {WinLoseConditionMet, OutOfTurns}
		public enum RoundState {Reset, Start, QueueingActions, ProcessingStartOfRoundProcesses, StartOfTurn, ProcessingStartOfTurnProcesses, EndOfTurn, ProcessingEndOfTurnProcesses,
			EndOfRound, ProcessingEndOfRoundProcesses, InProgress}

		enum ProgressType {BattleStart, Turn, Round, AfflictionEffect, AfflictionDuration, EnvironmentVariableCallback, EnvironmentVariableDuration, Immediate}

		//Events
		public UnityEvent OnBattleStart {get; private set;}
		public EndReasonEvent OnBattleEnd {get; private set;}
		public IntEvent OnRoundStart {get; private set;}
		public IntEvent OnRoundEnd {get; private set;}
		public ActorEvent OnTurnStart {get; private set;}
		public ActorEvent OnTurnEnd {get; private set;}
		public Actor_BoolEvent OnTurnSkip {get; private set;}
		public RoundStateEvent OnRoundStateChange {get; private set;}
		public AbilityResultsEvent OnAbilityResults {get; private set;}
		public ItemResultsEvent OnItemResults {get; private set;}
		public AfflictionResultsEvent OnAfflictionResults {get; private set;}
		public ActorEvent OnActorStartedParticipating {get; private set;}
		public ActorEvent OnActorStoppedParticipating {get; private set;}
		public Actor_ActionInputEvent OnActorNeedsActionInput {get; private set;}
		public Actor_SingleTargetInputEvent OnActorNeedsSingleTargetInput {get; private set;}
		public Actor_NumActorsTargetInputEvent OnActorNeedsActorsTargetInput {get; private set;}
		public Actor_GroupTargetInputEvent OnActorNeedsGroupTargetInput {get; private set;}
		public ActorEvent OnActorHasGivenAllNeededInput {get; private set;}
		public ActorEvent OnActorDiedBeforeMakingMove {get; private set;}
		public EnvironmentVariableEvent OnEnvironmentVariableSet {get; private set;}
		public EnvironmentVariableEvent OnEnvironmentVariableUnset {get; private set;}
		public EnvironmentVariableEvent OnEnvironmentVariableProcess {get; private set;}
		public EnvironmentVariable_IntEvent OnEnvironmentVariableDurationChange {get; private set;}
		public EnvironmentVariable_IntEvent OnEnvironmentVariableStageChange {get; private set;}

		//Getters
		static public List<Battle> ActiveBattles {get; private set;}
		static public Battle LastActiveBattle {get{return ActiveBattles != null && ActiveBattles.Count > 0 ? ActiveBattles.Last() : null;}}
		public List<BattleGroup> Groups {get; private set;} //all, including defeated
		public List<Actor> Actors {get; private set;} //all, including defeated
		public Dictionary<Actor, ActorInfo> ActorInfo {get; private set;} //all, including defeated
		public List<EnvironmentVariable> EnvironmentVariables {get; private set;}
		public List<DelayRequest> ProgressDelayRequests {get; private set;}
		public HashSet<BattleDelayElement> ProgressDelayLocks {get; private set;}
		public BattleRules Rules {get; private set;}
		public float TurnTimeLeft {get{return Mathf.Max(0f, turnTimeLeft);}}
//		public float TurnTimeLeft {get{return Mathf.Max(0f, turnTimeLeft);}}

		//Statistics
		public float Duration {get{return Time.time - startTime;}}
		public int CurrentRound {get; private set;}

		RoundState CurrentRoundState {
			get{return currentRoundState;}
			set{
				if(previousRoundState != value){
					OnRoundStateChange.Invoke(value);

					previousRoundState = currentRoundState;
					currentRoundState = value;
				}
			}
		}

		//Misc. member variables
		Ability fallbackAbility;

		//Management
		RoundState currentRoundState = RoundState.Reset;
		RoundState previousRoundState = RoundState.Reset;
		List<Actor> queuedActors; //current round only
		List<QueuedAction> queuedActions;
		int currentActorIndex = -1;
		Actor currentActor;
		bool canContinue = true;
		float startTime;
		int currentTimedProcessCounter;
		Coroutine actionSelectTimeoutCoroutine;
		float turnTimeLeft;
		bool TurnTimedOut {get{return TurnTimeLeft == 0f && Rules.TurnTimeout > 0f;}}

		//Cached values
		Dictionary<ProgressType, float> progressWaitTimes;
		Dictionary<RoundMoment, EnvironmentVariableData.ProcessingMoment> environmentVariableProcessingMoments;
		Dictionary<RoundMoment, AfflictionData.ProcessingMoment> afflictionProcessingMoments;

		public Battle(BattleRules rules){
			Rules = rules;

			if(rules.DefaultAbility != null){
				fallbackAbility = new Ability(rules.DefaultAbility);
			}

			OnBattleStart = new UnityEvent();
			OnBattleEnd = new EndReasonEvent();
			OnRoundStart = new IntEvent();
			OnRoundEnd = new IntEvent();
			OnTurnStart = new ActorEvent();
			OnTurnEnd = new ActorEvent();
			OnTurnSkip = new Actor_BoolEvent();
			OnRoundStateChange = new RoundStateEvent();
			OnAbilityResults = new AbilityResultsEvent();
			OnItemResults = new ItemResultsEvent();
			OnAfflictionResults = new AfflictionResultsEvent();

			OnActorStartedParticipating = new ActorEvent();
			OnActorStoppedParticipating = new ActorEvent();
			OnActorNeedsActionInput = new Actor_ActionInputEvent();
			OnActorNeedsSingleTargetInput = new Actor_SingleTargetInputEvent();
			OnActorNeedsActorsTargetInput = new Actor_NumActorsTargetInputEvent();
			OnActorNeedsGroupTargetInput = new Actor_GroupTargetInputEvent();
			OnActorHasGivenAllNeededInput = new ActorEvent();
			OnActorDiedBeforeMakingMove = new ActorEvent();

			OnEnvironmentVariableSet = new EnvironmentVariableEvent();
			OnEnvironmentVariableUnset = new EnvironmentVariableEvent();
			OnEnvironmentVariableProcess = new EnvironmentVariableEvent();
			OnEnvironmentVariableDurationChange = new EnvironmentVariable_IntEvent();
			OnEnvironmentVariableStageChange = new EnvironmentVariable_IntEvent();

			Groups = new List<BattleGroup>();
			ActorInfo = new Dictionary<Actor, ActorInfo>();

			Actors = new List<Actor>();
			EnvironmentVariables = new List<EnvironmentVariable>();
			queuedActions = new List<QueuedAction>();
			ProgressDelayLocks = new HashSet<BattleDelayElement>();
			ProgressDelayRequests = new List<DelayRequest>();

			progressWaitTimes = new Dictionary<ProgressType, float>{
				{ProgressType.BattleStart, rules.InitialBattleProgressDelay},
				{ProgressType.Turn, rules.TimeBetweenMoves},
				{ProgressType.Round, rules.TimeBetweenRounds},
				{ProgressType.AfflictionEffect, rules.TimeBetweenAfflictionEffects},
				{ProgressType.AfflictionDuration, rules.TimeBetweenAfflictionDurationAdjustments},
				{ProgressType.EnvironmentVariableCallback, rules.TimeBetweenEnvironmentVariableCallbacks},
				{ProgressType.EnvironmentVariableDuration, rules.TimeBetweenEnvironmentVariableDurationAdjustments},
				{ProgressType.Immediate, 0f}
			};

			environmentVariableProcessingMoments = new Dictionary<RoundMoment, EnvironmentVariableData.ProcessingMoment>{
				{RoundMoment.StartOfRound, EnvironmentVariableData.ProcessingMoment.StartOfRound},
				{RoundMoment.EndOfRound, EnvironmentVariableData.ProcessingMoment.EndOfRound},
				{RoundMoment.StartOfTurn, EnvironmentVariableData.ProcessingMoment.StartOfInstigatingActorTurn},
				{RoundMoment.EndOfTurn, EnvironmentVariableData.ProcessingMoment.EndOfInstigatingActorTurn}
			};

			afflictionProcessingMoments = new Dictionary<RoundMoment, AfflictionData.ProcessingMoment>{
				{RoundMoment.StartOfRound, AfflictionData.ProcessingMoment.StartOfRound},
				{RoundMoment.EndOfRound, AfflictionData.ProcessingMoment.EndOfRound},
				{RoundMoment.StartOfTurn, AfflictionData.ProcessingMoment.StartOfAfflictedActorTurn},
				{RoundMoment.EndOfTurn, AfflictionData.ProcessingMoment.EndOfAfflictedActorTurn}
			};
		}
	
		public BattleGroup AddGroup(string name){
			return AddGroup(name, null);
		}

		public BattleGroup AddGroup(string name, Inventory inventory){
			BattleGroup group = new BattleGroup(this, name, inventory);

			Groups.Add(group);

			return group;
		}

		public void AddActor(Actor actor, BattleGroup group, bool isParticipating){
			Debug.Assert(actor != null, "Attempting to add null Actor to a group. Please make sure any references are assigned in the Inspector.");

			Actors.Add(actor);
			ActorInfo.Add(actor, new ActorInfo(actor, group, isParticipating));
			actor.LinkToBattle(this);

			actor.OnHPDeplete.AddListener(() =>{
				OnActorHPDeplete(actor);
			});
		}

		public void Start(bool progressAutomatically = true){
			if(ActiveBattles == null) {
				ActiveBattles = new List<Battle>();
			}

			ActiveBattles.Add(this);

			VerboseLogger.Log("Battle Started!", VerboseLoggerSettings.State1Color, VerboseLogger.Emphasis.Bold);
			CurrentRound = 0;
			startTime = Time.time;
			OnBattleStart.Invoke();

			if(Rules.ProgressAutomatically){
				BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.BattleStart, ProgressBattle));
			}
		}

		public void ProgressBattle(){
			VerboseLogger.Log(string.Format("Progressing battle with state {0} at time {1}", CurrentRoundState, Time.time), VerboseLoggerSettings.UnimportantColor);

			ProgressDelayRequests.Clear();

			if(!canContinue){
				return;
			}

			if(CurrentRoundState == RoundState.Reset){
				OnRoundStart.Invoke(CurrentRound + 1);

				Groups.ForEach(g => {if(g.Inventory != null){g.Inventory.UnmarkAllPending();}});
				Actors.ForEach(a => {if(a.inventory != null){a.inventory.UnmarkAllPending();}}); //Here in case of edge cases like actor dying before use, then being revived later

				CurrentRoundState = RoundState.Start;
				BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));

				return;
			}

			if(CurrentRoundState == RoundState.Start){
				CurrentRound++;

				VerboseLogger.Log(string.Format("------- Starting round {0} -------", CurrentRound.ToString()), VerboseLoggerSettings.State1Color);
				
				queuedActors = Actors.Where(a => a.HP > 0 && ActorInfo[a].IsParticipating).ToList();

				if(Rules.ActorSortType != BattleRules.ActorSort.None){
					queuedActors.Sort(Rules.ActorOrderSorter);
				}

				foreach(Actor queuedActor in queuedActors){
					ActorInfo[queuedActor].defaultRotation = queuedActor.transform.rotation;
				}

				currentActorIndex = -1;
				currentTimedProcessCounter = -1;

				if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnRoundStart){
					CurrentRoundState = RoundState.QueueingActions;

					if(Rules.TurnTimeout > 0f){
						turnTimeLeft = Rules.TurnTimeout;
					}
				}
				else{
					CurrentRoundState = RoundState.ProcessingStartOfRoundProcesses;
				}
			}

			if(CurrentRoundState == RoundState.QueueingActions){
				currentActorIndex++;

				if(currentActorIndex == queuedActors.Count){
					if(Rules.TurnTimeout > 0f && actionSelectTimeoutCoroutine != null){
						BattleMonoBehaviour.Instance.StopCoroutine(actionSelectTimeoutCoroutine);
						actionSelectTimeoutCoroutine = null;
					}

					queuedActions = queuedActions.OrderByDescending(a => a.Priority).ToList();

					currentActorIndex = -1;
					CurrentRoundState = RoundState.ProcessingStartOfRoundProcesses;

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
				}
				else{
					currentActor = queuedActors[currentActorIndex];
					ActorInfo actorInfo = ActorInfo[currentActor];

					if(actorInfo.blockingAction != null){
						QueueActionWithTargets(actorInfo.blockingAction);

						return;
					}

					ActionInput actionInput = new ActionInput(GetValidAbilities(currentActor), GetValidItems(currentActor),
						ab => {return OnAbilitySelect(ab, QueueActionWithAbility);},
						it => {return OnItemSelect(it, QueueActionWithItem);},
						() => {return OnSkipSelect(QueueSkip);});

					if(RequiresPlayerInput(currentActor)){
						VerboseLogger.Log(string.Format("Waiting for input from {0}", currentActor.DisplayName), VerboseLoggerSettings.ActionColor, VerboseLogger.Emphasis.Italics);
						
						if(Rules.TurnTimeout > 0f){
							if(turnTimeLeft > 0f){
								if(actionSelectTimeoutCoroutine != null){
									BattleMonoBehaviour.Instance.StopCoroutine(actionSelectTimeoutCoroutine);
								}

								actionSelectTimeoutCoroutine = BattleMonoBehaviour.Instance.StartCoroutine(CRWaitForAndHandleRoundTimeout(currentActor, actionInput));
							}
							else if(turnTimeLeft <= 0f){
								HandleTurnTimeout(currentActor, actionInput);
								return;
							}
						}

						OnActorNeedsActionInput.Invoke(currentActor, actionInput);
//						else{
						//							OnActorNeedsActionInput.Invoke(currentActor, actionInput);
//						}
					}
					else{
						VerboseLogger.Log(string.Format("Waiting for decision by {0}", currentActor.DisplayName, VerboseLoggerSettings.ActionColor));

						((AIActor)currentActor).SelectAction(actionInput);
					}
				}

				return;
			}

			//Start of round handling
			if(CurrentRoundState == RoundState.ProcessingStartOfRoundProcesses){
				currentTimedProcessCounter++;

				if(currentTimedProcessCounter < Rules.TimedProcessesOrder.Length){
					DoCurrentTimedProcess(RoundMoment.StartOfRound);
					return;
				}
				else{
					currentTimedProcessCounter = -1;
					CurrentRoundState = RoundState.StartOfTurn;
				}
			}

			if(CurrentRoundState == RoundState.StartOfTurn){
				currentActorIndex++;

				Debug.Assert(queuedActors.TrueForAll(a => a != null), "One or more Actors are null at the start of round. Please make sure any references are assigned in the Inspector.");

				if(currentActorIndex < queuedActors.Count){
					currentActor = queuedActors[currentActorIndex];

					VerboseLogger.Log(string.Format("------- Starting turn for actor {0} -------", currentActor.DisplayName), VerboseLoggerSettings.State2Color);

					OnTurnStart.Invoke(currentActor);

					CurrentRoundState = RoundState.ProcessingStartOfTurnProcesses;

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));

					return;
				}
				else{
					VerboseLogger.Log(string.Format("Ending round {0}!", CurrentRound.ToString()), VerboseLoggerSettings.State1Color);

					OnRoundEnd.Invoke(CurrentRound);

					CurrentRoundState = RoundState.ProcessingEndOfRoundProcesses;

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));

					return;
				}
			}

			//Start of turn handling
			if(CurrentRoundState == RoundState.ProcessingStartOfTurnProcesses){
				currentTimedProcessCounter++;

				if(currentTimedProcessCounter < Rules.TimedProcessesOrder.Length){
					DoCurrentTimedProcess(RoundMoment.StartOfTurn);
					return;
				}
				else{
					currentTimedProcessCounter = -1;
					CurrentRoundState = RoundState.InProgress;
				}
			}

			//End of turn handling
			else if(CurrentRoundState == RoundState.EndOfTurn){
				VerboseLogger.Log(string.Format("Ending {0}'s turn!", currentActor.DisplayName), VerboseLoggerSettings.State2Color);

				OnTurnEnd.Invoke(currentActor);

				CurrentRoundState = RoundState.ProcessingEndOfTurnProcesses;
			}

			//End of round handling
			else if(CurrentRoundState == RoundState.EndOfRound){
				CurrentRoundState = RoundState.Reset;

				if(CurrentRound >= Rules.MaxRounds){
					EndBattle(EndReason.OutOfTurns);
				}
				else{
					ProgressBattle();
				}
				return;
			}

			//End of turn process handling
			if(CurrentRoundState == RoundState.ProcessingEndOfTurnProcesses){
				currentTimedProcessCounter++;

				if(currentTimedProcessCounter < Rules.TimedProcessesOrder.Length){
					DoCurrentTimedProcess(RoundMoment.EndOfTurn);
					return;
				}
				else{
					currentTimedProcessCounter = -1;
					CurrentRoundState = RoundState.StartOfTurn; //Try to start next turn
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
				}
			}

			//End of round process handling
			if(CurrentRoundState == RoundState.ProcessingEndOfRoundProcesses){
				currentTimedProcessCounter++;

				if(currentTimedProcessCounter < Rules.TimedProcessesOrder.Length){
					DoCurrentTimedProcess(RoundMoment.EndOfRound);
					return;
				}
				else{
					currentTimedProcessCounter = -1;
					CurrentRoundState = RoundState.EndOfRound;

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Round, ProgressBattle));
				}

				return;
			}

			if(CurrentRoundState == RoundState.InProgress){
				QueuedAction queuedAction = null;

				if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnRoundStart){
					queuedAction = queuedActions[0];
					queuedActions.RemoveAt(0);
					currentActor = queuedAction.Caster;
				}
				else{
					currentActor = queuedActors[currentActorIndex];
				}

				if(currentActor.HP <= 0){
					VerboseLogger.Log(currentActor + " died before they could make their move!", VerboseLoggerSettings.RegularColor);

					OnActorDiedBeforeMakingMove.Invoke(currentActor);

					CurrentRoundState = RoundState.StartOfTurn;

					if(Rules.ProgressAutomatically){
						ProgressBattle();
					}
					return;
				}

				if(queuedAction != null){
					if(queuedAction.Ability != null){
						queuedAction.UpdateTargetValidity(ActorInfo);

						if(queuedAction.Targets.Length == 0){
							HandleInvalidTargetFallback<Ability, AbilityData, AbilityAction>(Rules.InvalidAbilityTargetAction, queuedAction.Ability, queuedAction.Caster,
								(target) => {ContinueWithTarget(queuedAction.Ability, target);});
						}
						else{
							ContinueWithTargets(queuedAction.Ability, queuedAction.Targets);
						}
					}
					else if(queuedAction.Item != null){
						queuedAction.UpdateTargetValidity(ActorInfo);

						if(queuedAction.Targets.Length == 0){
							HandleInvalidTargetFallback<Item, ItemData, ItemAction>(Rules.InvalidItemTargetAction, queuedAction.Item, queuedAction.Caster,
								(target) => {ContinueWithTarget(queuedAction.Item, target);});
						}
						else{
							ContinueWithTargets(queuedAction.Item, queuedAction.Targets);
						}
					}
					else{
						VerboseLogger.Log(string.Format("{0} Skipped their turn! Moving on!", currentActor), VerboseLoggerSettings.RegularColor);
						ContinueWithoutAction();
					}
				}
				else{
					Ability[] validAbilities = GetValidAbilities(currentActor);
					Item[] validItems = GetValidItems(currentActor);
				
					if(validAbilities.Length == 0 && validItems.Length == 0){
						switch(Rules.NoValidActionsAction){
							case BattleRules.AbilityFallbackType.ContinueAsNormal:
								break;
							case BattleRules.AbilityFallbackType.SkipTurn:
								OnTurnSkip.Invoke(currentActor, false);
								BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
								return;
							case BattleRules.AbilityFallbackType.CastDefaultAbility:
								Debug.Assert(currentActor.FallbackAbility != null || fallbackAbility != null, "No valid action has been found. The Battle Rules dictate that the fallback" +
									"ability is cast. However, none was assigned to the actor, nor in the Battle Rules.");
								ContinueWithAbility(currentActor.FallbackAbility ?? fallbackAbility);
								return;
						}
					}

					ActorInfo actorInfo = ActorInfo[currentActor];

					if(actorInfo.blockingAction == null){
						ActionInput actionInput = new ActionInput(validAbilities, GetValidItems(currentActor),
							ab => {return OnAbilitySelect(ab, ContinueWithAbility);},
							it => {return OnItemSelect(it, ContinueWithItem);},
							() => {return OnSkipSelect(ContinueWithoutAction);});

						if(RequiresPlayerInput(currentActor)){ //&& !queuedAbility...
							VerboseLogger.Log(string.Format("Waiting for input from {0}", currentActor.DisplayName), VerboseLoggerSettings.ActionColor, VerboseLogger.Emphasis.Italics);
							 
							OnActorNeedsActionInput.Invoke(currentActor, actionInput);

							if(Rules.TurnTimeout > 0f){
								turnTimeLeft = Rules.TurnTimeout;
								actionSelectTimeoutCoroutine = BattleMonoBehaviour.Instance.StartCoroutine(CRWaitForAndHandleTurnTimeout(currentActor, actionInput));
							}
						}
						else{
							VerboseLogger.Log(string.Format("Waiting for decision by {0}", currentActor.DisplayName, VerboseLoggerSettings.ActionColor));

							((AIActor)currentActor).SelectAction(actionInput);
						}
					}
					else if(actorInfo.blockingAction.Ability != null){
						ContinueWithTargets(actorInfo.blockingAction.Ability, actorInfo.blockingAction.Targets);
					}
					else{
						ContinueWithTargets(actorInfo.blockingAction.Item, actorInfo.blockingAction.Targets);
					}
				}

				CurrentRoundState = RoundState.EndOfTurn;
			}
		}

		bool OnAbilitySelect(Ability ability, System.Action<Ability> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection. Mana <= 0, ability is locked, etc.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(ability);}));
			return true;
		}

		bool OnItemSelect(Item item, System.Action<Item> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection. Can't use items e.g.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(item);}));
			return true;
		}

		bool OnTargetSelect(Ability ability, Actor target, System.Action<Ability, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(ability, new Actor[]{target});}));
			return true;
		}

		bool OnTargetSelect(Ability ability, Actor[] targets, System.Action<Ability, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(ability, targets);}));
			return true;
		}

		bool OnTargetSelect(Ability ability, BattleGroup target, System.Action<Ability, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate,
				() => {successCallback(ability, target.Actors.Where(a => ability.CanUse(ActorInfo[currentActor], ActorInfo[a])).ToArray());}));
			return true;
		}
			
		bool OnTargetSelect(Item item, Actor target, System.Action<Item, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(item, new Actor[]{target});}));
			return true;
		}

		bool OnTargetSelect(Item item, Actor[] targets, System.Action<Item, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {successCallback(item, targets);}));
			return true;
		}

		bool OnTargetSelect(Item item, BattleGroup target, System.Action<Item, Actor[]> successCallback){
			//Return false if some unforseen circumstance has caused an invalid selection.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate,
				() => {successCallback(item, target.Actors.Where(a => item.CanUse(ActorInfo[currentActor], ActorInfo[a])).ToArray());}));
			return true;
		}

		bool OnSkipSelect(System.Action successCallback){
			//Return false if the actor's turn cannot be skipped.

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, successCallback));
			return true;
		}

		void DoCurrentTimedProcess(RoundMoment roundMoment){
			switch(Rules.TimedProcessesOrder[currentTimedProcessCounter]){
				case BattleRules.TimedProcess.AfflictionEffect:
					ProgressAfflictionEffectQueue(afflictionProcessingMoments[roundMoment], CreateAfflictionActorProcessingQueue(roundMoment), 0, new List<Affliction>());
					break;
				case BattleRules.TimedProcess.AfflictionDuration:
					ProgressAfflictionDurationQueue(afflictionProcessingMoments[roundMoment], CreateAfflictionActorProcessingQueue(roundMoment), 0, new List<Affliction>());
					break;
				case BattleRules.TimedProcess.EnvironmentVariableCallback:
					ProgressEnvironmentVariableCallbackQueue(environmentVariableProcessingMoments[roundMoment], new List<EnvironmentVariable>());
					break;
				case BattleRules.TimedProcess.EnvironmentVariableDuration:
					ProgressEnvironmentVariableDurationQueue(environmentVariableProcessingMoments[roundMoment], new List<EnvironmentVariable>());
					break;
				case BattleRules.TimedProcess.TemporaryBuffReduction:
					if(roundMoment == RoundMoment.StartOfTurn){
						List<TemporaryBuff> clearedBuffs = new List<TemporaryBuff>();

						foreach(TemporaryBuff buff in currentActor.TemporaryBuffs){
							buff.TurnsRemaining--;

							if(buff.TurnsRemaining == 0){
								currentActor.BuffStat(buff.Stat, -buff.Stages);
								clearedBuffs.Add(buff);
							}
						}

						foreach(TemporaryBuff buff in clearedBuffs){
							currentActor.TemporaryBuffs.Remove(buff);
						}
					}

					ProgressBattle();
					break;
			}
		}

		List<Actor> CreateAfflictionActorProcessingQueue(RoundMoment roundMoment){
			AfflictionData.ProcessingMoment processingMoment = afflictionProcessingMoments[roundMoment];

			if(processingMoment == AfflictionData.ProcessingMoment.StartOfRound || processingMoment == AfflictionData.ProcessingMoment.EndOfRound){
				return queuedActors;
			}
			else{
				return new List<Actor>(){currentActor};
			}
		}

		void ProgressAfflictionEffectQueue(AfflictionData.ProcessingMoment processingMoment, List<Actor> actorsToProcess, int currentProcessingActor,
			List<Affliction> processedAfflictions){
			//NOTE: Do not modify the actorsToProcess list. Would be "in" paramater if available.

			if(!canContinue){
				return;
			}

			for(int i = currentProcessingActor; i < actorsToProcess.Count; i++){
				Actor actor = actorsToProcess[i];
				Affliction currentAffliction = actor.Afflictions.FirstOrDefault(a => !processedAfflictions.Contains(a) && a.Data.EffectProcessingMoment == processingMoment);

				if(currentAffliction == null){
					continue;
				}

				processedAfflictions.Add(currentAffliction);

				BattleMonoBehaviour.Instance.StartCoroutine(CRProcessAffliction(actor, currentAffliction,
					() => {ProgressAfflictionEffectQueue(processingMoment, actorsToProcess, i, processedAfflictions);}));
				return;
			}

			ProgressBattle();
		}

		void ProgressAfflictionDurationQueue(AfflictionData.ProcessingMoment processingMoment, List<Actor> actorsToProcess, int currentProcessingActor,
			List<Affliction> processedAfflictions){
			//NOTE: Do not modify the actorsToProcess list. Would be "in" paramater if available.

			if(!canContinue){
				return;
			}

			for(int i = currentProcessingActor; i < actorsToProcess.Count; i++){
				Actor actor = actorsToProcess[i];
				Affliction currentAffliction = actor.Afflictions.FirstOrDefault(a => !processedAfflictions.Contains(a) && a.Data.EffectProcessingMoment == processingMoment);

				if(currentAffliction == null){
					continue;
				}

				processedAfflictions.Add(currentAffliction);

				currentAffliction.RoundsRemaining--;

				if(currentAffliction.ShouldEnd()){
					actor.Cure(currentAffliction, -1);
				}

				BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.AfflictionDuration,
					() => {ProgressAfflictionDurationQueue(processingMoment, actorsToProcess, i, processedAfflictions);}));
				return;
			}

			ProgressBattle();
		}

		void ProgressEnvironmentVariableCallbackQueue(EnvironmentVariableData.ProcessingMoment processingMoment, List<EnvironmentVariable> processedEnvironmentVariables){
			//We regrab this on every iteration just in case variables get (un)set in between calls
			EnvironmentVariable envVar = EnvironmentVariables.FirstOrDefault(v => v.Data.EffectProcessingMoment == processingMoment && !processedEnvironmentVariables.Contains(v));

			if(envVar == null){
				ProgressBattle();

				return;
			}

			OnEnvironmentVariableProcess.Invoke(envVar);
			envVar.OnProcess(this);
			EnvironmentVariableCallbacks.HandleProcess(this, envVar);

			processedEnvironmentVariables.Add(envVar);

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.EnvironmentVariableCallback,
				() => {ProgressEnvironmentVariableCallbackQueue(processingMoment, processedEnvironmentVariables);}));
		}

		void ProgressEnvironmentVariableDurationQueue(EnvironmentVariableData.ProcessingMoment processingMoment, List<EnvironmentVariable> processedEnvironmentVariables){
			//We regrab this on every iteration just in case variables get (un)set in between calls
			EnvironmentVariable envVar = EnvironmentVariables.FirstOrDefault(v => v.Data.DurationProcessingMoment == processingMoment && !processedEnvironmentVariables.Contains(v));

			if(envVar == null){
				ProgressBattle();

				return;
			}

			OnEnvironmentVariableDurationChange.Invoke(envVar, envVar.RoundsRemaining - 1);

			envVar.RoundsRemaining--;

			if(envVar.RoundsRemaining < 1){
				OnEnvironmentVariableUnset.Invoke(envVar);
				envVar.OnUnset(this);
				EnvironmentVariableCallbacks.HandleUnset(this, envVar);
				EnvironmentVariables.Remove(envVar);
			}

			processedEnvironmentVariables.Add(envVar);

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.EnvironmentVariableDuration,
				() => {ProgressEnvironmentVariableDurationQueue(processingMoment, processedEnvironmentVariables);}));
		}

		public bool RequestProgressDelay(float delay, DelayRequestReason reason){
			if(Rules.AllowWaitingFor(reason)){
				AddProgressDelay(delay);

				if(reason == DelayRequestReason.AbilityEvent){
					currentActor.AddAbilityEndDelay(delay);
				}

				return true;
			}

			return false;
		}

		public bool RequestProgressDelayLock(BattleDelayElement requestor, DelayRequestReason reason){
			if(Rules.AllowWaitingFor(reason)){
				AddProgressDelayLock(requestor);

				if(reason == DelayRequestReason.AbilityEvent){
					if(currentActorIndex < queuedActors.Count){
						currentActor.AddAbilityEndDelayLock(requestor);
					}
				}

				return true;
			}

			return false;
		}
		
		public void ReleaseProgressDelayLock(BattleDelayElement locker){
			if(ProgressDelayLocks.Remove(locker) && ProgressDelayLocks.Where(l => l == locker).Count() == 0){ //This was the final lock, allow for a final delay between turns
				VerboseLogger.Log(string.Format("Releasing lock from gameobject {0}", locker.name), VerboseLoggerSettings.UnimportantColor);

//				if(currentActorIndex < queuedActors.Count){
					currentActor.ReleaseAbilityEndDelayLock(locker);
//				}
			}
		}
		
		void AddProgressDelay(float delay){
			ProgressDelayRequests.Add(new DelayRequest(delay));
		}

		void AddProgressDelayLock(BattleDelayElement requestor){
			ProgressDelayLocks.Add(requestor);
		}

		void HandleInvalidTargetFallback<T, T2, T3>(BattleRules.TargetFallbackType fallback, T effector, Actor caster, System.Action<Actor> randomTargetAction) where T :
		BattleInteractor<T2, T3> where T2 : BattleInteractorData<T3> where T3 : ChainableAction{
			switch(fallback){
				case BattleRules.TargetFallbackType.SkipTurn:
					VerboseLogger.Log(string.Format("{0}'s target for {1} is no longer available. Skipping turn.",
						currentActor.DisplayName, effector.Data.DisplayName), VerboseLoggerSettings.RegularColor);

					OnTurnSkip.Invoke(currentActor, false);
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
					return;
				case BattleRules.TargetFallbackType.SelectRandomTarget:
					Actor[] validTargets = Actors.Where(target => effector.CanUse(ActorInfo[caster], ActorInfo[target])).ToArray();

					if(validTargets.Length == 0){
						VerboseLogger.Log(string.Format("{0}'s target for {1} is no longer available and no replacement target could be found. Skipping turn.",
							currentActor.DisplayName, effector.Data.DisplayName), VerboseLoggerSettings.RegularColor);

						OnTurnSkip.Invoke(currentActor, false);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
					}
					else{
						VerboseLogger.Log(string.Format("{0}'s target for {1} is no longer available. Continuing with random replacement target.",
							currentActor.DisplayName, effector.Data.DisplayName), VerboseLoggerSettings.RegularColor);

						randomTargetAction(validTargets[Random.Range(0, validTargets.Length)]);
					}
					break;
				case BattleRules.TargetFallbackType.CastDefaultAbility:
					Ability ability = caster.FallbackAbility ?? fallbackAbility;

					Debug.Assert(ability != null, "The chosen target is or has become invalid. The Battle Rules dictate that the fallback ability is cast." +
						"However, none was assigned to the actor, nor in the Battle Rules.");

					validTargets = GetValidTargets(caster, ability);

					if(validTargets.Length == 0){
						VerboseLogger.Log(string.Format("{0}'s target for {1} is no longer available and no fallback ability target could be found. Skipping turn.",
							currentActor.DisplayName, effector.Data.DisplayName), VerboseLoggerSettings.RegularColor);

						OnTurnSkip.Invoke(currentActor, false);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
					}
					else{
						VerboseLogger.Log(string.Format("{0}'s target for {1} is no longer available. Continuing with fallback ability {2} on random target.",
							currentActor.DisplayName, effector.Data.DisplayName, ability.Data.DisplayName), VerboseLoggerSettings.RegularColor);

						ContinueWithTarget(ability, validTargets[Random.Range(0, validTargets.Length)]);
					}
					break;
			}
		}

		void QueueActionWithAbility(Ability ability){
			Actor[] validTargets = GetValidTargets(currentActor, ability);

			if(validTargets.Length == 0){
				Debug.Assert(Rules.CanSelectAbilitiesWithNoValidTargets,
					string.Format("{0} attempted to cast {1} with no valid targets, which this battle's rules prohibit.", currentActor.DisplayName, ability.Data.DisplayName));

				switch(Rules.NoValidActionsAction){
					case BattleRules.AbilityFallbackType.ContinueAsNormal:
						queuedActions.Add(new QueuedAction(currentActor));
						ProgressBattle();
						return;
					case BattleRules.AbilityFallbackType.SkipTurn:
						queuedActions.Add(new QueuedAction(currentActor));
						ProgressBattle();
						return;
					case BattleRules.AbilityFallbackType.CastDefaultAbility:
						Ability newAbility = currentActor.FallbackAbility ?? fallbackAbility;

						Debug.AssertFormat(ability != null, "No valid targets could be found for {0}. The Battle Rules dictate that the fallback ability is cast." +
							"However, none was assigned to the actor, nor in the Battle Rules.", ability.Data.DisplayName);

						validTargets = GetValidTargets(currentActor, newAbility);

						if(validTargets.Length == 0){
							VerboseLogger.Log(string.Format("No valid targets could be found for {0} or the fallback ability. The turn will be skipped.", ability.Data.DisplayName),
								VerboseLoggerSettings.ActionColor);

							queuedActions.Add(new QueuedAction(currentActor));
							ProgressBattle();
							return;
						}

						ability = newAbility;
						break;
				}
			}

			switch(ability.Data.TargetType){
				case BattleInteractorData.TargetType.SingleActor:
					HandleTargetInput(ability, new TargetInputSingleActor(validTargets, t => {return OnTargetSelect(ability, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.NumberOfActors:
					HandleTargetInput(ability, new TargetInputNumActors(validTargets, ability.Data.NumberOfTargets,
						t => {return OnTargetSelect(ability, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.AllActorsInGroup:
					HandleTargetInput(ability, new TargetInputGroup(validTargets.Select(a => a.Group).Distinct().ToArray(),
						t => {return OnTargetSelect(ability, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.AllActors:
					QueueActionWithTargets(ability, validTargets);
					break;
			}
		}

		void QueueActionWithItem(Item item){
			Actor[] validTargets = GetValidTargets(currentActor, item);

			if(validTargets.Length == 0){
				Debug.Assert(Rules.CanSelectAbilitiesWithNoValidTargets,
					string.Format("{0} attempted to cast {1} with no valid targets, which this battle's rules prohibit.", currentActor.DisplayName, item.Data.DisplayName));

				switch(Rules.NoValidActionsAction){
					case BattleRules.AbilityFallbackType.ContinueAsNormal:
						queuedActions.Add(new QueuedAction(currentActor));
						ProgressBattle();
						return;
					case BattleRules.AbilityFallbackType.SkipTurn:
						queuedActions.Add(new QueuedAction(currentActor));
						ProgressBattle();
						return;
					case BattleRules.AbilityFallbackType.CastDefaultAbility:
						Ability ability = currentActor.FallbackAbility ?? fallbackAbility;

						Debug.AssertFormat(ability != null, "No valid targets could be found for {0}. The Battle Rules dictate that the fallback ability is cast." +
							"However, none was assigned to the actor, nor in the Battle Rules.", item.Data.DisplayName);

						validTargets = GetValidTargets(currentActor, ability);

						if(validTargets.Length > 0){
							QueueActionWithAbility(ability);
						}
						else{
							VerboseLogger.Log(string.Format("No valid targets could be found for {0} or the fallback ability. The turn will be skipped.", item.Data.DisplayName),
								VerboseLoggerSettings.ActionColor);
							
							queuedActions.Add(new QueuedAction(currentActor));
							ProgressBattle();
						}
						return;
				}
			}

			switch(item.Data.TargetType){
				case BattleInteractorData.TargetType.SingleActor:
					HandleTargetInput(item, new TargetInputSingleActor(validTargets, t => {return OnTargetSelect(item, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.NumberOfActors:
					HandleTargetInput(item, new TargetInputNumActors(validTargets, item.Data.NumberOfTargets, t => {return OnTargetSelect(item, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.AllActorsInGroup:
					HandleTargetInput(item, new TargetInputGroup(validTargets.Select(a => a.Group).Distinct().ToArray(), t => {return OnTargetSelect(item, t, QueueActionWithTargets);}));
					break;
				case BattleInteractorData.TargetType.AllActors:
					QueueActionWithTargets(item, validTargets);
					break;
			}
		}

		void QueueActionWithTargets(QueuedAction action){
			VerboseLogger.Log(string.Format("Queued {0}'s use of {1} {2}",
			                                currentActor.DisplayName, action.Ability != null ? action.Ability.Data.DisplayName : action.Item.Data.DisplayName,
			                                action.Targets.Length == 0 ? "without a target." :
			                                ("on " + string.Join(", ", action.Targets.Select(t => t.DisplayName).ToArray()))), VerboseLoggerSettings.ActionColor);

			queuedActions.Add(action);
			OnActorHasGivenAllNeededInput.Invoke(currentActor);

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
		}

		void QueueActionWithTargets(Ability ability, Actor[] targets){
			VerboseLogger.Log(string.Format("Queued {0}'s use of {1} {2}",
				currentActor.DisplayName, ability.Data.DisplayName, targets.Length == 0 ? "without a target." :
				("on " + string.Join(", ", targets.Select(t => t.DisplayName).ToArray()))), VerboseLoggerSettings.ActionColor);

			queuedActions.Add(new QueuedAction(currentActor, targets, ability));
			OnActorHasGivenAllNeededInput.Invoke(currentActor);

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
		}

		void QueueActionWithTargets(Item item, Actor[] targets){
			VerboseLogger.Log(string.Format("Queued {0}'s use of {1} {2}",
				currentActor.DisplayName, item.Data.DisplayName, targets.Length == 0 ? "without a target." :
				("on " + string.Join(", ", targets.Select(t => t.DisplayName).ToArray()))), VerboseLoggerSettings.ActionColor);

			switch(Rules.ItemComsumptionMoment){
				case BattleRules.RoundStartModeItemComsumptionMoment.OnRoundStart:
					queuedActions.Add(new QueuedAction(currentActor, targets, item, int.MaxValue));
					break;
				case BattleRules.RoundStartModeItemComsumptionMoment.OnTurn:
					queuedActions.Add(new QueuedAction(currentActor, targets, item));
					break;
				case BattleRules.RoundStartModeItemComsumptionMoment.OnTurnButMarkPendingOnSelect:
					queuedActions.Add(new QueuedAction(currentActor, targets, item));
					item.PendingUseActors.Add(currentActor);
					break;
			}

			OnActorHasGivenAllNeededInput.Invoke(currentActor);
			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
		}

		void QueueSkip(){
			VerboseLogger.Log(string.Format("{0} will be skipping their turn.", currentActor.DisplayName), VerboseLoggerSettings.ActionColor);

			queuedActions.Add(new QueuedAction(currentActor));

			OnActorHasGivenAllNeededInput.Invoke(currentActor);

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
		}

		public bool CancelLastQueuedAction(){
			if(CurrentRoundState != RoundState.QueueingActions || queuedActions.Count == 0){
				return false;
			}

			queuedActions.RemoveAt(queuedActions.Count - 1);
			currentActorIndex -= 2;
			ProgressBattle();

			return true;
		}

		public bool CancelLastQueuedPlayerInputAction(){
			if(CurrentRoundState != RoundState.QueueingActions || queuedActions.Count == 0){
				return false;
			}

			QueuedAction resetAction = queuedActions.FirstOrDefault(a => RequiresPlayerInput(a.Caster));

			if(resetAction == null){
				return false;
			}

			int resetIndex = queuedActions.IndexOf(resetAction);
			queuedActions.RemoveRange(resetIndex, queuedActions.Count - resetIndex);

			currentActorIndex = queuedActors.IndexOf(resetAction.Caster) - 1;
			ProgressBattle();

			return true;
		}

		public bool CancelAllQueuedActions(){
			if(CurrentRoundState != RoundState.QueueingActions || queuedActions.Count == 0){
				return false;
			}

			queuedActions.Clear();
			currentActorIndex = -1;
			ProgressBattle();

			return true;
		}

		void ContinueWithAbility(Ability ability){
			Actor[] validTargets = GetValidTargets(currentActor, ability);

			if(validTargets.Length == 0){
				Debug.Assert(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnRoundStart || Rules.CanSelectAbilitiesWithNoValidTargets,
					string.Format("{0} attempted to cast {1} with no valid targets, which this battle's rules prohibit.", currentActor.DisplayName, ability.Data.DisplayName));

				switch(Rules.NoValidActionsAction){
					case BattleRules.AbilityFallbackType.ContinueAsNormal:
						HandleInvalidTargetFallback<Ability, AbilityData, AbilityAction>(Rules.InvalidAbilityTargetAction, ability, currentActor,
							(target) => {ContinueWithTarget(ability, target);});
						return;
					case BattleRules.AbilityFallbackType.SkipTurn:
						OnTurnSkip.Invoke(currentActor, false);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
						return;
					case BattleRules.AbilityFallbackType.CastDefaultAbility:
						Ability newAbility = currentActor.FallbackAbility ?? fallbackAbility;

						Debug.AssertFormat(ability != null, "No valid targets could be found for {0}. The Battle Rules dictate that the fallback ability is cast." +
							"However, none was assigned to the actor, nor in the Battle Rules.", ability.Data.DisplayName);

						validTargets = GetValidTargets(currentActor, newAbility);

						if(validTargets.Length == 0){
							VerboseLogger.Log(string.Format("No valid targets could be found for {0} or the fallback ability. The turn will be skipped.", ability.Data.DisplayName),
								VerboseLoggerSettings.ActionColor);

							queuedActions.Add(new QueuedAction(currentActor));
							ProgressBattle();
							return;
						}

						ability = newAbility;
						break;
				}
			}
			else{
				switch(ability.Data.TargetType){
					case BattleInteractorData.TargetType.SingleActor:
						HandleTargetInput(ability, new TargetInputSingleActor(validTargets, t => {return OnTargetSelect(ability, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.NumberOfActors:
						HandleTargetInput(ability, new TargetInputNumActors(validTargets, ability.Data.NumberOfTargets,
							t => {return OnTargetSelect(ability, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.AllActorsInGroup:
						HandleTargetInput(ability, new TargetInputGroup(validTargets.Select(a => a.Group).Distinct().ToArray(),
							t => {return OnTargetSelect(ability, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.AllActors:
						ContinueWithTargets(ability, validTargets);
						break;
				}
			}
		}

		void ContinueWithItem(Item item){
			Actor[] validTargets = GetValidTargets(currentActor, item);

			if(validTargets.Length == 0){
				Debug.Assert(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnRoundStart || Rules.CanSelectItemsWithNoValidTargets,
					string.Format("{0} attempted to use {1} with no valid targets, which this battle's rules prohibit.", currentActor.DisplayName, item.Data.DisplayName));

				switch(Rules.NoValidActionsAction){
					case BattleRules.AbilityFallbackType.ContinueAsNormal:
						HandleInvalidTargetFallback<Item, ItemData, ItemAction>(Rules.InvalidItemTargetAction, item, currentActor,
							(target) => {ContinueWithTarget(item, target);});
						return;
					case BattleRules.AbilityFallbackType.SkipTurn:
						OnTurnSkip.Invoke(currentActor, false);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
						return;
					case BattleRules.AbilityFallbackType.CastDefaultAbility:
						Ability ability = currentActor.FallbackAbility ?? fallbackAbility;

						Debug.AssertFormat(ability != null, "No valid targets could be found for {0}. The Battle Rules dictate that the fallback ability is cast." +
							"However, none was assigned to the actor, nor in the Battle Rules.", item.Data.DisplayName);

						validTargets = GetValidTargets(currentActor, ability);

						if(validTargets.Length > 0){
							QueueActionWithAbility(ability);
						}
						else{
							VerboseLogger.Log(string.Format("No valid targets could be found for {0} or the fallback ability. The turn will be skipped.", item.Data.DisplayName),
								VerboseLoggerSettings.ActionColor);

							queuedActions.Add(new QueuedAction(currentActor));
							ProgressBattle();
						}
						return;
				}
			}
			else{
				switch(item.Data.TargetType){
					case BattleInteractorData.TargetType.SingleActor:
						HandleTargetInput(item, new TargetInputSingleActor(validTargets, t => {return OnTargetSelect(item, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.NumberOfActors:
						HandleTargetInput(item, new TargetInputNumActors(validTargets, item.Data.NumberOfTargets,
							t => {return OnTargetSelect(item, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.AllActorsInGroup:
						HandleTargetInput(item, new TargetInputGroup(validTargets.Select(a => a.Group).Distinct().ToArray(),
							t => {return OnTargetSelect(item, t, ContinueWithTargets);}));
						break;
					case BattleInteractorData.TargetType.AllActors:
						ContinueWithTargets(item, validTargets);
						break;
				}
			}
		}

		Actor[] GetValidTargets(Actor caster, Ability ability){
			return Actors.Where(target => ability.CanUse(ActorInfo[caster], ActorInfo[target])).ToArray();
		}

		Actor[] GetValidTargets(Actor caster, Item item){
			return Actors.Where(target => item.CanUse(ActorInfo[caster], ActorInfo[target])).ToArray();
		}

		void HandleAutoTargetSelect(bool success){
			if(!success){
				VerboseLogger.LogWarning(string.Format(@"The automatically selected target(s) could not be chosen.
					Please consider modifying how valid targets are selected. The turn will be skipped instead."));
			}
		}

		void HandleTargetInput(Ability ability, TargetInputSingleActor targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length == 1 && Rules.AutoSelectSingleOptionTargetForAbility){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets[0]));
			}
			else if(ability.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsSingleTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTarget(ability, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}

		void HandleTargetInput(Item item, TargetInputSingleActor targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length == 1 && Rules.AutoSelectSingleOptionTargetForItem){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets[0]));
			}
			else if(item.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsSingleTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTarget(item, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}

		void HandleTargetInput(Ability ability, TargetInputNumActors targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length <= ability.Data.NumberOfTargets && Rules.AutoSelectNoChoiceMultiTargetsForAbility){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets));
			}
			else if(ability.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsActorsTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTargets(ability, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}


		void HandleTargetInput(Item item, TargetInputNumActors targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length <= item.Data.NumberOfTargets && Rules.AutoSelectNoChoiceMultiTargetsForItem){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets));
			}
			else if(item.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsActorsTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTargets(item, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}
	
		void HandleTargetInput(Ability ability, TargetInputGroup targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length == 1 && Rules.AutoSelectSingleOptionTargetForAbility){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets[0]));
			}
			else if(ability.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsGroupTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTargets(ability, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}

		void HandleTargetInput(Item item, TargetInputGroup targetInput){
			if(TurnTimedOut){
				HandleTargetInputRandomly(targetInput);
			}
			else if(targetInput.ValidTargets.Length == 1 && Rules.AutoSelectSingleOptionTargetForItem){
				HandleAutoTargetSelect(targetInput.TargetSelectCallback(targetInput.ValidTargets[0]));
			}
			else if(item.Data.IsTargetable){
				if(RequiresPlayerInput(currentActor)){
					OnActorNeedsGroupTargetInput.Invoke(currentActor, targetInput);
				}
				else{
					((AIActor)currentActor).SelectTargets(item, targetInput);
				}
			}
			else{
				HandleTargetInputRandomly(targetInput);
			}
		}

		void HandleTargetInputRandomly(TargetInputSingleActor targetInput){
			Actor candidateTarget = targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)];

			if(!targetInput.TargetSelectCallback(candidateTarget)){ //candidate was rejected for unknown reasons
				List<Actor> remainingValidTargets = targetInput.ValidTargets.ToList();
				remainingValidTargets.Remove(candidateTarget);

				while(remainingValidTargets.Count > 0){ //keep trying
					candidateTarget = remainingValidTargets[Random.Range(0, remainingValidTargets.Count)];

					if(targetInput.TargetSelectCallback(candidateTarget)){
						return;
					}
					else{
						remainingValidTargets.Remove(candidateTarget);
					}
				}

				Debug.LogError(@"All valid targets got rejected while selecting one at random. Please consider modifying how valid targets are selected.
					The turn will be skipped instead.");
			}
		}

		void HandleTargetInputRandomly(TargetInputNumActors targetInput){
			Actor[] candidateTargets = targetInput.ValidTargets.RandomSample(Mathf.Min(targetInput.TargetsRequired, targetInput.ValidTargets.Length)).ToArray();

			if(!targetInput.TargetSelectCallback(candidateTargets)){ //candidates were rejected for unknown reasons
				Debug.LogError(@"One or more valid targets got rejected while selecting them at random. Please consider modifying how valid targets are selected.
						The turn will be skipped instead.");
			}
		}

		void HandleTargetInputRandomly(TargetInputGroup targetInput){
			BattleGroup candidateGroup = targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)];

			if(!targetInput.TargetSelectCallback(candidateGroup)){ //candidate was rejected for unknown reasons
				List<BattleGroup> remainingValidTargets = targetInput.ValidTargets.ToList();
				remainingValidTargets.Remove(candidateGroup);

				while(remainingValidTargets.Count > 0){ //keep trying
					candidateGroup = remainingValidTargets[Random.Range(0, remainingValidTargets.Count)];

					if(targetInput.TargetSelectCallback(candidateGroup)){
						return;
					}
					else{
						remainingValidTargets.Remove(candidateGroup);
					}
				}

				Debug.LogError(@"All valid target groups got rejected while selecting one at random. Please consider modifying how valid targets are selected.
						The turn will be skipped instead.");
			}
		}

		void ContinueWithTarget(Ability ability, Actor target){
			ContinueWithTargets(ability, new Actor[]{target});
		}

		void ContinueWithTarget(Item item, Actor target){
			ContinueWithTargets(item, new Actor[]{target});
		}
		
		void ContinueWithTargets(Ability ability, Actor[] targets){
			VerboseLogger.Log((targets == null || targets.Length == 0) ?
				"Continuing without a target" :
				string.Format("{0} continuing with targets {1}", currentActor, string.Join(", ", targets.Select(t => t.DisplayName).ToArray())), VerboseLoggerSettings.ActionColor);

			if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
				OnActorHasGivenAllNeededInput.Invoke(currentActor);

				if(actionSelectTimeoutCoroutine != null){
					BattleMonoBehaviour.Instance.StopCoroutine(actionSelectTimeoutCoroutine);
					actionSelectTimeoutCoroutine = null;
					turnTimeLeft = Rules.TurnTimeout;
				}
			}

			ActorInfo actorInfo = ActorInfo[currentActor];

			if(actorInfo.blockingAction == null && ability.Data.Preparation.Turns > 0){
				ability.preparationTurnsRemaining = ability.Data.Preparation.Turns;
				actorInfo.blockingAction = new QueuedAction(currentActor, targets, ability);

				currentActor.OnAbilityPreparationStart.Invoke(ability, ability.Data.Preparation.GetMessage(ability.preparationTurnsRemaining, currentActor));
				ability.OnPrepare(currentActor, actorInfo.blockingAction.Targets, ability.preparationTurnsRemaining);

				if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
					CurrentRoundState = RoundState.EndOfTurn;
				}

				BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
				return;
			}
			else if(actorInfo.blockingAction != null){

				if(ability.preparationTurnsRemaining > 0){
					int turnReduction = ability.Data.Preparation.GetTurnReduction();

					if(ability.preparationTurnsRemaining > turnReduction){
						currentActor.OnAbilityPreparationUpdate.Invoke(ability, ability.preparationTurnsRemaining - turnReduction,
						                                               ability.Data.Preparation.GetMessage(ability.preparationTurnsRemaining - turnReduction, currentActor));
						
						ability.OnPrepare(currentActor, actorInfo.blockingAction.Targets, ability.preparationTurnsRemaining - turnReduction);
					}
					else{
						currentActor.OnAbilityPreparationEnd.Invoke(ability, false);
					}

					actorInfo.UpdateBlockingAction();

					if(actorInfo.blockingAction != null){
						if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
							CurrentRoundState = RoundState.EndOfTurn;
						}

						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
						return;
					}
				}
				else if(ability.recoveryTurnsRemaining > 0){
					int turnReduction = ability.Data.Recovery.GetTurnReduction();

					if(ability.recoveryTurnsRemaining >= turnReduction){
						currentActor.OnAbilityRecoveryUpdate.Invoke(ability, ability.recoveryTurnsRemaining,
						                                            ability.Data.Recovery.GetMessage(ability.recoveryTurnsRemaining, currentActor));
					}
					else{
						currentActor.OnAbilityRecoveryEnd.Invoke(ability, false);
					}

					actorInfo.UpdateBlockingAction();

					if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
						CurrentRoundState = RoundState.EndOfTurn;
					}

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
					return;
				}
			}

			if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnRoundStart){
				Ability[] validAbilities = GetValidAbilities(currentActor);

				if(!validAbilities.Contains(ability)){ //Ability has become invalid since queueing it.
					VerboseLogger.Log(string.Format("{0} became uncastable. The turn will be skipped.", ability.Data.DisplayName), VerboseLoggerSettings.ActionColor);
					OnTurnSkip.Invoke(currentActor, false);
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));

					return;

//					switch(rules.InvalidActionFallback){ //V2
//						case BattleRules.ActionFallbackType.SelectRandomActionAndTarget:
//							//////
//							return;
//						case BattleRules.AbilityFallbackType.SkipTurn:
//							VerboseLogger.Log(string.Format("{0} became uncastable. The turn will be skipped.", ability.Data.DisplayName), VerboseLoggerSettings.ActionColor);
//							OnTurnSkip.Invoke(currentActor);
//							BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
//							return;
//						case BattleRules.AbilityFallbackType.CastDefaultAbility:
//							Ability newAbility = currentActor.FallbackAbility ?? fallbackAbility;
//							Actor[] validTargets = GetValidTargets(currentActor, newAbility);
//
//							if(validTargets.Length == 0){
//								VerboseLogger.Log(string.Format("{0} became uncastable and no valid targets could be found for the fallback ability. The turn will be skipped.",
//									ability.Data.DisplayName), VerboseLoggerSettings.ActionColor);
//
//								OnTurnSkip.Invoke(currentActor);
//								BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
//								return;
//							}
//
//							ability = newAbility;
//							HandeTargetInputRandomly();
//							break;
//					}
				}
			}

			BattleMonoBehaviour.Instance.StartCoroutine(CRPerformAbility(currentActor, ability, targets));
		}

		void ContinueWithTargets(Item item, Actor[] targets){
			VerboseLogger.Log((targets == null || targets.Length == 0) ?
				"Continuing without a target" :
				string.Format("{0} continuing with targets {1}", currentActor, string.Join(", ", targets.Select(t => t.DisplayName).ToArray())), VerboseLoggerSettings.ActionColor);

			if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
				OnActorHasGivenAllNeededInput.Invoke(currentActor);
			}

			ActorInfo actorInfo = ActorInfo[currentActor];

			if(actorInfo.blockingAction == null && item.Data.Preparation.Turns > 0){
				item.preparationTurnsRemaining = item.Data.Preparation.Turns;
				actorInfo.blockingAction = new QueuedAction(currentActor, targets, item,
				                                            Rules.ItemComsumptionMoment == BattleRules.RoundStartModeItemComsumptionMoment.OnRoundStart ? int.MaxValue : 0);

				currentActor.OnItemPreparationStart.Invoke(item, item.Data.Preparation.GetMessage(item.preparationTurnsRemaining, currentActor));

				BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
				return;
			}
			else if(actorInfo.blockingAction != null){
				if(item.preparationTurnsRemaining > 0){
					int turnReduction = item.Data.Preparation.GetTurnReduction();

					if(item.preparationTurnsRemaining > turnReduction){
						currentActor.OnItemPreparationUpdate.Invoke(item, item.preparationTurnsRemaining - turnReduction,
						                                           item.Data.Preparation.GetMessage(item.preparationTurnsRemaining - turnReduction, currentActor));
					}
					else{
						currentActor.OnItemPreparationEnd.Invoke(item, false);
					}

					actorInfo.UpdateBlockingAction();

					if(actorInfo.blockingAction != null){
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
						return;
					}
				}
				else if(item.recoveryTurnsRemaining > 0){
					int turnReduction = item.Data.Recovery.GetTurnReduction();

					if(item.recoveryTurnsRemaining > turnReduction){
						currentActor.OnItemRecoveryUpdate.Invoke(item, item.recoveryTurnsRemaining - turnReduction,
						                                            item.Data.Recovery.GetMessage(item.recoveryTurnsRemaining - turnReduction, currentActor));
					}
					else{
						currentActor.OnItemRecoveryEnd.Invoke(item, false);
					}

					actorInfo.UpdateBlockingAction();

					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
					return;
				}
			}

			BattleMonoBehaviour.Instance.StartCoroutine(CRUseItem(item.Data.MockUserAsTarget ? targets[0] : currentActor, item, targets));
		}

		void ContinueWithoutAction(){
			VerboseLogger.Log(string.Format("{0} will be skipping their turn.", currentActor.DisplayName), VerboseLoggerSettings.ActionColor);

			OnTurnSkip.Invoke(currentActor, true);

			if(Rules.SelectActorAction == BattleRules.SelectActionMoment.OnTurnStart){
				OnActorHasGivenAllNeededInput.Invoke(currentActor);
			}

			BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, ProgressBattle));
		}

		void ProcessAbilityActionEffect(Actor[] chosenAbilityTargets, Ability ability, AbilityAction action){ //non-child actions only
			List<AbilityAction> actionsToProcess = ability.GetChainActionWithChildren<AbilityAction>(ability.Data.Actions, action);

			//foreach(Actor chosenAbilityTarget in chosenAbilityTargets){
			//	Debug.Log ("CAT: " + chosenAbilityTarget.DisplayName);
			//	ability.EvaluateActionTokenFormulas(currentActor, chosenAbilityTarget);
			//}

			foreach(AbilityAction currentAction in actionsToProcess){
				foreach(Actor chosenAbilityTarget in chosenAbilityTargets){
					Actor actionTarget = chosenAbilityTarget;
				
					switch(currentAction.TargetMode){
						case AbilityAction.TargetType.Self:
							actionTarget = currentActor;
							break;
						case AbilityAction.TargetType.RandomAlly:
							var allies = Actors.Where(a => ActorInfo[a].Group == ActorInfo[currentActor].Group && a != currentActor);
							actionTarget = allies.ElementAt(Random.Range(0, allies.Count()));
							break;
						case AbilityAction.TargetType.RandomEnemy:
							var enemies = Actors.Where(a => ActorInfo[a].Group != ActorInfo[currentActor].Group && a != currentActor);
							actionTarget = enemies.ElementAt(Random.Range(0, enemies.Count()));
							break;
					}

					int power = Mathf.RoundToInt(ability.EvaluatePower(currentActor, chosenAbilityTarget, actionTarget, currentAction));
					int special = Mathf.RoundToInt(ability.EvaluateSpecial(currentActor, chosenAbilityTarget, actionTarget, currentAction));
					int result = ProcessChainAction(ability, currentAction, currentActor, actionTarget, power, special);

					ability.SetActionResult(currentAction, chosenAbilityTarget, result);
				}
			}
		}

		void ProcessItemActionEffect(Actor[] chosenItemTargets, Item item, ItemAction action){ //non-child actions only
			List<ItemAction> actionsToProcess = item.GetChainActionWithChildren<ItemAction>(item.Data.Actions, action);

			foreach(ItemAction currentAction in actionsToProcess){
				foreach(Actor chosenItemTarget in chosenItemTargets){
					Actor actionTarget = chosenItemTarget;

					switch(currentAction.TargetMode){
						case AbilityAction.TargetType.Self:
							actionTarget = currentActor;
							break;
						case AbilityAction.TargetType.RandomAlly:
							var allies = Actors.Where(a => ActorInfo[a].Group == ActorInfo[currentActor].Group && a != currentActor);
							actionTarget = allies.ElementAt(Random.Range(0, allies.Count()));
							break;
						case AbilityAction.TargetType.RandomEnemy:
							var enemies = Actors.Where(a => ActorInfo[a].Group != ActorInfo[currentActor].Group && a != currentActor);
							actionTarget = enemies.ElementAt(Random.Range(0, enemies.Count()));
							break;
					}

					int power = Mathf.RoundToInt(item.EvaluatePower(currentActor, chosenItemTarget, actionTarget, currentAction));
					int special = Mathf.RoundToInt(item.EvaluateSpecial(currentActor, chosenItemTarget, actionTarget, currentAction));
					int result = ProcessChainAction(item, currentAction, currentActor, actionTarget, power, special);

					item.SetActionResult(currentAction, chosenItemTarget, result);
				}
			}
		}

		void ProcessAfflictionActionEffect(Actor target, Affliction affliction, AfflictionAction action){ //non-child actions only
			List<AfflictionAction> actionsToProcess = affliction.GetChainActionWithChildren<AfflictionAction>(affliction.Data.Actions, action);
			
			foreach(AfflictionAction currentAction in actionsToProcess){
				int power = Mathf.RoundToInt(affliction.EvaluatePower(target, currentAction));
				int result = ProcessChainAction(affliction, currentAction, affliction.Afflicter, currentAction.TargetMode == AfflictionAction.TargetType.Afflicted ? target : affliction.Afflicter, power, -1);

				affliction.SetActionResult(currentAction, target, result);
			}
		}
		
		void EndAbilityActionEffect(Actor[] targets, Ability ability, AbilityAction action, bool hitAtLeastOneTarget){
			if(ability.Data.TurnTowardsTarget && Rules.ActorCanFaceTarget != BattleRules.ActorFacingMoment.Never){
				BattleMonoBehaviour.Instance.StartCoroutine(CRRestoreActorRotation(currentActor));
			}

			if(hitAtLeastOneTarget){
				if(action == ability.Data.Actions.Where(a => !a.IsChildEffect).Last()){
					currentActor.OnAbilityEnd.Invoke(ability, targets);

					if(Rules.ProgressAutomatically){
						VerboseLogger.Log("Progressing battle automatically after ability", VerboseLoggerSettings.RegularColor);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, ProgressBattle));
					}
				}
			}
		}

		void EndItemActionEffect(Actor[] targets, Item item, ItemAction action){
			if(item.Data.TurnTowardsTarget && Rules.ActorCanFaceTarget != BattleRules.ActorFacingMoment.Never){
				BattleMonoBehaviour.Instance.StartCoroutine(CRRestoreActorRotation(currentActor));
			}

			if(action == item.Data.Actions.Where(a => !a.IsChildEffect).Last()){
				currentActor.OnItemEnd.Invoke(item, targets);

				if(Rules.ProgressAutomatically){
					VerboseLogger.Log("Progressing battle automatically after item", VerboseLoggerSettings.RegularColor);
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, ProgressBattle));
				}
			}
		}

		void EndAfflictionActionEffect(Actor actor, Affliction affliction, AfflictionAction action, bool hitAtLeastOneTarget){//, System.Action progressAction){
			if(hitAtLeastOneTarget && action == affliction.Data.Actions.Where(a => !a.IsChildEffect).Last()){
				actor.OnAfflictionEnd.Invoke(affliction);

				if(Rules.ProgressAutomatically){
					VerboseLogger.Log("Progressing battle automatically after affliction", VerboseLoggerSettings.RegularColor);
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, ProgressBattle)); //
				}
			}
		}

		int ProcessChainAction(object evaluater, ChainableAction action, Actor caster, Actor target, int power, int special){
			foreach(EnvironmentVariable envVar in EnvironmentVariables){
				power = Mathf.RoundToInt(envVar.Filter(evaluater, action, caster, target, power));
			}

			ActorInfo casterActorInfo = ActorInfo[caster];

			InterruptBlockerIfNeeded(casterActorInfo, null, BattleInteractorData.RecoveryInterrupt.OnTargetHit);

			switch(action.Action){
				case ChainEvaluator.ActionType.Damage:
					ActorInfo targetActorInfo = ActorInfo[target];
					InterruptBlockerIfNeeded(casterActorInfo, null, BattleInteractorData.RecoveryInterrupt.OnTargetDamage);
					InterruptBlockerIfNeeded(targetActorInfo, BattleInteractorData.PreparationInterrupt.OnDamage, BattleInteractorData.RecoveryInterrupt.OnDamage);

					int damageDealt = target.TakeDamage(power);

					if(target.HP == 0){
						InterruptBlockerIfNeeded(casterActorInfo, null, BattleInteractorData.RecoveryInterrupt.OnTargetDeath);
						InterruptBlockerIfNeeded(targetActorInfo, BattleInteractorData.PreparationInterrupt.OnDeath, BattleInteractorData.RecoveryInterrupt.OnDeath);
					}

					return damageDealt;
				case ChainEvaluator.ActionType.Heal:
					return target.Heal(power);
				case ChainEvaluator.ActionType.Buff:
					if(special > 0){
						target.TemporaryBuffs.Add(new TemporaryBuff(action.Stat, power, special));
					}

					return target.BuffStat(action.Stat, power);
				case ChainEvaluator.ActionType.Cure:
					return target.Cure(action.Affliction, power);
				case ChainEvaluator.ActionType.Environment:
					return ModifyEnvironmentVariable(action.EnvironmentVariable, action.EnvironmentVariableSetMode, power, caster);
				case ChainEvaluator.ActionType.Afflict:
					return (target.HP <= 0 && !action.Affliction.CanAfflictDefeatedActors) ? 0 : target.Afflict(action.Affliction, power, CurrentRound, caster);
			}

			return -1;
		}

		int ModifyEnvironmentVariable(EnvironmentVariableData data, ChainableAction.EnvironmentVariableSetType setType, int stage, Actor caster){
			EnvironmentVariable envVar = EnvironmentVariables.FirstOrDefault(v => v.Data == data);

			if(envVar == null){
				if(setType == ChainableAction.EnvironmentVariableSetType.Set){
					envVar = new EnvironmentVariable(data, stage);

					OnEnvironmentVariableSet.Invoke(envVar);
					envVar.OnSet(this, caster);
					EnvironmentVariables.Add(envVar);
					EnvironmentVariableCallbacks.HandleSet(this, envVar);

					return stage;
				}

				return 0;
			}
			else{
				if(setType == ChainableAction.EnvironmentVariableSetType.Set){
					switch(envVar.Data.DoubleSetDurationBehaviour){
						case DoubleSetDurationAction.IncreaseByOne:
							OnEnvironmentVariableDurationChange.Invoke(envVar, Mathf.Min(envVar.RoundsRemaining + 1, envVar.Data.MaxDuration));
							envVar.RoundsRemaining++;
							break;
						case DoubleSetDurationAction.ResetToStart:
							OnEnvironmentVariableDurationChange.Invoke(envVar, envVar.TotalDuration);
							envVar.RoundsRemaining = envVar.TotalDuration;
							break;
					}

					switch(envVar.Data.DoubleSetStageBehaviour){
						case DoubleSetStageAction.IncreaseByOne:
							OnEnvironmentVariableStageChange.Invoke(envVar, Mathf.Min(envVar.Stage + 1, envVar.Data.MaxStage));
							envVar.Stage++;
							return 1;
						case DoubleSetStageAction.ModifyByValue:
							OnEnvironmentVariableStageChange.Invoke(envVar, Mathf.Clamp(envVar.Stage + stage, envVar.Data.MinStage, envVar.Data.MaxStage));
							envVar.Stage += stage;
							return stage;
						case DoubleSetStageAction.SetToValue:
							OnEnvironmentVariableStageChange.Invoke(envVar, Mathf.Clamp(stage, envVar.Data.MinStage, envVar.Data.MaxStage));
							envVar.Stage = stage;
							return stage - envVar.Stage;
					}

					return 0;
				}
				else{
					if(EnvironmentVariables.Contains(envVar)){
						OnEnvironmentVariableUnset.Invoke(envVar);
						envVar.OnUnset(this);
						EnvironmentVariableCallbacks.HandleUnset(this, envVar);
						EnvironmentVariables.Remove(envVar);

						return 1;
					}

					return 0;
				}
			}
		}

		void InterruptBlockerIfNeeded(ActorInfo actorInfo, BattleInteractorData.PreparationInterrupt? preparationInterrupt, BattleInteractorData.RecoveryInterrupt? recoveryInterrupt){
			if(actorInfo.blockingAction == null){
				return;
			}

			if(actorInfo.blockingAction.Ability != null){
				Ability ability = actorInfo.blockingAction.Ability;
				if(ability.preparationTurnsRemaining > 0 && preparationInterrupt.HasValue && (ability.Data.Preparation.Interrupt & preparationInterrupt) == preparationInterrupt){
					actorInfo.Actor.OnAbilityPreparationEnd.Invoke(ability, true);
					actorInfo.blockingAction = null;
				}
				else if(ability.recoveryTurnsRemaining > 0 && recoveryInterrupt.HasValue && (ability.Data.Recovery.Interrupt & recoveryInterrupt) == recoveryInterrupt){
					actorInfo.Actor.OnAbilityRecoveryEnd.Invoke(ability, true);
					actorInfo.blockingAction = null;

					QueuedAction recoveryAction = queuedActions.FirstOrDefault(q => q.Caster == actorInfo.Actor);

					if(recoveryAction != null){
						recoveryAction.Clear();
					}
				}
			}
			else if(actorInfo.blockingAction.Item != null){
				Item item = actorInfo.blockingAction.Item;
				if(item.preparationTurnsRemaining > 0 && preparationInterrupt.HasValue && (item.Data.Preparation.Interrupt & preparationInterrupt) == preparationInterrupt){
					actorInfo.Actor.OnItemPreparationEnd.Invoke(item, true);
					actorInfo.blockingAction = null;
				}
				else if(item.recoveryTurnsRemaining > 0 && recoveryInterrupt.HasValue && (item.Data.Recovery.Interrupt & recoveryInterrupt) == recoveryInterrupt){
					actorInfo.Actor.OnItemRecoveryEnd.Invoke(item, true);
					actorInfo.blockingAction = null;

					QueuedAction recoveryAction = queuedActions.FirstOrDefault(q => q.Caster == actorInfo.Actor);

					if(recoveryAction != null){
						recoveryAction.Clear();
					}
				}
			}
		}

		bool RequiresPlayerInput(Actor actor){
			System.Type actorType = actor.GetType();
			return actorType == typeof(PlayerActor) || actorType.IsSubclassOf(typeof(PlayerActor));
		}

		public void SetParticipation(Actor actor, bool isParticipating){
			ActorInfo actorInfo = ActorInfo[actor];
			
			if(actorInfo.IsParticipating && !isParticipating){
				OnActorStoppedParticipating.Invoke(actorInfo.Actor);
				actorInfo.IsParticipating = isParticipating;
			}
			else if(!actorInfo.IsParticipating && isParticipating){
				OnActorStartedParticipating.Invoke(actorInfo.Actor);
				actorInfo.IsParticipating = isParticipating;
			}
		}

		Ability[] GetValidAbilities(Actor actor){
			var validAbilities = Rules.CanSelectAbilitiesWithNoValidTargets ?
								 actor.Abilities.Where(ab => ab.Enabled) :
								 actor.Abilities.Where(ab => ab.Enabled && Actors.Any(ac => ab.CanUse(ActorInfo[actor], ActorInfo[ac])));

			foreach(EnvironmentVariable envVar in EnvironmentVariables){
				validAbilities = envVar.FilterBlockedAbilities(validAbilities, actor);
			}

			/* It's likely you'll want to extend this to account for game-specific rules, like
			 * max ability uses or mana cost e.g. You can do so here in any way you like by
			 * modifying the validAbilities collection.
			 * 
			 * For example, a simple mana filter might look like the following:
			 *     validAbilities = validAbilities.Where(ability => actor.Mana >= ability.ManaCost);
			*/

			return validAbilities.ToArray();
		}

		Item[] GetValidItems(Actor actor){
			IEnumerable<Item> validItems;

			if(actor.inventory is LinearInventory){
				validItems = ((LinearInventory)actor.inventory).Items;
			}
			else if(actor.inventory is StackedInventory){
				validItems = ((StackedInventory)actor.inventory).Items.Select(si => si.Item);
			}
			else{
				validItems = new Item[0];
			}

			if(actor.Group.Inventory is LinearInventory){
				validItems = validItems.Concat(((LinearInventory)actor.Group.Inventory).Items);
			}
			else if(actor.Group.Inventory is StackedInventory){
				validItems = validItems.Concat(((StackedInventory)actor.Group.Inventory).Items.Select(si => si.Item));
			}

			if(!Rules.CanSelectItemsWithNoValidTargets){
				validItems = validItems.Where(i => Actors.Any(ac => i.CanUse(ActorInfo[actor], ActorInfo[ac])));
			}

			foreach(EnvironmentVariable envVar in EnvironmentVariables){
				validItems = envVar.FilterBlockedItems(validItems, actor);
			}

			/* It's likely you'll want to extend this to account for game-specific rules, like
			 * item locks e.g. You can do so here in any way you like by modifying the validItems collection.
			 * 
			 * For example, a simple lock filter might look like the following:
			 *     validItems = validItems.Where(item => actor.CanUseItems);
			*/

			return validItems.ToArray();
		}

		void OnActorHPDeplete(Actor actor){
			foreach(Actor other in Actors){
				other.Afflictions.RemoveWhere(a => a.Data.CureOnAfflicterDeath && a.Afflicter == actor);
			}
		}

		public void EndBattle(EndReason reason){
			VerboseLogger.Log("Ending battle!", VerboseLoggerSettings.State1Color, VerboseLogger.Emphasis.Bold);

			OnBattleEnd.Invoke(reason);

			canContinue = false;
			ActiveBattles.Remove(this);

			Cleanup(Rules.CleanupProperties, Rules.CleanupDelay);
		}

		public void Cleanup(CleanupProperty propertyFlags, float delay){
			BattleMonoBehaviour.Instance.StartCoroutine(CRCleanup(propertyFlags, delay));
		}

		IEnumerator CRCleanup(CleanupProperty propertyFlags, float delay){
			yield return new WaitForSeconds(delay);

			if((propertyFlags & CleanupProperty.HelperMonoBehaviour) > 0){
				Object.Destroy(BattleMonoBehaviour.Instance.gameObject);
			}

			if((propertyFlags & CleanupProperty.Actors) > 0){
				foreach(Actor actor in Actors){
					Object.Destroy(actor.gameObject);
				}
			}

			if((propertyFlags & CleanupProperty.BattleEffects) > 0){
				foreach(InstantiationEffectInstance effect in InstantiationEffectInstance.ActiveEffects){
					Object.Destroy(effect.gameObject);
				}
			}

			if((propertyFlags & CleanupProperty.AudioEffectSources) > 0){
				AudioEffect.DestroyAll();
			}
		}

		IEnumerator CRRotateTowardsTargets(Actor actor, Actor[] targets){
			Vector3 targetPos = Vector3.zero;
			int rotationTargets = 0;

			for(int i = 0; i < targets.Length; i++){
				if(targets[i] != actor){
					targetPos += targets[i].transform.position;
					rotationTargets++;
				}
			}

			if(rotationTargets == 0){
				yield break;
			}

			targetPos /= rotationTargets;

			Quaternion lookAtRotation = Quaternion.LookRotation(targetPos - actor.transform.position);
			Quaternion targetRotation = Quaternion.Euler(actor.transform.rotation.x, lookAtRotation.eulerAngles.y, actor.transform.rotation.z);
			float deltaAngle = Quaternion.Angle(actor.transform.rotation, targetRotation);

			RequestProgressDelay(deltaAngle / Rules.FaceTargetSpeed, DelayRequestReason.AnimationEvent);

			while(actor.transform.rotation != targetRotation){
				actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, targetRotation, Rules.FaceTargetSpeed * Time.deltaTime);
				yield return null;
			}
		}

		IEnumerator CRPerformAbility(Actor actor, Ability ability, Actor[] targets){
			Quaternion startRotation = actor.transform.rotation;
			bool wantsToRotate = ability.Data.TurnTowardsTarget;

			if(wantsToRotate && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.Always){
				yield return BattleMonoBehaviour.Instance.StartCoroutine(CRRotateTowardsTargets(actor, targets));
			}

			yield return null;

			actor.OnAbilityStart.Invoke(ability, targets);
			ability.OnUse(actor, targets);
			
			ability.PrepareForChainEvaluation(actor, targets);

			List<Actor> remainingTargets = targets.ToList();
			AbilityResults abilityResults = new AbilityResults(ability);

			foreach(AbilityAction action in ability.Data.Actions.Where(a => !a.IsChildEffect)){
				BattleActionResults actionResults = abilityResults.CreateResult(action);

				for(int i = 0; i < remainingTargets.Count; i++){
					Actor target = remainingTargets[i];
					bool breakChain = false;
					BattleInteractorData.HitStatus hitStatus = target != null ?
						target.PerformHitTest(actor, ability.Data, action) :
						(Random.value <= action.HitChance ? BattleInteractorData.HitStatus.Hit : BattleInteractorData.HitStatus.Evade);

					switch(hitStatus){
						case BattleInteractorData.HitStatus.Hit:
							if(action == ability.Data.Actions[0]){
								if(wantsToRotate && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.OnAttackHit){
									yield return BattleMonoBehaviour.Instance.StartCoroutine(CRRotateTowardsTargets(actor, targets));
								}

								float baseDelay = ability.BaseDuration;

								if(baseDelay > 0){
									yield return new WaitForSeconds(baseDelay);
								}
							}

							actionResults.hitTargets.Add(target);
							break;
						default:
							actionResults.missedActions.Add(new MissedAction(target, hitStatus));

							breakChain = action.BreaksChainOnMiss || action == ability.Data.Actions.Last(a => !a.IsChildEffect);
							break;
					}

					if(breakChain){
						remainingTargets.RemoveAt(i);
						i--;
					}

					if(remainingTargets.Count == 0){
						EndPerformAbility(actor, abilityResults);

						if(ability.Data.TurnTowardsTarget && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.Always){
							while(actor.transform.rotation != startRotation){
								actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, startRotation, Rules.FaceTargetSpeed * Time.deltaTime);
								yield return null;
							}
						}

						if(Rules.ProgressAutomatically){
							VerboseLogger.Log("Progressing battle automatically after failed ability action", VerboseLoggerSettings.RegularColor);
							BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, ProgressBattle));
						}

						break;
					}
				}
			}

			if(remainingTargets.Count > 0){
				EndPerformAbility(actor, abilityResults);
			}

			if(ability.Data.Recovery.Turns > 0){
				bool hitAtLeastOneTarget = false;
				bool damagedAtLeastOneTarget = false;

				foreach(KeyValuePair<AbilityAction, BattleActionResults> result in abilityResults.actionResults){
					if(result.Value.hitTargets.Count > 0){
						hitAtLeastOneTarget = true;

						if(result.Key.Action == ChainEvaluator.ActionType.Damage){
							damagedAtLeastOneTarget = true;
						}
					}
				}

				if(!((ability.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetHit) == BattleInteractorData.RecoveryInterrupt.OnTargetHit && hitAtLeastOneTarget) &&
				   !((ability.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetDamage) == BattleInteractorData.RecoveryInterrupt.OnTargetDamage && damagedAtLeastOneTarget) &&
				   !((ability.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetMiss) == BattleInteractorData.RecoveryInterrupt.OnTargetMiss && !hitAtLeastOneTarget)){
					ability.recoveryTurnsRemaining = ability.Data.Recovery.Turns;
					ActorInfo[actor].blockingAction = new QueuedAction(actor, targets, ability);
					actor.OnAbilityRecoveryStart.Invoke(ability);
				}
			}
		}

		void EndPerformAbility(Actor actor, AbilityResults abilityResults){
			OnAbilityResults.Invoke(actor, abilityResults);
			ProcessAbilityEnd(actor, abilityResults);
		}

		void ProcessAbilityEnd(Actor actor, AbilityResults abilityResults){ //recursive
			KeyValuePair<AbilityAction, BattleActionResults> currentResultKVP = abilityResults.actionResults.First();
			abilityResults.actionResults.Remove(currentResultKVP.Key);

			if(abilityResults.actionResults.Count > 0){
				actor.OnAbilityActionEnd.AddOneTimeListener<Actor[], Ability, AbilityAction>((ac, ab, aa) => {
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {ProcessAbilityEnd(actor, abilityResults);}));
				});
			}

			if(currentResultKVP.Value.hitTargets.Count > 0){
				actor.OnAbilityActionEnd.AddOneTimeListener<Actor[], Ability, AbilityAction>((ac, ab, aa) => {EndAbilityActionEffect(ac, ab, aa, true);});
				actor.OnAbilityActionProcess.AddOneTimeListener<Actor[], Ability, AbilityAction>(ProcessAbilityActionEffect);
				actor.ConfirmAbilityActionSuccess(currentResultKVP.Value.hitTargets.ToArray(), abilityResults.ability, currentResultKVP.Key);
			}
			else{
				actor.OnAbilityActionEnd.AddOneTimeListener<Actor[], Ability, AbilityAction>((ac, ab, aa) => {EndAbilityActionEffect(ac, ab, aa, false);});
			}

			if(currentResultKVP.Value.missedActions.Count > 0){
				actor.ConfirmAbilityActionFail(currentResultKVP.Value.missedActions.Select(a => a.target).ToArray(), abilityResults.ability,
					currentResultKVP.Value.missedActions.Select(a => a.hitStatus).ToArray(), currentResultKVP.Key, currentResultKVP.Value.missedActions.Count == 0);

				foreach(MissedAction missedAction in currentResultKVP.Value.missedActions){
					if(missedAction.hitStatus == BattleInteractorData.HitStatus.Evade && missedAction.target != null){
						missedAction.target.OnAbilityActionAvoid.Invoke(actor, abilityResults.ability, currentResultKVP.Key);
					}
				}
			}
		}

		IEnumerator CRWaitForAndHandleTurnTimeout(Actor actor, ActionInput actionInput){
			while(turnTimeLeft > 0f){
				turnTimeLeft -= Time.deltaTime;
				yield return null;
			}

			HandleTurnTimeout(actor, actionInput);
		}

		IEnumerator CRWaitForAndHandleRoundTimeout(Actor actor, ActionInput actionInput){
			while(turnTimeLeft > 0f){
				turnTimeLeft -= Time.deltaTime;
				yield return null;
			}

			HandleTurnTimeout(actor, actionInput);
		}

		void HandleTurnTimeout(Actor actor, ActionInput actionInput){
			switch(Rules.TurnTimeoutAction){
				case BattleRules.ActionFallbackType.CastDefaultAbility:
					Debug.Assert(actor.FallbackAbility != null || fallbackAbility != null, "The player has run out of time for this turn. The Battle Rules dictate that the fallback ability is cast." +
						"However, none was assigned to the actor, nor in the Battle Rules.");
					
					actionInput.AbilitySelectCallback(actor.FallbackAbility ?? fallbackAbility);
					break;
				case BattleRules.ActionFallbackType.CastRandomAbility:
					actionInput.AbilitySelectCallback(actionInput.ValidAbilities[Random.Range(0, actionInput.ValidAbilities.Length)]);
					break;
				case BattleRules.ActionFallbackType.SkipTurn:
					actionInput.SkipCallback();
					break;
			}
		}

		IEnumerator CRUseItem(Actor actor, Item item, Actor[] targets){
			Quaternion startRotation = actor.transform.rotation;
			bool wantsToRotate = item.Data.TurnTowardsTarget;

			if(wantsToRotate && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.Always){
				yield return BattleMonoBehaviour.Instance.StartCoroutine(CRRotateTowardsTargets(actor, targets));
			}

			yield return null;

			actor.OnItemStart.Invoke(item, targets);
			item.OnUse(actor, targets);

			item.PrepareForChainEvaluation(actor, targets);

			List<Actor> remainingTargets = targets.ToList();
			ItemResults itemResults = new ItemResults(item);

			foreach(ItemAction action in item.Data.Actions.Where(a => !a.IsChildEffect)){
				BattleActionResults actionResults = itemResults.CreateResult(action);

				for (int i = 0; i < remainingTargets.Count; i++) {
					Actor target = remainingTargets[i];
					bool breakChain = false;
					BattleInteractorData.HitStatus hitStatus = target != null ?
						target.PerformHitTest(actor, item.Data, action) :
						(Random.value <= action.HitChance ? BattleInteractorData.HitStatus.Hit : BattleInteractorData.HitStatus.Evade);

					switch(hitStatus){
						case BattleInteractorData.HitStatus.Hit:
							if(action == item.Data.Actions[0]){
								if(wantsToRotate && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.OnAttackHit){
									yield return BattleMonoBehaviour.Instance.StartCoroutine(CRRotateTowardsTargets(actor, targets));
								}

								float baseDelay = item.Data.BaseDuration;

								if(baseDelay > 0){
									yield return new WaitForSeconds(baseDelay);
								}
							}

							actionResults.hitTargets.Add(target);
							break;
						default:
							actionResults.missedActions.Add(new MissedAction(target, hitStatus));

							breakChain = action.BreaksChainOnMiss || action == item.Data.Actions.Last(a => !a.IsChildEffect);
							break;
					}

					if(breakChain){
						remainingTargets.RemoveAt(i);
						i--;
					}

					if(remainingTargets.Count == 0){
						EndPerformItem(actor, itemResults);

						if(item.Data.TurnTowardsTarget && Rules.ActorCanFaceTarget == BattleRules.ActorFacingMoment.Always){
							while(actor.transform.rotation != startRotation){
								actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, startRotation, Rules.FaceTargetSpeed * Time.deltaTime);
								yield return null;
							}
						}

						if(Rules.ProgressAutomatically){
							VerboseLogger.Log("Progressing battle automatically after failed item action", VerboseLoggerSettings.RegularColor);
							BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, ProgressBattle));
						}

						break;
					}
				}
			}

			item.Consume(1);

			if(remainingTargets.Count > 0){
				EndPerformItem(actor, itemResults);
			}

			if(item.Data.Recovery.Turns > 0){
				bool hitAtLeastOneTarget = false;
				bool damagedAtLeastOneTarget = false;

				foreach(KeyValuePair<ItemAction, BattleActionResults> result in itemResults.actionResults){
					if(result.Value.hitTargets.Count > 0){
						hitAtLeastOneTarget = true;

						if(result.Key.Action == ChainEvaluator.ActionType.Damage){
							damagedAtLeastOneTarget = true;
						}
					}
				}

				if(!((item.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetHit) == BattleInteractorData.RecoveryInterrupt.OnTargetHit && hitAtLeastOneTarget) &&
				   !((item.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetDamage) == BattleInteractorData.RecoveryInterrupt.OnTargetDamage && damagedAtLeastOneTarget) &&
				   !((item.Data.Recovery.Interrupt & BattleInteractorData.RecoveryInterrupt.OnTargetMiss) == BattleInteractorData.RecoveryInterrupt.OnTargetMiss && !hitAtLeastOneTarget)){
					item.recoveryTurnsRemaining = item.Data.Recovery.Turns;
					ActorInfo[actor].blockingAction = new QueuedAction(actor, targets, item);
					actor.OnItemRecoveryStart.Invoke(item);
				}
			}
		}

		void EndPerformItem(Actor actor, ItemResults itemResults){ //recursive
			actor.OnItemActionProcess.AddOneTimeListener<Actor[], Item, ItemAction>(ProcessItemActionEffect);
			actor.OnItemActionEnd.AddOneTimeListener<Actor[], Item, ItemAction>(EndItemActionEffect);

			KeyValuePair<ItemAction, BattleActionResults> currentResultKVP = itemResults.actionResults.First();
			itemResults.actionResults.Remove(currentResultKVP.Key);

			if(itemResults.actionResults.Count > 0){
				actor.OnItemActionEnd.AddOneTimeListener<Actor[], Item, ItemAction>((ac, it, ia) => {
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {EndPerformItem(actor, itemResults);}));
				});
			}

			if(currentResultKVP.Value.hitTargets.Count > 0){
				actor.ConfirmItemActionSuccess(currentResultKVP.Value.hitTargets.ToArray(), itemResults.item, currentResultKVP.Key);
			}

			if(currentResultKVP.Value.missedActions.Count > 0){
				actor.ConfirmItemActionFail(currentResultKVP.Value.missedActions.Select(a => a.target).ToArray(), itemResults.item,
					currentResultKVP.Value.missedActions.Select(a => a.hitStatus).ToArray(), currentResultKVP.Key, currentResultKVP.Value.missedActions.Count == 0);

				foreach(MissedAction missedAction in currentResultKVP.Value.missedActions){
					if(missedAction.hitStatus == BattleInteractorData.HitStatus.Evade && missedAction.target != null){
						missedAction.target.OnItemActionAvoid.Invoke(actor, itemResults.item, currentResultKVP.Key);
					}
				}
			}
		}

		IEnumerator CRProcessAffliction(Actor actor, Affliction affliction, System.Action progressAction){
			actor.OnAfflictionStart.Invoke(affliction);

			affliction.OnTrigger(actor);
			affliction.PrepareForChainEvaluation(affliction.Afflicter, new Actor[]{actor});

			AfflictionResults afflictionResults = new AfflictionResults(affliction);

			foreach(AfflictionAction action in affliction.Data.Actions.Where(a => !a.IsChildEffect)){
				BattleActionResults actionResults = afflictionResults.CreateResult(action);
				bool breakChain = false;
				BattleInteractorData.HitStatus hitStatus = actor.PerformHitTest(affliction.Afflicter, affliction.Data, action);

				switch(hitStatus){
					case BattleInteractorData.HitStatus.Hit:
						if(action == affliction.Data.Actions[0]){
							if(affliction.Data.BaseDuration > 0){
								yield return new WaitForSeconds(affliction.Data.BaseDuration);
							}
						}

						actionResults.hitTargets.Add(actor);
						//actor.ConfirmAfflictionActionSuccess(affliction, action); //was on

						//actor.OnAfflictionActionProcess.AddOneTimeListener<Affliction, AfflictionAction>((af, ac) => {ProcessAfflictionActionEffect(actor, af, ac);});
						//actor.OnAfflictionActionEnd.AddOneTimeListener<Affliction, AfflictionAction>((af, ac) => {EndAfflictionActionEffect(actor, af, ac, progressAction);});
						break;
					default:
						//actor.ConfirmAfflictionActionFail(affliction, hitStatus, action); //was on

						//breakChain = action.BreaksChainOnMiss;

						actionResults.missedActions.Add(new MissedAction(actor, hitStatus));

						breakChain = action.BreaksChainOnMiss || action == affliction.Data.Actions.Last(a => !a.IsChildEffect);
						break;
				}

				if(breakChain){
					EndPerformAffliction(actor, afflictionResults);

					if(Rules.ProgressAutomatically) {
						VerboseLogger.Log("Progressing battle automatically after failed affliction action", VerboseLoggerSettings.RegularColor);
						BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Turn, progressAction));
					}

					break;
				}
			}

			EndPerformAffliction (actor, afflictionResults);
		}

		void EndPerformAffliction(Actor actor, AfflictionResults afflictionResults){
			OnAfflictionResults.Invoke(actor, afflictionResults);
			ProcessAfflictionEnd(actor, afflictionResults);
		}

		void ProcessAfflictionEnd(Actor actor, AfflictionResults afflictionResults){ //recursive
			if(afflictionResults.actionResults.Count == 0){
				return;
			}

			KeyValuePair<AfflictionAction, BattleActionResults> currentResultKVP = afflictionResults.actionResults.First();
			afflictionResults.actionResults.Remove(currentResultKVP.Key);
			
			if(afflictionResults.actionResults.Count > 0){
				actor.OnAfflictionActionEnd.AddOneTimeListener<Affliction, AfflictionAction>((af, aa) => {
					BattleMonoBehaviour.Instance.StartCoroutine(CRProgressBattleAsSoonAsAllowed(ProgressType.Immediate, () => {ProcessAfflictionEnd(actor, afflictionResults);}));
				});
			}

			if(currentResultKVP.Value.hitTargets.Count > 0){
				actor.OnAfflictionActionEnd.AddOneTimeListener<Affliction, AfflictionAction>((af, aa) => {EndAfflictionActionEffect(actor, af, aa, true);});
				actor.OnAfflictionActionProcess.AddOneTimeListener<Affliction, AfflictionAction>((af, aa) => ProcessAfflictionActionEffect(actor, af, aa));
				actor.ConfirmAfflictionActionSuccess(afflictionResults.affliction, currentResultKVP.Key);
			}
			else{
				actor.OnAfflictionActionEnd.AddOneTimeListener<Affliction, AfflictionAction>((af, aa) => {EndAfflictionActionEffect(actor, af, aa, false);});
			}

			if(currentResultKVP.Value.missedActions.Count > 0){
				actor.ConfirmAfflictionActionFail(afflictionResults.affliction, currentResultKVP.Value.missedActions.Select(a => a.hitStatus).ToArray(),
												  currentResultKVP.Key, currentResultKVP.Value.missedActions.Count == 0);

				foreach(MissedAction missedAction in currentResultKVP.Value.missedActions){
					if(missedAction.hitStatus == BattleInteractorData.HitStatus.Evade && missedAction.target != null){
						missedAction.target.OnAfflictionActionAvoid.Invoke(afflictionResults.affliction, currentResultKVP.Key);
					}
				}
			}
		}

		IEnumerator CRRestoreActorRotation(Actor actor){
			Quaternion targetRotation = ActorInfo[actor].defaultRotation;
			float deltaAngle = Quaternion.Angle(actor.transform.rotation, targetRotation);

			RequestProgressDelay(deltaAngle / Rules.FaceTargetSpeed, DelayRequestReason.AnimationEvent);

			while(actor.transform.rotation != targetRotation){
				actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, targetRotation, Rules.FaceTargetSpeed * Time.deltaTime);
				yield return null;
			}
		}

		IEnumerator CRProgressBattleAsSoonAsAllowed(ProgressType progressType, System.Action progressAction){
			float baseDelay = progressWaitTimes[progressType];
			float startTime = Time.time;
			float maxDelay = ProgressDelayRequests.Count > 0 ? ProgressDelayRequests.Max(d => d.TimeRemaining) : 0f;

			yield return null; //always wait at least one frame (required in cases like initialBattleDelay being 0, and Battle.Start() being called from Start() or Awake())

			while(maxDelay > 0f || ProgressDelayLocks.Count > 0){
				ProgressDelayRequests.RemoveAll(d => d.TimeRemaining == 0f);
				maxDelay = ProgressDelayRequests.Count > 0 ? ProgressDelayRequests.Max(d => d.TimeRemaining) : 0f;

				yield return null;
			}

			yield return new WaitForSeconds(baseDelay);

			progressAction();
		}
	}
}