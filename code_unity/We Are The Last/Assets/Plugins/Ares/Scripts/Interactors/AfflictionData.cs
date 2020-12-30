using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Ability", menuName="Ares/Affliction", order=52)]
	public class AfflictionData : PowerData {
		public enum Cure {ConstantNumberOfTurns, RandomNumberOfTurns, RandomChance}
		public enum ProcessingMoment {Never, StartOfRound, EndOfRound, StartOfAfflictedActorTurn, EndOfAfflictedActorTurn}//, OnCure

		public string DisplayName {get{return displayName;}}
		public string Description {get{return description;}}
		public float BaseDuration {get{return baseDuration;}}
		public Cure CureCondition {get{return cureCondition;}}
		public int Power {get{return power;}}
		public float CureChance {get{return cureChance;}}
		public bool CureOnAfflictedDeath {get{return cureOnAfflictedDeath;}}
		public bool CureOnAfflicterDeath {get{return cureOnAfflicterDeath;}}
		public bool CanAfflictDefeatedActors {get{return canAfflictDefeatedActors;}}
		public ProcessingMoment EffectProcessingMoment {get{return effectProcessingMoment;}}
		public ProcessingMoment DurationProcessingMoment {get{return durationProcessingMoment;}}
		public DoubleSetStageAction DoubleSetStageBehaviour {get{return doubleSetStageBehaviour;}}
		public DoubleSetDurationAction DoubleSetDurationBehaviour {get{return doubleSetDurationBehaviour;}}
		public List<ActionToken> ActionTokens {get{return actionTokens;}}
		public List<AfflictionAction> Actions {get{return actions;}}

		public AnimationEffect ObtainAnimation {get{return obtainAnimation;}}
		public AnimationEffect TriggerAnimation {get{return triggerAnimation;}}
		public AnimationEffect StageIncreaseAnimation {get{return stageIncreaseAnimation;}}
		public AnimationEffect StageDecreaseAnimation {get{return stageDecreaseAnimation;}}
		public AnimationEffect EndAnimation {get{return endAnimation;}}
		public InstantiationEffect ObtainInstantiation {get{return obtainInstantiation;}}
		public InstantiationEffect TriggerInstantiation {get{return triggerInstantiation;}}
		public InstantiationEffect StageIncreaseInstantiation {get{return stageIncreaseInstantiation;}}
		public InstantiationEffect StageDecreaseInstantiation {get{return stageDecreaseInstantiation;}}
		public InstantiationEffect EndInstantiation {get{return endInstantiation;}}
		public AudioEffect ObtainAudio {get{return obtainAudio;}}
		public AudioEffect TriggerAudio {get{return triggerAudio;}}
		public AudioEffect StageIncreaseAudio {get{return stageIncreaseAudio;}}
		public AudioEffect StageDecreaseAudio {get{return stageDecreaseAudio;}}
		public AudioEffect EndAudio {get{return endAudio;}}
		
		[SerializeField, Header("Info")] string displayName = "New Affliction";
		[SerializeField, Multiline(3)] string description;
		[SerializeField, Tooltip("The base duration of this ability, before any effects are executed."), Header("Timing")] float baseDuration;
		[SerializeField] int power;
		[SerializeField, Header("Curing")] Cure cureCondition;
		[SerializeField] int duration1;
		[SerializeField] int duration2;
		[SerializeField, Range(0f, 1f)] float cureChance;
		[SerializeField, Tooltip("Remove the affliction when the afflicted actor dies.")] bool cureOnAfflictedDeath;
		[SerializeField, Tooltip("Remove the affliction when the actor who caused it dies.")] bool cureOnAfflicterDeath;
		[SerializeField, Header("Afflicting"), Tooltip("Allow actors who have already been defeated to obtain this affliction.")] bool canAfflictDefeatedActors = false;

		[SerializeField, Tooltip("The moment at which to process the effect and evaluate the action chain.")]
		ProcessingMoment effectProcessingMoment = ProcessingMoment.EndOfAfflictedActorTurn;

		[SerializeField, Tooltip("The moment at which to adjust the affliction's remaining duration if needed.")]
		ProcessingMoment durationProcessingMoment = ProcessingMoment.EndOfAfflictedActorTurn;

		[SerializeField, Tooltip("The adjustment to make to the affliction's stage if an already-afflicted actor tries to get afflicted by it again.")]
		DoubleSetStageAction doubleSetStageBehaviour;

		[SerializeField, Tooltip("The adjustment to make to the affliction's remaining duration if an already-afflicted actor tries to get afflicted by it again.")]
		DoubleSetDurationAction doubleSetDurationBehaviour;

		[SerializeField] List<ActionToken> actionTokens;
		[SerializeField] List<AfflictionAction> actions;
		[SerializeField] AnimationEffect obtainAnimation;
		[SerializeField] AnimationEffect triggerAnimation;
		[SerializeField] AnimationEffect stageIncreaseAnimation;
		[SerializeField] AnimationEffect stageDecreaseAnimation;
		[SerializeField] AnimationEffect endAnimation;
		[SerializeField] InstantiationEffect obtainInstantiation;
		[SerializeField] InstantiationEffect triggerInstantiation;
		[SerializeField] InstantiationEffect stageIncreaseInstantiation;
		[SerializeField] InstantiationEffect stageDecreaseInstantiation;
		[SerializeField] InstantiationEffect endInstantiation;
		[SerializeField] AudioEffect obtainAudio;
		[SerializeField] AudioEffect triggerAudio;
		[SerializeField] AudioEffect stageIncreaseAudio;
		[SerializeField] AudioEffect stageDecreaseAudio;
		[SerializeField] AudioEffect endAudio;

		void OnEnable(){
			if(actions == null){
				actionTokens = new List<ActionToken>();
				actions = new List<AfflictionAction>();
			}
		}

		public int GetCureDuration(){
			switch(cureCondition){
				case Cure.ConstantNumberOfTurns:
					return duration1;
				case Cure.RandomNumberOfTurns:
					return Random.Range(duration1, duration2 + 1);
			}

			return -1;
		}

		public float GetScaledPower(int stage){
			return GetScaledPowerFloat(power, stage);
		}
	}

	public class Affliction : ChainEvaluator {
		public AfflictionData Data {get; private set;}
		public Actor Afflicter {get; private set;}
		public int Stage {get{return stage;} set{stage = Mathf.Clamp(value, Data.MinStage, Data.MaxStage);}}
		public int RoundsRemaining {get{return roundsRemaining;} set{roundsRemaining = Mathf.Max(value, -1);}}
		public int RoundInflicted {get; private set;}

		int stage;
		int roundsRemaining; //in case of random value: only evaluate once at start, else higher value has a lower chance due to repeated evaluations

		public Affliction(AfflictionData afflictionData, int stage, int roundInflicted, Actor afflicter){
			Data = afflictionData;
			Afflicter = afflicter;
			RoundInflicted = roundInflicted;
			this.stage = stage;

			roundsRemaining = afflictionData.CureCondition == AfflictionData.Cure.RandomChance ? -1 : afflictionData.GetCureDuration();
		}

		bool animationObtainEventConsumed = false;
		bool animationTriggerEventConsumed = false;
		bool animationStageIncreaseEventConsumed = false;
		bool animationStageDecreaseEventConsumed = false;
		bool animationEndEventConsumed = false;
		bool instantiationObtainEventConsumed = false;
		bool instantiationTriggerEventConsumed = false;
		bool instantiationStageIncreaseEventConsumed = false;
		bool instantiationStageDecreaseEventConsumed = false;
		bool instantiationEndEventConsumed = false;
		bool audioObtainEventConsumed = false;
		bool audioTriggerEventConsumed = false;
		bool audioStageIncreaseEventConsumed = false;
		bool audioStageDecreaseEventConsumed = false;
		bool audioEndEventConsumed = false;

		public void ConsumeAnimationObtainEvent(){animationObtainEventConsumed = true;}
		public void ConsumeAnimationTriggerEvent(){animationTriggerEventConsumed = true;}
		public void ConsumeAnimationStageIncreaseEvent(){animationStageIncreaseEventConsumed = true;}
		public void ConsumeAnimationStageDecreaseEvent(){animationStageDecreaseEventConsumed = true;}
		public void ConsumeAnimationEndEvent(){animationEndEventConsumed = true;}
		public void ConsumeInstantiationObtainEvent(){instantiationObtainEventConsumed = true;}
		public void ConsumeInstantiationTriggerEvent(){instantiationTriggerEventConsumed = true;}
		public void ConsumeInstantiationStageIncreaseEvent(){instantiationStageIncreaseEventConsumed = true;}
		public void ConsumeInstantiationStageDecreaseEvent(){instantiationStageDecreaseEventConsumed = true;}
		public void ConsumeInstantiationEndEvent(){instantiationEndEventConsumed = true;}
		public void ConsumeAudioObtainEvent(){audioObtainEventConsumed = true;}
		public void ConsumeAudioTriggerEvent(){audioTriggerEventConsumed = true;}
		public void ConsumeAudioStageIncreaseEvent(){audioStageIncreaseEventConsumed = true;}
		public void ConsumeAudioStageDecreaseEvent(){audioStageDecreaseEventConsumed = true;}
		public void ConsumeAudioEndEvent(){audioEndEventConsumed = true;}

		public virtual void OnObtain(Actor caster, Actor target){
			if(Data.ObtainAnimation.enabled && !animationObtainEventConsumed){
				Data.ObtainAnimation.Trigger(caster);
			}

			if(Data.ObtainInstantiation.enabled && !instantiationObtainEventConsumed){
				Data.ObtainInstantiation.Trigger<Affliction>(caster, new Actor[]{target}, this);
			}

			if(Data.ObtainAudio.enabled && !audioObtainEventConsumed){
				Data.ObtainAudio.Trigger(caster, new Actor[]{target});
			}

			instantiationObtainEventConsumed = false;
			audioObtainEventConsumed = false;
		}

		public virtual void OnTrigger(Actor target){
			if(Data.TriggerAnimation.enabled && !animationTriggerEventConsumed){
				Data.TriggerAnimation.Trigger(Afflicter);
			}

			if(Data.TriggerInstantiation.enabled && !instantiationTriggerEventConsumed){
				Data.TriggerInstantiation.Trigger<Affliction>(Afflicter, new Actor[]{target}, this);
			}

			if(Data.TriggerAudio.enabled && !audioTriggerEventConsumed){
				Data.TriggerAudio.Trigger(Afflicter, new Actor[]{target});
			}

			instantiationTriggerEventConsumed = false;
			audioTriggerEventConsumed = false;
		}

		public virtual void OnStageIncrease(Actor target){
			if(Data.StageIncreaseAnimation.enabled && !animationStageIncreaseEventConsumed){
				Data.StageIncreaseAnimation.Trigger(Afflicter);
			}

			if(Data.StageIncreaseInstantiation.enabled && !instantiationStageIncreaseEventConsumed){
				Data.StageIncreaseInstantiation.Trigger<Affliction>(Afflicter, new Actor[]{target}, this);
			}

			if(Data.StageIncreaseAudio.enabled && !audioStageIncreaseEventConsumed){
				Data.StageIncreaseAudio.Trigger(Afflicter, new Actor[]{target});
			}

			instantiationStageIncreaseEventConsumed = false;
			audioStageIncreaseEventConsumed = false;
		}

		public virtual void OnStageDecrease(Actor target){
			if(Data.StageDecreaseAnimation.enabled && !animationStageDecreaseEventConsumed){
				Data.StageDecreaseAnimation.Trigger(Afflicter);
			}

			if(Data.StageDecreaseInstantiation.enabled && !instantiationStageDecreaseEventConsumed){
				Data.StageDecreaseInstantiation.Trigger<Affliction>(Afflicter, new Actor[]{target}, this);
			}

			if(Data.StageDecreaseAudio.enabled && !audioStageDecreaseEventConsumed){
				Data.StageDecreaseAudio.Trigger(Afflicter, new Actor[]{target});
			}

			instantiationStageDecreaseEventConsumed = false;
			audioStageDecreaseEventConsumed = false;
		}

		public virtual void OnEnd(Actor target){
			if(Data.EndAnimation.enabled && !animationEndEventConsumed){
				Data.EndAnimation.Trigger(Afflicter);
			}

			if(Data.EndInstantiation.enabled && !instantiationEndEventConsumed){
				Data.EndInstantiation.Trigger<Affliction>(Afflicter, new Actor[]{target}, this);
			}

			if(Data.EndAudio.enabled && !audioEndEventConsumed){
				Data.EndAudio.Trigger(Afflicter, new Actor[]{target});
			}

			instantiationEndEventConsumed = false;
			audioEndEventConsumed = false;
		}

		public bool ShouldEnd(){
			switch(Data.CureCondition){
				case AfflictionData.Cure.RandomChance:
					return Random.value <= Data.CureChance;
				default:
					return roundsRemaining <= 0;
			}
		}

		public void ResetEndTurn(int currentRound){
			roundsRemaining = currentRound + roundsRemaining - RoundInflicted;
		}

		public override void PrepareForChainEvaluation(Actor caster, Actor[] targets){
			PrepareForChainEvaluation(Data.ActionTokens, Data.Actions.Cast<ChainableAction>().ToList(), caster, targets);
		}

		protected override void AddUninitializedDefaultTokens(Actor caster, Actor[] targets){
			// Add special formula identifiers
			foreach(Actor target in targets){
				Dictionary<string, float> targetActionValues = new Dictionary<string, float>(targets.Length);
				targetActionValues.Add("AFFLICTED_HP", 0);
				targetActionValues.Add("AFFLICTED_MAX_HP", 0);

				targetActionValues.Add("AFFLICTER_HP", 0);
				targetActionValues.Add("AFFLICTER_MAX_HP", 0);

				targetActionValues.Add("BASE_POWER", 0);
				targetActionValues.Add("CURRENT_POWER", 0);

				foreach(string stat in caster.Stats.Keys){
					targetActionValues.Add("AFFLICTER_" + stat.ToUpper(), 0);
					targetActionValues.Add("AFFLICTED_" + stat.ToUpper(), 0);
				}

				foreach(ActionToken token in Data.ActionTokens){
					targetActionValues.Add(token.ID, token.EvaluationMode == ActionChainValueEvaluator.PowerType.Formula ? token.Evaluate(targetActionValues) : evaluatedActionTokens[token.ID]);
				}

				evaluatedActionValues.Add(target, targetActionValues);
			}
		}

		public float EvaluatePower(Actor afflicted, AfflictionAction action){
			Dictionary<string, float> targetEvaluatedActionValues = evaluatedActionValues[afflicted];

			if(action.PowerMode == AbilityAction.PowerType.Formula){
				targetEvaluatedActionValues["AFFLICTED_HP"] = afflicted.HP;
				targetEvaluatedActionValues["AFFLICTED_MAX_HP"] = afflicted.MaxHP;

				targetEvaluatedActionValues["AFFLICTER_HP"] = Afflicter.HP;
				targetEvaluatedActionValues["AFFLICTER_MAX_HP"] = Afflicter.MaxHP;

				foreach(string stat in afflicted.Stats.Keys){
					targetEvaluatedActionValues["AFFLICTED_" + stat.ToUpper()] = afflicted.Stats[stat].Value;
					targetEvaluatedActionValues["AFFLICTER_" + stat.ToUpper()] = Afflicter.Stats[stat].Value;
				}
			}
			
			targetEvaluatedActionValues["BASE_POWER"] = Data.Power;
			targetEvaluatedActionValues["CURRENT_POWER"] = Data.GetScaledPower(stage);

			float result = action.EvaluatePower(targetEvaluatedActionValues);

			if(action.PowerMode == ChainableAction.PowerType.Constant || action.PowerMode == ChainableAction.PowerType.Random){ //use as multiplier for regular power
				result *= targetEvaluatedActionValues["CURRENT_POWER"];
			}

			targetEvaluatedActionValues.Add(actionIdentifiers[action]+"_RAW", result);

			return result;
		}

		public void SetActionResult(AfflictionAction action, Actor target, int value){
			evaluatedActionValues[target].Add(actionIdentifiers[action], value);
		}
	}
}