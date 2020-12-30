using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	public static class BattleInteractorData {
		public enum TargetType {SingleActor, NumberOfActors, AllActorsInGroup, AllActors}
		public enum TargetGroupActors {All, Self, Allies, Opponents, AlliesAndOpponents, AlliesAndSelf}
		public enum TargetGroupGroups {Allies, Opponents, All}
		public enum TargetAliveState {Alive, Defeated, All}
		public enum TargetParticipationState {Participating, NonParticipating, All}
		public enum HitStatus {Hit, Evade, Immune}
		
		[System.Flags]
		public enum PreparationInterrupt {OnDamage = 1, OnDeath = 2}
		
		[System.Flags]
		public enum RecoveryInterrupt {OnTargetMiss = 1, OnTargetDamage = 2, OnTargetHit = 4, OnTargetDeath = 8, OnDamage = 16, OnDeath = 32}

		[System.Serializable]
		public class BlockingInteractorData{
			public int Turns {get{return turns;}}
			public AnimationEffect[] Animations {get{return animations;}}
			public AudioEffect[] Audios {get{return audios;}}
			public InstantiationEffect[] Instantiations {get{return instantiations;}}
			
			[SerializeField] int turns;
			[SerializeField] string[] texts;
			[SerializeField] AnimationEffect[] animations;
			[SerializeField] AudioEffect[] audios;
			[SerializeField] InstantiationEffect[] instantiations;
			
			public string GetMessage(int turnsRemaining, Actor user){
				string message = texts[turns - turnsRemaining];
				
				message = message.Replace("ACTOR_NAME", user.DisplayName);
				message = message.Replace("TURNS_REMAINING", turnsRemaining.ToString());
				
				return message;
			}

//			public AnimationEffect GetAnimationEffect(int turnsRemaining){
//				return animations[turns - turnsRemaining];
//			}
//
//			public AudioEffect GetAudioEffect(int turnsRemaining){
//				return audios[turns - turnsRemaining];
//			}
//
//			public InstantiationEffect GetInstantiationEffect(int turnsRemaining){
//				return instantiations[turns - turnsRemaining];
//			}
			
			// Here so that the Actor.OnInteractor[Preparation|Recovery]Update events can fire properly, even with custom logic.
			public int GetTurnReduction(){
				return 1;
			}
		}

		[System.Serializable]
		public class PreparationData : BlockingInteractorData{
			public PreparationInterrupt Interrupt {get{return interrupt;}}

			[SerializeField, EnumFlagsAttribute] PreparationInterrupt interrupt = PreparationInterrupt.OnDeath;
		}
		
		[System.Serializable]
		public class RecoveryData : BlockingInteractorData{
			public RecoveryInterrupt Interrupt {get{return interrupt;}}

			[SerializeField, EnumFlagsAttribute] RecoveryInterrupt interrupt;
		}
	}

	public abstract class BattleInteractorData<T> : ScriptableObject where T : ChainableAction {
		abstract public string FullCategoryPath {get;}

		public string DisplayName {get{return displayName;}}
		public string Description {get{return description;}}
		public float BaseDuration {get{return baseDuration;}}
		public BattleInteractorData.PreparationData Preparation {get{return preparation;}}
		public BattleInteractorData.RecoveryData Recovery {get{return recovery;}}
		public BattleInteractorData.TargetType TargetType{get{return targetType;}}
		public int NumberOfTargets{get{return numberOfTargets;}}
		public BattleInteractorData.TargetGroupActors ValidTargets {get{return validTargets;}}
		public bool IsTargetable {get{return isTargetable;}}
		public BattleInteractorData.TargetGroupGroups ValidTargetGroups {get{return validTargetGroups;}}
		public BattleInteractorData.TargetAliveState ValidTargetStates {get{return validTargetStates;}}
		public BattleInteractorData.TargetParticipationState ValidTargetParticipants {get{return validTargetParticipants;}}
		public bool TurnTowardsTarget {get{return turnTowardsTarget;}}
		public List<ActionToken> ActionTokens {get{return actionTokens;}}
		public List<T> Actions { get{return actions;}}
		public AnimationEffect Animation {get{return animation;}}
		public InstantiationEffect Instantiation {get{return instantiation;}}
		public AudioEffect Audio {get{return audio;}}

		[SerializeField, Header("Info")] string displayName;
		[SerializeField, Multiline(3)] string description;

		[SerializeField, Tooltip("The base duration of this ability, before any effects are executed."), Header("Timing")] float baseDuration;
		[SerializeField, Tooltip("Number of turns needed to charge before this interactor evaluates its action chain.")] BattleInteractorData.PreparationData preparation;
		[SerializeField, Tooltip("Number of turns needed to charge before this interactor evaluates its action chain.")] BattleInteractorData.RecoveryData recovery;
		[SerializeField, Tooltip("The type of targets this interactor requires."), Header("Targeting")] BattleInteractorData.TargetType targetType;
		[SerializeField, Tooltip("The maximum number of actors this interactor can target.")] int numberOfTargets = 1;
		[SerializeField, Tooltip("The type of actors this interactor can target.")] BattleInteractorData.TargetGroupActors validTargets;
		[SerializeField, Tooltip("Allows the actor to choose the target actors for this interactor.")] bool isTargetable = true;
		[SerializeField, Tooltip("The type of actors this interactor can target.")] BattleInteractorData.TargetGroupGroups validTargetGroups;
		[SerializeField, Tooltip("The liveness of the actors this interactor can target.")] BattleInteractorData.TargetAliveState validTargetStates;
		[SerializeField, Tooltip("The participation status of the actors this interactor can target.")] BattleInteractorData.TargetParticipationState validTargetParticipants;
		[SerializeField, Tooltip("Let the user of this interactor to turn towards the chosen target(s) if the rules allow it.")] bool turnTowardsTarget;
		[SerializeField] List<ActionToken> actionTokens;
		[SerializeField] List<T> actions;
		[SerializeField] AnimationEffect animation;
		[SerializeField] InstantiationEffect instantiation;
		[SerializeField] AudioEffect audio;

		void Reset(){
			actionTokens = new List<ActionToken>();
			actions = new List<T>();

			if(animation == null){
				animation = new AnimationEffect();
				instantiation = new InstantiationEffect(null, "", InstantiationTargetActor.Caster, false, Vector3.zero, Vector3.zero, Vector3.one, 0f);
				audio = new AudioEffect(null, AudioPlayPosition.Caster, 1f, 0f);
			}
			else{
				animation.Reset();
				audio.Reset();
				instantiation.Reset(InstantiationTargetMode.FindByName);
			}
		}
	}

	[System.Serializable]
	public class BattleInteractor<T1, T2> : ChainEvaluator where T1 : BattleInteractorData<T2> where T2 : ChainableAction {
		public T1 Data {get{return data;} protected set{data = value;}}

		[SerializeField] T1 data;
		public int preparationTurnsRemaining;
		public int recoveryTurnsRemaining;

		bool animationUseEventConsumed = false;
		bool instantiationUseEventConsumed = false;
		bool audioUseEventConsumed = false;

		public void ConsumeAnimationUseEvent(){
			animationUseEventConsumed = true;
		}

		public void ConsumeInstantiationUseEvent(){
			instantiationUseEventConsumed = true;
		}

		public void ConsumeAudioUseEvent(){
			audioUseEventConsumed = true;
		}

		// This lives inside BattleInteractorData.cs to easily allow custom overrides for specialized derivatives.
		// For example, a subclass could add extra hp or enemy type requirements.
		public virtual bool CanUse(ActorInfo user, ActorInfo target){
			if((Data.ValidTargetStates == BattleInteractorData.TargetAliveState.Alive && target.Actor.HP == 0) ||
			   (Data.ValidTargetStates == BattleInteractorData.TargetAliveState.Defeated && target.Actor.HP > 0) ||
			   (Data.ValidTargetParticipants == BattleInteractorData.TargetParticipationState.Participating && !target.IsParticipating) ||
			   (Data.ValidTargetParticipants == BattleInteractorData.TargetParticipationState.NonParticipating && target.IsParticipating)){
				return false;
			}

			if(Data.TargetType == BattleInteractorData.TargetType.SingleActor || Data.TargetType == BattleInteractorData.TargetType.NumberOfActors){
				switch(Data.ValidTargets){
					case BattleInteractorData.TargetGroupActors.Self:					return user.Actor == target.Actor;
					case BattleInteractorData.TargetGroupActors.Allies:					return user.Group == target.Group && user.Actor != target.Actor;
					case BattleInteractorData.TargetGroupActors.AlliesAndSelf:			return user.Group == target.Group;
					case BattleInteractorData.TargetGroupActors.AlliesAndOpponents:		return user.Actor != target.Actor;
					case BattleInteractorData.TargetGroupActors.Opponents:				return user.Group != target.Group;
					case BattleInteractorData.TargetGroupActors.All:					return true;
				}
			}
			else{
				switch(Data.ValidTargetGroups){
					case BattleInteractorData.TargetGroupGroups.Allies:					return user.Group == target.Group;
					case BattleInteractorData.TargetGroupGroups.Opponents:				return user.Group != target.Group;
					case BattleInteractorData.TargetGroupGroups.All:					return true;
				}
			}

			return false;
		}

		public virtual void HandleEffects(Actor caster, Actor[] targets, AnimationEffect animationEffect, AudioEffect audioEffect, InstantiationEffect instantiationEffect){
			if(animationEffect.enabled && !animationUseEventConsumed){
				animationEffect.Trigger(caster);
			}

			if(instantiationEffect.enabled && !instantiationUseEventConsumed){
				instantiationEffect.Trigger<BattleInteractor<T1, T2>>(caster, targets, this);
			}

			if(audioEffect.enabled && !audioUseEventConsumed){
				audioEffect.Trigger(caster, targets);
			}

			animationUseEventConsumed = false;
			instantiationUseEventConsumed = false;
			audioUseEventConsumed = false;
		}

		public virtual void OnUse(Actor caster, Actor[] targets){
			HandleEffects(caster, targets, data.Animation, data.Audio, data.Instantiation);
		}

		public virtual void OnPrepare(Actor caster, Actor[] targets, int turnsRemaining){
			int effectIndex = data.Preparation.Turns - turnsRemaining;

			HandleEffects(caster, targets, data.Preparation.Animations[effectIndex], data.Preparation.Audios[effectIndex], data.Preparation.Instantiations[effectIndex]);
		}

		public virtual void OnRecover(Actor caster, Actor[] targets, int turnsRemaining){
			int effectIndex = data.Recovery.Turns - turnsRemaining;

			HandleEffects(caster, targets, data.Recovery.Animations[effectIndex], data.Recovery.Audios[effectIndex], data.Recovery.Instantiations[effectIndex]);
		}

		public override void PrepareForChainEvaluation(Actor caster, Actor[] targets){
			PrepareForChainEvaluation(Data.ActionTokens, Data.Actions.Cast<ChainableAction>().ToList(), caster, targets);
		}

		protected override void AddUninitializedDefaultTokens(Actor caster, Actor[] targets){
			// Add special formula identifiers
			foreach(Actor target in targets){
				Dictionary<string, float> targetActionValues = new Dictionary<string, float>(targets.Length);

				targetActionValues.Add("CASTER_HP", caster.HP);
				targetActionValues.Add("CASTER_MAX_HP", caster.MaxHP);

				targetActionValues.Add("TARGET_HP", target.HP);
				targetActionValues.Add("TARGET_MAX_HP", target.MaxHP);

				foreach(string stat in caster.Stats.Keys){
					targetActionValues.Add("CASTER_" + stat.ToUpper(), caster.Stats[stat].Value);
					targetActionValues.Add("TARGET_" + stat.ToUpper(), target.Stats[stat].Value);
				}

				foreach(ActionToken token in Data.ActionTokens){
					targetActionValues.Add(token.ID, token.EvaluationMode == ActionChainValueEvaluator.PowerType.Formula ? token.Evaluate(targetActionValues) : evaluatedActionTokens[token.ID]);
				}

				evaluatedActionValues.Add(target, targetActionValues);
			}
		}
		
		protected virtual void ApplyPowerModifiers(Actor caster, Actor interactorTarget, Actor actionTarget, AbilityAction action, ref float result){
			// The actual power calculation. Here you could implement extra modifiers like
			// weaknesses, critical hits, etc.

			switch(action.Action){
				case ActionType.Damage:
					result *= caster.Stats["attack"].Value / actionTarget.Stats["defense"].Value;
					break;
				case ActionType.Heal:
					break;
				case ActionType.Afflict:
					break;
				case ActionType.Cure:
					break;
				case ActionType.Buff:
					break;
				case ActionType.Environment:
					break;
			}
		}

		public float EvaluatePower(Actor caster, Actor interactorTarget, Actor actionTarget, AbilityAction action){
			Dictionary<string, float> targetEvaluatedActionValues = evaluatedActionValues[interactorTarget];

			if(action.PowerMode == AbilityAction.PowerType.Formula){
				UpdateFormulaTokens(targetEvaluatedActionValues, caster, actionTarget);
			}

			float result = action.EvaluatePower(targetEvaluatedActionValues);

			targetEvaluatedActionValues.Add(actionIdentifiers[action] + "_RAW", result);

			ApplyPowerModifiers(caster, interactorTarget, actionTarget, action, ref result);

			return result;
		}

		public float EvaluateSpecial(Actor caster, Actor interactorTarget, Actor actionTarget, AbilityAction action){
			Dictionary<string, float> targetEvaluatedActionValues = evaluatedActionValues[interactorTarget];

			if(action.SpecialMode == AbilityAction.PowerType.Formula){
				UpdateFormulaTokens(targetEvaluatedActionValues, caster, actionTarget);
			}

			return action.EvaluateSpecial(targetEvaluatedActionValues);
		}

		void UpdateFormulaTokens(Dictionary<string, float> targetEvaluatedActionValues, Actor caster, Actor actionTarget){
			targetEvaluatedActionValues["CASTER_HP"] = caster.HP;
			targetEvaluatedActionValues["CASTER_MAX_HP"] = caster.MaxHP; 
			targetEvaluatedActionValues["TARGET_HP"] = actionTarget.HP;
			targetEvaluatedActionValues["TARGET_MAX_HP"] = actionTarget.MaxHP;

			foreach(string stat in caster.Stats.Keys){
				targetEvaluatedActionValues["CASTER_" + stat.ToUpper()] = caster.Stats[stat].Value;
				targetEvaluatedActionValues["TARGET_" + stat.ToUpper()] = actionTarget.Stats[stat].Value;
			}
		}
		
		public void SetActionResult(AbilityAction action, Actor abilityTarget, int value){
			evaluatedActionValues[abilityTarget].Add(actionIdentifiers[action], value);
		}
	}
}