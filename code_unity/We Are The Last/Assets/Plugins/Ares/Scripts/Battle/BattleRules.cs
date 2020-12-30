using UnityEngine;
using System.Collections.Generic;

namespace Ares {
	[CreateAssetMenu(fileName="New Battle Rules", menuName="Ares/Battle Rules", order=10)]
	public class BattleRules : ScriptableObject {
		public enum AbilityFallbackType {ContinueAsNormal, CastDefaultAbility, SkipTurn}
		public enum TargetFallbackType {SelectRandomTarget, CastDefaultAbility, SkipTurn}
		public enum ActionFallbackType {CastRandomAbility, CastDefaultAbility, SkipTurn}
		public enum ActorFacingMoment {Always, OnAttackHit, Never}
		public enum SelectActionMoment {OnRoundStart, OnTurnStart}
		public enum RoundStartModeItemComsumptionMoment {OnRoundStart, OnTurn, OnTurnButMarkPendingOnSelect}
		public enum TimedProcess {AfflictionEffect, AfflictionDuration, EnvironmentVariableCallback, EnvironmentVariableDuration, TemporaryBuffReduction}
		public enum ActorSort {BySpeed, Random, None}

		public int MaxRounds {get{return maxRounds;}}
		public float TurnTimeout {get{return turnTimeout;}}
		public bool ProgressAutomatically {get{return progressAutomatically;}}
		public float InitialBattleProgressDelay {get{return initialBattleProgressDelay;}}
		public float TimeBetweenMoves {get{return timeBetweenMoves;}}
		public float TimeBetweenRounds {get{return timeBetweenRounds;}}
		public float TimeBetweenAfflictionEffects {get{return timeBetweenAfflictionEffects;}}
		public float TimeBetweenAfflictionDurationAdjustments {get{return timeBetweenAfflictionDurationAdjustments;}}
		public float TimeBetweenEnvironmentVariableCallbacks {get{return timeBetweenEnvironmentVariableCallbacks;}}
		public float TimeBetweenEnvironmentVariableDurationAdjustments {get{return timeBetweenEnvironmentVariableDurationAdjustments;}}
		public bool CanSelectAbilitiesWithNoValidTargets {get{return canCastAbilitiesWithNoValidTargets;}}
		public bool CanSelectItemsWithNoValidTargets {get{return canUseItemsWithNoValidTargets;}}
		public AbilityFallbackType NoValidActionsAction {get{return noValidActionsAction;}}
		public TargetFallbackType InvalidAbilityTargetAction {get{return invalidAbilityTargetAction;}}
		public TargetFallbackType InvalidItemTargetAction {get{return invalidItemTargetAction;}}
		public ActionFallbackType TurnTimeoutAction {get{return turnTimeoutAction;}}
//		public ActionFallbackType InvalidActionFallback {get{return invalidActionFallback;}} //V2
		public AbilityData DefaultAbility {get{return defaultAbility;}}
		public ActorFacingMoment ActorCanFaceTarget {get{return actorCanFaceTarget;}}
		public SelectActionMoment SelectActorAction {get{return selectActorAction;}}
		public RoundStartModeItemComsumptionMoment ItemComsumptionMoment {get{return itemComsumptionMoment;}}
		public bool AutoSelectSingleOptionTargetForAbility {get{return autoSelectSingleOptionTargetForAbility;}}
		public bool AutoSelectNoChoiceMultiTargetsForAbility {get{return autoSelectNoChoiceMultiTargetsForAbility;}}
		public bool AutoSelectSingleOptionTargetForItem {get{return autoSelectSingleOptionTargetForItem;}}
		public bool AutoSelectNoChoiceMultiTargetsForItem {get{return autoSelectNoChoiceMultiTargetsForItem;}}
		public float FaceTargetSpeed {get{return faceTargetSpeed;}}
		public ActorSort ActorSortType {get{return actorSort;}}
		public CleanupProperty CleanupProperties {get{return cleanupProperties;}}
		public float CleanupDelay {get{return cleanupDelay;}}

		public TimedProcess[] TimedProcessesOrder {
			get{
				#if UNITY_EDITOR
				System.Array timedProcessValues = System.Enum.GetValues(typeof(TimedProcess));

				if(timedProcessValues.Length != timedProcessesOrder.Length){
					Debug.LogError("Not all timed processes had an explicit order defined, or old processes have since been deleted. " +
						"A new order has been established. Please view the Rules object in the Inspector to verify it.");

					timedProcessesOrder = new TimedProcess[timedProcessValues.Length];

					for(int i = 0; i < timedProcessesOrder.Length; i++){
						timedProcessesOrder[i] = (TimedProcess)timedProcessValues.GetValue(i);
					}
				}
				#endif

				return timedProcessesOrder;
			}
		}
		
		public System.Comparison<Actor> ActorOrderSorter {
			get{
				switch(actorSort){
					case ActorSort.BySpeed:		return ActorSortSpeed;
					case ActorSort.Random:		return ActorSortRandom;
					case ActorSort.None:		return ActorSortNone;
				}
				
				return ActorSortNone;
			}
		}
		
		[SerializeField, Tooltip("The number of round before the battle automatically ends.")] int maxRounds = 10;
		[SerializeField, Tooltip("The maximum time allowed for a player to select their actions. A value of 0 disables timeouts alltogether.")] float turnTimeout;
		[SerializeField, Tooltip("Automatically progress the battle between rounds and turns. Disable to progress the battle manually instead.")]bool progressAutomatically = true;
		[SerializeField, Tooltip("Allow UI delay requests to delay automatic battle progression.")] bool waitForUIEvents = true;
		[SerializeField, Tooltip("Allow animation delay requests to delay automatic battle progression.")] bool waitForAnimationEvents = true;
		[SerializeField, Tooltip("Allow ability delay requests to delay automatic battle progression.")] bool waitForAbilityEvents = true;
		[SerializeField, Tooltip("A delay in progressing the battle after it's been first started.")] float initialBattleProgressDelay = .5f;
		[SerializeField] float timeBetweenMoves = .5f;
		[SerializeField] float timeBetweenRounds = .5f;
		[SerializeField] float timeBetweenAfflictionEffects = .5f;
		[SerializeField] float timeBetweenAfflictionDurationAdjustments = .5f;
		[SerializeField] float timeBetweenEnvironmentVariableCallbacks = .5f;
		[SerializeField] float timeBetweenEnvironmentVariableDurationAdjustments = .5f;
		[SerializeField, Tooltip("Allow abilities to be cast even when there are no valid targets for it at the time of casting.")] bool canCastAbilitiesWithNoValidTargets = true;
		[SerializeField, Tooltip("Allow items to be used even when there are no valid targets for it at the time.")] bool canUseItemsWithNoValidTargets = true;
		[SerializeField, Tooltip("Action to take when an actor has no valid actions to select from.")] AbilityFallbackType noValidActionsAction;
		[SerializeField, Tooltip("Action to take when the cast ability has no valid targets for it at the time of processing.")] TargetFallbackType invalidAbilityTargetAction;
		[SerializeField, Tooltip("Action to take when the used item has no valid targets for it at the time of processing.")] TargetFallbackType invalidItemTargetAction;
		[SerializeField, Tooltip("Action to take when the time to select actions has run out.")] ActionFallbackType turnTimeoutAction;
//		[SerializeField, Tooltip("Action to take when the queued action is no longer valid at the time of processing.")] ActionFallbackType invalidActionFallback; //V2
		[SerializeField, Tooltip("The default fallback ability for actors to cast. Can be overridden per actor.")] AbilityData defaultAbility;
		[SerializeField, Tooltip("The circumstances under which an actor is allowed to face its target if an item or ability requests it.")] ActorFacingMoment actorCanFaceTarget;
		[SerializeField, Tooltip("The moment at which moment to have players and AI select their next action.")] SelectActionMoment selectActorAction;
		[SerializeField, Tooltip("The moment at which moment item uses should be processed.")] RoundStartModeItemComsumptionMoment itemComsumptionMoment;
		[SerializeField, Tooltip("Automatically select available ability target when there is only one option.")] bool autoSelectSingleOptionTargetForAbility = false;
		[SerializeField, Tooltip("Automatically select all available ability targets when there are [target options] <= [requested targets].")] bool autoSelectNoChoiceMultiTargetsForAbility = false;
		[SerializeField, Tooltip("Automatically select available item target when there is only one option.")] bool autoSelectSingleOptionTargetForItem = false;
		[SerializeField, Tooltip("Automatically select all available item targets when there are [target options] <= [requested targets].")] bool autoSelectNoChoiceMultiTargetsForItem = false;
		[SerializeField, Tooltip("The speed at which actors rotate to face their targets (in degrees/second).")] float faceTargetSpeed = 360f;
		[SerializeField, Tooltip("The order in which to process events scheduled for the same moment.")] TimedProcess[] timedProcessesOrder;
		[SerializeField, Tooltip("The method by which to sort actors' turns within a round.")] ActorSort actorSort;
		[SerializeField, EnumFlagsAttribute, Tooltip("The objects to cleanup after the battle is over.")] CleanupProperty cleanupProperties;
		[SerializeField, Tooltip("The amount of time to wait before cleaning up after the battle is over.")] float cleanupDelay;

		public bool AllowWaitingFor(DelayRequestReason reason){
			return (reason == DelayRequestReason.UIEvent && waitForUIEvents) ||
				   (reason == DelayRequestReason.AnimationEvent && waitForAnimationEvents) ||
				   (reason == DelayRequestReason.AbilityEvent && waitForAbilityEvents);
		}

		static int ActorSortSpeed(Actor a1, Actor a2){
			return (int)(a2.Stats["speed"].Value - a1.Stats["speed"].Value);
		}

		static int ActorSortRandom(Actor a1, Actor a2){
			return Random.value < .5f ? -1 : 1;
		}

		static int ActorSortNone(Actor a1, Actor a2){
			return 1;
		}
	}
}