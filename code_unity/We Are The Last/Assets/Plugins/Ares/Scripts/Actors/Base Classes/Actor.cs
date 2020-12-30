using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Ares.Extensions;

namespace Ares {
	public abstract class Actor : MonoBehaviour {
		//Events
		public IntEvent OnHPChange {get; private set;}
		public UnityEvent OnHPDeplete {get; private set;}

		public Ability_StringEvent OnAbilityPreparationStart {get; private set;}
		public Ability_Int_StringEvent OnAbilityPreparationUpdate {get; private set;}
		public Ability_BoolEvent OnAbilityPreparationEnd {get; private set;}

		public AbilityEvent OnAbilityRecoveryStart {get; private set;}
		public Ability_Int_StringEvent OnAbilityRecoveryUpdate {get; private set;}
		public Ability_BoolEvent OnAbilityRecoveryEnd {get; private set;}

		public Ability_ActorsEvent OnAbilityStart {get; private set;}
		public Actors_Ability_AbilityActionEvent OnAbilityActionProcess {get; private set;}
		public Actors_Ability_AbilityActionEvent OnAbilityActionEnd {get; private set;}
		public Actors_Ability_AbilityActionEvent OnAbilityActionMiss {get; private set;}
		public Actor_Ability_AbilityActionEvent OnAbilityActionAvoid {get; private set;}
		public Ability_ActorsEvent OnAbilityEnd {get; private set;}

		public Item_StringEvent OnItemPreparationStart {get; private set;}
		public Item_Int_StringEvent OnItemPreparationUpdate {get; private set;}
		public Item_BoolEvent OnItemPreparationEnd {get; private set;}

		public ItemEvent OnItemRecoveryStart {get; private set;}
		public Item_Int_StringEvent OnItemRecoveryUpdate {get; private set;}
		public Item_BoolEvent OnItemRecoveryEnd {get; private set;}

		public Item_ActorsEvent OnItemStart {get; private set;}
		public Actors_Item_ItemActionEvent OnItemActionProcess {get; private set;}
		public Actors_Item_ItemActionEvent OnItemActionEnd {get; private set;}
		public Actors_Item_ItemActionEvent OnItemActionMiss {get; private set;}
		public Actor_Item_ItemActionEvent OnItemActionAvoid {get; private set;}
		public Item_ActorsEvent OnItemEnd {get; private set;}

		public AfflictionEvent OnAfflictionObtain {get; private set;}
		public AfflictionEvent OnAfflictionStart {get; private set;}
		public Affliction_IntEvent OnAfflictionStageChange {get; private set;}
		public Affliction_IntEvent OnAfflictionDurationChange {get; private set;}
		public Affliction_AfflictionActionEvent OnAfflictionActionProcess {get; private set;}
		public Affliction_AfflictionActionEvent OnAfflictionActionEnd {get; private set;}
		public Affliction_AfflictionActionEvent OnAfflictionActionAvoid {get; private set;}
		public AfflictionEvent OnAfflictionEnd {get; private set;}
		public AfflictionEvent OnAfflictionCure {get; private set;}

		public Stat_IntEvent OnStatBuff {get; private set;}
		public Stat_IntEvent OnStatDebuff {get; private set;}
		
		//Getters for serialized fields (to keep Editor UI functionality)
		public string DisplayName {get{return displayName;}}
		public int MaxHP {get{return maxHP;}}
		public Ability[] Abilities {get{return abilities;}}
		public HashSet<Affliction> Afflictions {get{return afflictions;}}
		public List<TemporaryBuff> TemporaryBuffs {get{return temporaryBuffs;}}
		public Ability FallbackAbility {get{return fallbackAbility;}}
		public Dictionary<string, Stat> Stats {get{return statsMap;}}
		public bool IsCastingAbility {get; private set;}
		public bool IsUsingItem {get; private set;}
		public bool IsProcessingOwnAffliction {get; private set;}

		public int HP {
			get{
				return hp;
			}
			private set{
				int newHp = Mathf.Clamp (value, 0, maxHP);

				OnHPChange.Invoke(newHp);

				hp = newHp;

				if(hp == 0){
					Afflictions.RemoveWhere(a => a.Data.CureOnAfflictedDeath);

					OnHPDeplete.Invoke();
				}
			}
		}
		
		//Misc. properties
		public Battle Battle {get; private set;} //Only used as a convenience reference
		public BattleGroup Group {get{return Battle.ActorInfo[this].Group;}} //Only used as a convenience reference

		//Public fields
		public Inventory inventory;

		//Actual fields for Editor UI
		[SerializeField] string displayName = "Actor Name";
		[SerializeField] int hp;
		[SerializeField] int maxHP;
		[SerializeField] Ability[] abilities;
		[SerializeField] Ability fallbackAbility;
		[SerializeField] List<Stat> stats;

		Dictionary<string, Stat> statsMap;
		HashSet<Affliction> afflictions;
		List<TemporaryBuff> temporaryBuffs;
		List<DelayRequest> abilityEndDelays;
		HashSet<BattleDelayElement> abilityEndDelayLocks;

		#if UNITY_EDITOR
		void OnValidate(){
			maxHP = Mathf.Max(0, maxHP);
			hp = Mathf.Clamp(hp, 0, maxHP);
		}
		
		void Reset(){
			hp = maxHP = 100;
			abilities = new Ability[0];
		}
		#endif

		void Awake(){
			OnHPChange = new IntEvent();
			OnHPDeplete = new UnityEvent();

			OnAbilityPreparationStart = new Ability_StringEvent();
			OnAbilityPreparationUpdate = new Ability_Int_StringEvent();
			OnAbilityPreparationEnd = new Ability_BoolEvent();

			OnAbilityRecoveryStart = new AbilityEvent();
			OnAbilityRecoveryUpdate = new Ability_Int_StringEvent();
			OnAbilityRecoveryEnd = new Ability_BoolEvent();

			OnAbilityStart = new Ability_ActorsEvent();
			OnAbilityActionProcess = new Actors_Ability_AbilityActionEvent();
			OnAbilityActionEnd = new Actors_Ability_AbilityActionEvent();
			OnAbilityActionMiss = new Actors_Ability_AbilityActionEvent();
			OnAbilityActionAvoid = new Actor_Ability_AbilityActionEvent();
			OnAbilityEnd = new Ability_ActorsEvent();

			OnItemPreparationStart = new Item_StringEvent();
			OnItemPreparationUpdate = new Item_Int_StringEvent();
			OnItemPreparationEnd = new Item_BoolEvent();

			OnItemRecoveryStart = new ItemEvent();
			OnItemRecoveryUpdate = new Item_Int_StringEvent();
			OnItemRecoveryEnd = new Item_BoolEvent();

			OnItemStart = new Item_ActorsEvent();
			OnItemActionProcess = new Actors_Item_ItemActionEvent();
			OnItemActionEnd = new Actors_Item_ItemActionEvent();
			OnItemActionMiss = new Actors_Item_ItemActionEvent();
			OnItemActionAvoid = new Actor_Item_ItemActionEvent();
			OnItemEnd = new Item_ActorsEvent();

			OnAfflictionObtain = new AfflictionEvent();
			OnAfflictionStart = new AfflictionEvent();
			OnAfflictionStageChange = new Affliction_IntEvent();
			OnAfflictionDurationChange = new Affliction_IntEvent();
			OnAfflictionActionProcess = new Affliction_AfflictionActionEvent();
			OnAfflictionActionEnd = new Affliction_AfflictionActionEvent();
			OnAfflictionActionAvoid = new Affliction_AfflictionActionEvent();
			OnAfflictionEnd = new AfflictionEvent();
			OnAfflictionCure = new AfflictionEvent();

			OnStatBuff = new Stat_IntEvent();
			OnStatDebuff = new Stat_IntEvent();

			afflictions = new HashSet<Affliction>();
			temporaryBuffs = new List<TemporaryBuff>();

			abilityEndDelays = new List<DelayRequest>();
			abilityEndDelayLocks = new HashSet<BattleDelayElement>();

			OnAbilityStart.AddListener((a, t) => {IsCastingAbility = true;});
			OnAbilityEnd.AddListener((a, t) => {IsCastingAbility = false;});
			OnItemStart.AddListener((a, t) => {IsUsingItem = true;});
			OnItemEnd.AddListener((a, t) => {IsUsingItem = false;});
			OnAfflictionStart.AddListener(a => {IsProcessingOwnAffliction = a.Afflicter == this;});
			OnAfflictionEnd.AddListener(a => {IsProcessingOwnAffliction = false;});

			OnAbilityPreparationEnd.AddListener((a, i) => {if(i){a.preparationTurnsRemaining = 0;}});
			OnAbilityRecoveryEnd.AddListener((a, i) => {if(i){a.recoveryTurnsRemaining = 0;}});
			OnItemPreparationEnd.AddListener((it, i) => {if(i){it.preparationTurnsRemaining = 0;}});
			OnItemRecoveryEnd.AddListener((it, i) => {if(i){it.recoveryTurnsRemaining = 0;}});

			SetupStats();
		}

		public void SetupStats(bool sort = false){
			statsMap = new Dictionary<string, Stat>(System.StringComparer.OrdinalIgnoreCase);

			if(stats == null){
				stats = new List<Stat>();
			}

			stats.RemoveAll(s => s.Data == null);

			foreach(StatData statData in StatData.All){
				bool needsAdding = true;

				foreach(Stat stat in stats){
					if(stat.Data == statData){
						statsMap.Add(stat.Data.name, stat);

						needsAdding = false;
						break;
					}
				}

				if(needsAdding){
					Stat stat = new Stat(statData);

					statsMap.Add(stat.Data.name, stat);
					stats.Add(stat);
				}
			}

			if(sort){
				stats.OrderBy(s => s);
			}
		}

		public void Init(string displayName, int hp, int maxHP, Dictionary<string, int> stats, Ability[] abilities,
		                 Ability fallbackAbility, HashSet<Affliction> afflictions, Inventory inventory){
			this.displayName = displayName;
			this.hp = hp;
			this.maxHP = maxHP;
			this.fallbackAbility = fallbackAbility;
			this.abilities = abilities == null ? new Ability[0] : abilities;
			this.afflictions = afflictions == null ? new HashSet<Affliction>() : afflictions;
			this.inventory = inventory;

			foreach(KeyValuePair<string, int> statInfo in stats){
				Stats[statInfo.Key].baseValue = statInfo.Value;
			}
		}

		public void LinkToBattle(Battle battle){
			Battle = battle;
		}

		public int TakeDamage(int power){
			int oldHP = HP;

			HP -= Mathf.Max(0, power);

			return oldHP - HP;
		}

		public int Heal(int power){
			int oldHP = HP;

			HP += Mathf.Max(0, power);

			return HP - oldHP;
		}
		
		public int BuffStat(StatData statData, int stages){
			return BuffStat(Stats[statData.name], stages);
		}

		public int BuffStat(Stat stat, int stages){
			int oldStage = stat.Stage;

			if(stages > 0){
				OnStatBuff.Invoke(stat, Mathf.Min(stat.Stage + stages, stat.Data.MaxStage));
				stat.Buff(stages);
			}
			else{
				OnStatDebuff.Invoke(stat, Mathf.Max(stat.Stage - stages, stat.Data.MinStage));
				stat.Debuff(-stages);
			}

			return stat.Stage - oldStage;
		}

		public int Afflict(AfflictionData afflictionData, int stage, int currentBattleTurn, Actor inflicter){
			Affliction affliction = afflictions.FirstOrDefault(a => a.Data == afflictionData);

			if(affliction == null){
				affliction = new Affliction(afflictionData, stage, currentBattleTurn, inflicter);

				afflictions.Add(affliction);
				OnAfflictionObtain.Invoke(affliction);
				affliction.OnObtain(inflicter, this);

				return 1;
			}

			switch(affliction.Data.DoubleSetDurationBehaviour){
				case DoubleSetDurationAction.IncreaseByOne:
					OnAfflictionDurationChange.Invoke(affliction, 1);

					affliction.RoundsRemaining++;

					break;
				case DoubleSetDurationAction.ResetToStart:
					if(affliction.RoundInflicted != Battle.CurrentRound){
						OnAfflictionDurationChange.Invoke(affliction, Battle.CurrentRound - affliction.RoundInflicted);
						
						affliction.ResetEndTurn(Battle.CurrentRound);
					}
					break;
			}

			switch(affliction.Data.DoubleSetStageBehaviour){
				case DoubleSetStageAction.IncreaseByOne:
					if(affliction.Stage < affliction.Data.MaxStage){
						OnAfflictionStageChange.Invoke(affliction, 1);

						affliction.Stage++;

						return 1;
					}
					break;
				case DoubleSetStageAction.ModifyByValue:
					int newStage = Mathf.Clamp(affliction.Stage + stage, affliction.Data.MinStage, affliction.Data.MaxStage);

					if(newStage != affliction.Stage){
						OnAfflictionStageChange.Invoke(affliction, newStage - affliction.Stage);

						affliction.Stage += stage;

						return stage;
					}
					break;
				case DoubleSetStageAction.SetToValue:
					if(stage != affliction.Stage){
						OnAfflictionStageChange.Invoke(affliction, stage - affliction.Stage);

						affliction.Stage = stage;

						return stage - affliction.Stage;
					}
					break;
			}

			return 0;
		}

		public int Cure(AfflictionData afflictionData, int stages){
			return Cure(afflictions.FirstOrDefault(a => a.Data == afflictionData), stages);
		}

		public int Cure(Affliction affliction, int stages){
			if(affliction != null){
				if(stages == -1){
					OnAfflictionCure.Invoke(affliction);
					affliction.OnEnd(this);
					afflictions.Remove(affliction);

					return affliction.Stage;
				}
				else{
					int delta = Mathf.Max(0, stages);

					if(delta == 0){
						return 0;
					}

					int oldStage = affliction.Stage;
					int newStage = Mathf.Max(affliction.Stage - delta, affliction.Data.MinStage);

					OnAfflictionStageChange.Invoke(affliction, newStage);

					if(newStage > oldStage){
						affliction.OnStageIncrease(this);
					}
					else{
						affliction.OnStageDecrease(this);
					}

					affliction.Stage -= delta;

					if(affliction.Stage < affliction.Data.MinStage){
						OnAfflictionCure.Invoke(affliction);
						affliction.OnEnd(this);
						afflictions.Remove(affliction);
					}

					return oldStage - affliction.Stage;
				}

			}

			return 0;
		}

		public int Worsen(AfflictionData afflictionData, int stages){
			return Worsen(afflictions.FirstOrDefault(a => a.Data == afflictionData), stages);
		}

		public int Worsen(Affliction affliction, int stages){
			if(affliction != null){
				if(stages < 1){
					return 0;
				}
				else{
					int delta = Mathf.Max(0, stages);
					int oldStage = affliction.Stage;
					int newStage = Mathf.Min(affliction.Stage + delta, affliction.Data.MaxStage);

					OnAfflictionStageChange.Invoke(affliction, newStage);

					if(newStage > oldStage){
						affliction.OnStageIncrease(this);
					}
					else{
						affliction.OnStageDecrease(this);
					}

					affliction.Stage += delta;

					return oldStage - affliction.Stage;
				}

			}

			return 0;
		}

		public BattleInteractorData.HitStatus PerformHitTest(Actor caster, AbilityData ability, AbilityAction action){
			//Speed, evasiveness, type immunity, etc. can be incorporated here

			if(Random.Range(0f, 1f) <= action.HitChance){
				return BattleInteractorData.HitStatus.Hit;
			}

			return BattleInteractorData.HitStatus.Evade;
		}

		public BattleInteractorData.HitStatus PerformHitTest(Actor caster, ItemData item, AbilityAction action){
			//Speed, evasiveness, item immunity, etc. can be incorporated here

			if(Random.Range(0f, 1f) <= action.HitChance){
				return BattleInteractorData.HitStatus.Hit;
			}

			return BattleInteractorData.HitStatus.Evade;
		}

		public BattleInteractorData.HitStatus PerformHitTest(Actor inflicter, AfflictionData affliction, AfflictionAction action){
			//Special evade effects etc. can be incorporated here

			if(Random.Range(0f, 1f) <= action.HitChance){
				return BattleInteractorData.HitStatus.Hit;
			}

			return BattleInteractorData.HitStatus.Evade;
		}

		//Ability confirmations
		public void ConfirmAbilityActionSuccess(Actor[] targets, Ability ability, AbilityAction action){//, System.Action<Actor, Ability, AbilityAction> onProcessCallback){
			VerboseLogger.Log(string.Format("{0} succesfully landed ability {1}'s {2} action on {3}", DisplayName, ability.Data.DisplayName, action.Action,
				string.Join(",", targets.Select(t => t.displayName).ToArray())));

			StartCoroutine(ScheduleOnAbilityActionProcessedAndEnded(targets, ability, action));
		}

		public void ConfirmAbilityActionFail(Actor[] targets, Ability ability, BattleInteractorData.HitStatus[] hitStatuses, AbilityAction action, bool canEndAbility){
			VerboseLogger.Log(string.Format("{0} did not land ability {1}'s {2} action on {3}", DisplayName, ability.Data.DisplayName, action.Action,
				string.Join(",", targets.Select(t => t.displayName).AresZip(hitStatuses, (t, h) => string.Format("{0} ({1})", t, h)).ToArray())));
			
			OnAbilityActionMiss.Invoke(targets, ability, action);
			OnAbilityActionEnd.Invoke(targets, ability, action);

			if(action.BreaksChainOnMiss && canEndAbility){
				OnAbilityEnd.Invoke(ability, targets);
			}
		}

		public void ConfirmAbilityActionAvoid(Actor caster, Ability ability, BattleInteractorData.HitStatus hitStatus, AbilityAction action){
			VerboseLogger.Log(string.Format("{0} avoided ability {1}'s {2} action from {3}", DisplayName, ability.Data.DisplayName, action.Action, caster.DisplayName));
			
			OnAbilityActionAvoid.Invoke(caster, ability, action);
		}

		//Item confirmations
		public void ConfirmItemActionSuccess(Actor[] targets, Item item, ItemAction action){//, System.Action<Actor, Ability, AbilityAction> onProcessCallback){
			VerboseLogger.Log(string.Format("{0} succesfully landed item {1}'s {2} action on {3}", DisplayName, item.Data.DisplayName, action.Action,
				string.Join(",", targets.Select(t => t.displayName).ToArray())));
			
			StartCoroutine(ScheduleOnItemActionProcessedAndEnded(targets, item, action));
		}
		
		public void ConfirmItemActionFail(Actor[] targets, Item item, BattleInteractorData.HitStatus[] hitStatuses, ItemAction action, bool canEndItem){
			VerboseLogger.Log(string.Format("{0} did not land item {1}'s {2} action on {3} (Hit status: x)", DisplayName, item.Data.DisplayName, action.Action,
				string.Join(",", targets.Select(t => t.displayName).AresZip(hitStatuses, (t, h) => string.Format("{0} ({1})", t, h)).ToArray())));
			
			OnItemActionMiss.Invoke(targets, item, action);
			OnItemActionEnd.Invoke(targets, item, action);

			if (action.BreaksChainOnMiss && canEndItem){
				OnItemEnd.Invoke(item, targets);
			}
		}

		public void ConfirmItemActionAvoid(Actor caster, Item item, BattleInteractorData.HitStatus hitStatus, ItemAction action){
			VerboseLogger.Log(string.Format("{0} avoided item {1}'s {2} action from {3}", DisplayName, item.Data.DisplayName, action.Action, caster.DisplayName));

			OnItemActionAvoid.Invoke(caster, item, action);
		}
		
		//Affliction confirmations
		public void ConfirmAfflictionActionSuccess(Affliction affliction, AfflictionAction action){
			VerboseLogger.Log(string.Format("{0} succesfully got afflicted by {1} ({2})", DisplayName, affliction.Data.DisplayName, action.Action));

			StartCoroutine(ScheduleOnAfflictionActionProcessedAndEnded(affliction, action));
		}

		public void ConfirmAfflictionActionFail(Affliction affliction, BattleInteractorData.HitStatus[] hitStatus, AfflictionAction action, bool canEndAffliction){
			VerboseLogger.Log(string.Format("{0} avoided afflicted action {1}", DisplayName, affliction.Data.DisplayName));

			OnAfflictionActionAvoid.Invoke(affliction, action);
			OnAfflictionActionEnd.Invoke(affliction, action); //V1.3

			if(action.BreaksChainOnMiss && canEndAffliction){
				OnAfflictionEnd.Invoke(affliction);
			}
		}

		public void AddAbilityEndDelay(float delay){
			abilityEndDelays.Add(new DelayRequest(delay));
		}

		public void AddAbilityEndDelayLock(BattleDelayElement requestor){
			abilityEndDelayLocks.Add(requestor);
		}

		public void ReleaseAbilityEndDelayLock(BattleDelayElement requestor){
			abilityEndDelayLocks.Remove(requestor);
		}
		
		IEnumerator ScheduleOnAbilityActionProcessedAndEnded(Actor[] targets, Ability ability, AbilityAction action){ //TODO: Move these to Battle.cs?
			yield return new WaitForSeconds(action.Duration * action.NormalizedProcessTime);
			yield return null;

			OnAbilityActionProcess.Invoke(targets, ability, action);

			yield return new WaitForSeconds(action.Duration * (1f - action.NormalizedProcessTime));

			float startTime = Time.time;
			float maxDelay = abilityEndDelays.Count > 0 ? abilityEndDelays.Max(d => d.TimeRemaining) : 0f;

			while(maxDelay > 0 || abilityEndDelayLocks.Count > 0){
				abilityEndDelays.RemoveAll(d => d.TimeRemaining == 0f);
				maxDelay = abilityEndDelays.Count > 0 ? abilityEndDelays.Max(d => d.TimeRemaining) : 0f;

				yield return null;
			}

			OnAbilityActionEnd.Invoke(targets, ability, action);

			if(ability.Data.Actions[ability.Data.Actions.Count - 1] == action){
				OnAbilityEnd.Invoke(ability, targets);
			}
		}

		IEnumerator ScheduleOnItemActionProcessedAndEnded(Actor[] targets, Item item, ItemAction action){
			yield return new WaitForSeconds(action.Duration * action.NormalizedProcessTime);
			yield return null;

			OnItemActionProcess.Invoke(targets, item, action);

			yield return new WaitForSeconds(action.Duration * (1f - action.NormalizedProcessTime));

			OnItemActionEnd.Invoke(targets, item, action);

			if(item.Data.Actions[item.Data.Actions.Count - 1] == action){
				OnItemEnd.Invoke(item, targets);
			}
		}

		IEnumerator ScheduleOnAfflictionActionProcessedAndEnded(Affliction affliction, AfflictionAction action){
			yield return new WaitForSeconds(action.Duration * action.NormalizedProcessTime);

			OnAfflictionActionProcess.Invoke(affliction, action);

			yield return new WaitForSeconds(action.Duration * (1f - action.NormalizedProcessTime));

			OnAfflictionActionEnd.Invoke(affliction, action);

			if(affliction.Data.Actions[affliction.Data.Actions.Count - 1] == action){
				OnAfflictionEnd.Invoke(affliction);
			}
		}
	}
}