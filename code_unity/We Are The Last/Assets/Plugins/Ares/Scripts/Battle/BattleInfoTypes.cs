using UnityEngine;
using System.Collections.Generic;

namespace Ares {
	public class ActorInfo{
		public Actor Actor {get; private set;}
		public BattleGroup Group {get; private set;}
		public bool IsParticipating {
			get{
				return isParticipating;
			}
			set{
				if(value != isParticipating){
					if(value){
						Actor.Battle.OnActorStartedParticipating.Invoke(Actor);
					}
					else{
						Actor.Battle.OnActorStoppedParticipating.Invoke(Actor);
					}

					isParticipating = value;
				}
			}}

		public Quaternion defaultRotation;
		public QueuedAction blockingAction;

		bool isParticipating;

		public ActorInfo(Actor actor, BattleGroup group, bool isParticipating){
			Actor = actor;
			Group = group;
			this.isParticipating = isParticipating;
		}

		public void UpdateBlockingAction(){
			if(blockingAction.Ability != null){
				UpdateBlockingAction(ref blockingAction.Ability.preparationTurnsRemaining, ref blockingAction.Ability.recoveryTurnsRemaining,
				                     blockingAction.Ability.Data.Preparation, blockingAction.Ability.Data.Recovery);
			}
			else{
				UpdateBlockingAction(ref blockingAction.Item.preparationTurnsRemaining, ref blockingAction.Item.recoveryTurnsRemaining,
				                     blockingAction.Item.Data.Preparation, blockingAction.Item.Data.Recovery);
			}
		}

		void UpdateBlockingAction(ref int preparationTurnsRemaining, ref int recoveryTurnsRemaining, BattleInteractorData.PreparationData preparation,
		                          BattleInteractorData.RecoveryData recovery){
			if(preparationTurnsRemaining > 0){
				preparationTurnsRemaining -= preparation.GetTurnReduction();

				if(preparationTurnsRemaining <= 0){
					blockingAction = null;
				}
			}
			else if(recoveryTurnsRemaining > 0){
				recoveryTurnsRemaining -= recovery.GetTurnReduction();

				if(recoveryTurnsRemaining <= 0){
					blockingAction = null;
				}
			}
		}
	}

	public class QueuedAction{
		public Actor Caster {get; private set;}
		public Actor[] Targets {get; private set;}
		public Ability Ability {get; private set;}
		public Item Item {get; private set;}
		public int Priority {get; private set;}

		public QueuedAction(Actor caster){
			Caster = caster;
		}

		public QueuedAction(Actor caster, Actor[] targets, Ability ability){
			Caster = caster;
			Targets = targets;
			Ability = ability;
			Priority = ability.Data.Priority;
		}

		public QueuedAction(Actor caster, Actor[] targets, Item item, int priority = 0){
			Caster = caster;
			Targets = targets;
			Item = item;
			Priority = priority;
		}

		public void Clear(){
			Ability = null;
			Item = null;
			Targets = null;
		}

		public void UpdateTargetValidity(Dictionary<Actor, ActorInfo> actorInfo){
			System.Func<ActorInfo, ActorInfo, bool> poll = Ability != null ? (System.Func<ActorInfo, ActorInfo, bool>)Ability.CanUse : (System.Func<ActorInfo, ActorInfo, bool>)Item.CanUse;

			List<Actor> validTargets = new List<Actor>(Targets.Length);

			foreach(Actor target in Targets){
				if(poll(actorInfo[Caster], actorInfo[target])){
					validTargets.Add(target);
				}
			}

			if(validTargets.Count != Targets.Length){
				Targets = validTargets.ToArray();
			}
		}
	}

	public struct DelayRequest{
		public float TimeRemaining{get{return Mathf.Max(delay - (Time.time - requestedAt), 0f);}}

		float delay;
		float requestedAt;

		public DelayRequest(float delay){
			this.delay = delay;

			requestedAt = Time.time;
		}
	}

	public struct AfflictionQueue{ //Tuples not supported in .Net 3.5
		public Actor Actor {get; private set;}
		public Queue<Affliction> Queue {get; private set;}

		public AfflictionQueue(Actor actor, Queue<Affliction> queue){
			this.Actor = actor;
			this.Queue = queue;
		}
	}

	public struct ActionInput {
		public readonly Ability[] ValidAbilities;
		public readonly Item[] ValidItems;
		public readonly System.Func<Ability, bool> AbilitySelectCallback;
		public readonly System.Func<Item, bool> ItemSelectCallback;
		public readonly System.Func<bool> SkipCallback;

		public ActionInput(Ability[] validAbilities, Item[] validItems, System.Func<Ability, bool> abilitySelectCallback,
			System.Func<Item, bool> itemSelectCallback, System.Func<bool> skipCallback){
			ValidAbilities = validAbilities;
			ValidItems = validItems;
			AbilitySelectCallback = abilitySelectCallback;
			ItemSelectCallback = itemSelectCallback;
			SkipCallback = skipCallback;
		}
	}

	public struct TargetInputSingleActor {
		public readonly Actor[] ValidTargets;
		public readonly System.Func<Actor, bool> TargetSelectCallback;

		public TargetInputSingleActor(Actor[] validTargets, System.Func<Actor, bool> targetSelectCallback){
			ValidTargets = validTargets;
			TargetSelectCallback = targetSelectCallback;
		}
	}

	public struct TargetInputNumActors {
		public readonly Actor[] ValidTargets;
		public readonly int TargetsRequired;
		public readonly System.Func<Actor[], bool> TargetSelectCallback;

		public TargetInputNumActors(Actor[] validTargets, int targetsRequired, System.Func<Actor[], bool> targetSelectCallback){
			ValidTargets = validTargets;
			TargetsRequired = targetsRequired;
			TargetSelectCallback = targetSelectCallback;
		}
	}

	public struct TargetInputGroup {
		public readonly BattleGroup[] ValidTargets;
		public readonly System.Func<BattleGroup, bool> TargetSelectCallback;

		public TargetInputGroup(BattleGroup[] validTargets, System.Func<BattleGroup, bool> targetSelectCallback){
			ValidTargets = validTargets;
			TargetSelectCallback = targetSelectCallback;
		}
	}

	public struct MissedAction {
		public readonly Actor target;
		public readonly Ares.BattleInteractorData.HitStatus hitStatus;

		public MissedAction(Actor target, Ares.BattleInteractorData.HitStatus hitStatus){
			this.target = target;
			this.hitStatus = hitStatus;
		}
	}

	public struct BattleActionResults {
		public readonly List<Actor> hitTargets;
		public readonly List<MissedAction> missedActions;

		public BattleActionResults(int __UNUSED){ //Cannot be a parameterless ctor, but want to enforce readonly-ness
			hitTargets = new List<Actor>();
			missedActions = new List<MissedAction>();
		}
	}

	public struct AbilityResults {
		public readonly Ability ability;
		public readonly Dictionary<AbilityAction, BattleActionResults> actionResults;

		public AbilityResults(Ability ability){
			this.ability = ability;
			actionResults = new Dictionary<AbilityAction, BattleActionResults>();
		}

		public BattleActionResults CreateResult(AbilityAction action){
			BattleActionResults results = new BattleActionResults(0);
			actionResults.Add(action, results);

			return results;
		}
	}

	public struct ItemResults {
		public readonly Item item;
		public readonly Dictionary<ItemAction, BattleActionResults> actionResults;

		public ItemResults(Item item){
			this.item = item;
			actionResults = new Dictionary<ItemAction, BattleActionResults>();
		}

		public BattleActionResults CreateResult(ItemAction action){
			BattleActionResults results = new BattleActionResults(0);
			actionResults.Add(action, results);

			return results;
		}
	}

	public struct AfflictionResults {
		public readonly Affliction affliction;
		public readonly Dictionary<AfflictionAction, BattleActionResults> actionResults;

		public AfflictionResults(Affliction affliction){
			this.affliction = affliction;
			actionResults = new Dictionary<AfflictionAction, BattleActionResults> ();
		}

		public BattleActionResults CreateResult(AfflictionAction action){
			BattleActionResults results = new BattleActionResults(0);
			actionResults.Add (action, results);

			return results;
		}
	}

	/* Replay structs */
	public struct EnvironmentVariableResults {
		public enum Action {Set, Unset, StageIncrease, StageDecrease, DurationIncrease, DurationDecrease, AbilityAction}

		public readonly EnvironmentVariable environmentVariable;
		public readonly RoundMoment roundMoment;
		public readonly Action[] actions;
		public readonly Dictionary<AbilityAction, BattleActionResults> actionResults;

		public EnvironmentVariableResults(EnvironmentVariable environmentVariable, RoundMoment roundMoment){
			this.environmentVariable = environmentVariable;
			this.roundMoment = roundMoment;
			actions = new Action[1];
			actionResults = new Dictionary<AbilityAction, BattleActionResults>();
		}

		public BattleActionResults CreateResult(AbilityAction action){
			BattleActionResults results = new BattleActionResults(0);
			actionResults.Add(action, results);

			return results;
		}
	}

	public struct AfflictionResult {
		public enum Action {Set, Cure, StageIncrease, StageDecrease, DurationIncrease, DurationDecrease, ProcessActionChain}

		public readonly Affliction affliction;
		public readonly RoundMoment roundMoment;
		public readonly Action[] actions;
		public readonly Dictionary<AbilityAction, BattleActionResults> actionResults;
	}

	public struct TurnResults {
		public readonly Actor actor;
		public readonly AbilityResults abilityResults;
		public readonly ItemResults itemResults;
		public readonly AfflictionResult[] afflictionResults;
		public readonly AfflictionResult[] environmentVariableResults;
	}
}