using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Environment Variable", menuName="Ares/Environment Variable", order=31)]
	public class EnvironmentVariableData : ScriptableObject {
		public enum DurationType {Permanent, ConstantNumberOfTurns, RandomNumberOfTurns}
		public enum ProcessingMoment {Never, StartOfRound, EndOfRound, StartOfInstigatingActorTurn, EndOfInstigatingActorTurn}

		public string DisplayName {get{return displayName;}}
		public DoubleSetStageAction DoubleSetStageBehaviour {get{return doubleSetStageBehaviour;}}
		public DoubleSetDurationAction DoubleSetDurationBehaviour {get{return doubleSetDurationBehaviour;}}
		public DurationType DurationMode {get{return durationMode;}}
		public ProcessingMoment DurationProcessingMoment {get{return durationProcessingMoment;}}
		public ProcessingMoment EffectProcessingMoment {get{return effectProcessingMoment;}}
		public int MinStage {get{return minStage;}}
		public int MaxStage {get{return maxStage;}}
		public int MinDuration {get{return minDuration;}}
		public int MaxDuration {get{return maxDuration;}}
		public BattleInteractorModifier[] Filters {get{return filters;}}
		public InstantiationEffect SetInstantiation {get{return setInstantiation;}}
		public InstantiationEffect ProcessInstantiation {get{return processInstantiation;}}
		public InstantiationEffect FilterInstantiation {get{return filterInstantiation;}}
		public InstantiationEffect UnsetInstantiation {get{return unsetInstantiation;}}
		public AudioEffect SetAudio {get{return setAudio;}}
		public AudioEffect ProcessAudio {get{return processAudio;}}
		public AudioEffect FilterAudio {get{return filterAudio;}}
		public AudioEffect UnsetAudio {get{return unsetAudio;}}

		[SerializeField] string displayName;
		[SerializeField] DoubleSetStageAction doubleSetStageBehaviour;
		[SerializeField] DoubleSetDurationAction doubleSetDurationBehaviour;
		[SerializeField] DurationType durationMode;
		[SerializeField] ProcessingMoment durationProcessingMoment;
		[SerializeField] ProcessingMoment effectProcessingMoment;
		[SerializeField] int minStage = 0;
		[SerializeField] int maxStage = 5;
		[SerializeField] int minDuration = 1;
		[SerializeField] int maxDuration = 5;
		[SerializeField] BattleInteractorModifier[] filters;
		[SerializeField] InstantiationEffect setInstantiation;
		[SerializeField] InstantiationEffect processInstantiation;
		[SerializeField] InstantiationEffect filterInstantiation;
		[SerializeField] InstantiationEffect unsetInstantiation;
		[SerializeField] AudioEffect setAudio;
		[SerializeField] AudioEffect processAudio;
		[SerializeField] AudioEffect filterAudio;
		[SerializeField] AudioEffect unsetAudio;

		public virtual void OnSet(Battle battle, Actor setter){}
		public virtual void OnUnset(Battle battle, Actor setter){}
		public virtual void OnProcess(Battle battle, Actor setter){}
		public virtual void OnFilter(Battle battle, Actor setter){}
	}

	public class EnvironmentVariable {
		protected EnvironmentVariable(){} //only exists to allow inheritance

		public EnvironmentVariableData Data {get; private set;}
		public int TotalDuration {get; private set;}

		public int RoundsRemaining {
			get{
				return roundsRemaining;
			}
			set{
				roundsRemaining = Mathf.Max(value, -1);
			}
		}

		public int Stage {
			get{
				return stage;
			}
			set{
				stage = Mathf.Clamp(value, Data.MinStage, Data.MaxStage);
			}
		}

		public Actor Setter {get; private set;}
		int stage;
		int roundsRemaining;

		public EnvironmentVariable(EnvironmentVariableData data, int stage){
			Data = data;
			Stage = stage;
			
			if(data.DurationMode == EnvironmentVariableData.DurationType.ConstantNumberOfTurns){
				TotalDuration = roundsRemaining = Data.MinDuration;
			}
			else if(data.DurationMode == EnvironmentVariableData.DurationType.ConstantNumberOfTurns){
				TotalDuration = roundsRemaining = Random.Range(Data.MinDuration, Data.MaxDuration + 1);
			}
		}
		
		public virtual void OnSet(Battle battle, Actor caster){
			Setter = caster;
			Data.OnSet(battle, Setter);

			if(Data.SetInstantiation.enabled){
				Data.SetInstantiation.Trigger<EnvironmentVariable>(Setter, new Actor[]{Setter}, this);
			}

			if(Data.SetAudio.enabled){
				Data.SetAudio.Trigger(Setter, null);
			}
		}

		public virtual void OnProcess(Battle battle){
			Data.OnProcess(battle, Setter);

			if(Data.ProcessInstantiation.enabled){
				Data.ProcessInstantiation.Trigger<EnvironmentVariable>(Setter, new Actor[]{Setter}, this);
			}

			if(Data.ProcessAudio.enabled){
				Data.ProcessAudio.Trigger(Setter, null);
			}
		}

		public virtual void OnFilter(Battle battle, Actor caster, Actor target){
			Data.OnFilter(battle, Setter);

			if(Data.FilterInstantiation.enabled){
				Data.FilterInstantiation.Trigger<EnvironmentVariable>(caster, new Actor[]{target}, this);
			}

			if(Data.FilterAudio.enabled){
				Data.FilterAudio.Trigger(Setter, null);
			}
		}

		public virtual void OnUnset(Battle battle){
			Data.OnUnset(battle, Setter);

			if(Data.UnsetInstantiation.enabled){
				Data.UnsetInstantiation.Trigger<EnvironmentVariable>(Setter, new Actor[]{Setter}, this);
			}

			if(Data.UnsetAudio.enabled){
				Data.UnsetAudio.Trigger(Setter, null);
			}
		}

		public IEnumerable<Ability> FilterBlockedAbilities(IEnumerable<Ability> abilities, Actor caster){
			HashSet<Ability> blockedAbilities = new HashSet<Ability>();

			foreach(BattleInteractorModifier filter in Data.Filters){
				foreach(Ability ability in abilities){
					if(filter.IsBlocked(ability, caster, Setter)){
						blockedAbilities.Add(ability);
					}
				}
			}

			if(blockedAbilities.Count == 0){
				return abilities;
			}

			List<Ability> results = abilities.ToList();

			results.RemoveAll(a => blockedAbilities.Contains(a));

			return results;
		}

		public IEnumerable<Item> FilterBlockedItems(IEnumerable<Item> items, Actor user){
			HashSet<Item> blockedItems = new HashSet<Item>();

			foreach(BattleInteractorModifier filter in Data.Filters){
				foreach(Item item in items){
					if(filter.IsBlocked(item, user, Setter)){
						blockedItems.Add(item);
					}
				}
			}

			if(blockedItems.Count == 0){
				return items;
			}

			List<Item> results = items.ToList();

			results.RemoveAll(a => blockedItems.Contains(a));

			return results;
		}

		public virtual float Filter(object evaluater, ChainableAction action, Actor caster, Actor target, int power){
			float result = power;
			bool hasModified = false;

			foreach(BattleInteractorModifier filter in Data.Filters){
				if(filter.CanModify(evaluater, action, caster, Setter)){
					result = filter.ModifyPower(result);
					hasModified = true;
				}
			}

			if(hasModified){
				OnFilter(caster.Battle, caster, target);
			}

			return result;
		}
	}
}