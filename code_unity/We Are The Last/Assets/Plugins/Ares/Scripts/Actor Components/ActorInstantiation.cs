using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares.ActorComponents {
	[System.Serializable]
	public class ActorInstantiationAbilityElement : ActorAbilityCallbackElementBase {
		public InstantiationEffect Effect {get{return effect;}}

		public bool Enabled {get{return effect.enabled;} set{effect.enabled = value;}}// Convenience wrapper for Effect.Enabled

		[SerializeField] protected InstantiationEffect effect;

		#if UNITY_EDITOR
		public bool EditorShowExpanded {get{return editorShowExpanded;} set{editorShowExpanded = value;}}

		[SerializeField] bool editorShowExpanded = false;
		#endif

		public ActorInstantiationAbilityElement(){
			effect = new InstantiationEffect(null, null, false, Vector3.zero, Vector3.zero, Vector3.one);
		}
	}
	
	[System.Serializable]
	public class ActorInstantiationEventElement : ActorInstantiationAbilityElement {
		public EventCallbackType Type {get{return type;}}

		[EnumFlagsAttribute] public EventIgnoreFlags ignoreEvents;

		[SerializeField] EventCallbackType type;
		
		public ActorInstantiationEventElement(EventCallbackType type){
			this.type = type;
		}
	}
	
	[System.Serializable]
	public class ActorInstantiationItemElement : ActorInstantiationAbilityElement {
		public ItemData Item {get{return item;}}

		[SerializeField] ItemData item;

		public ActorInstantiationItemElement(ItemData data, InstantiationEffect effect){
			this.effect = effect;

			item = data;
		}
	}
	
	[System.Serializable]
	public class ActorInstantiationAfflictionElement : ActorInstantiationAbilityElement {
		public AfflictionData Affliction {get{return affliction;}}
		public AfflictionCallbackType Type {get{return type;}}

		[SerializeField] AfflictionData affliction;
		[SerializeField] AfflictionCallbackType type;

		public ActorInstantiationAfflictionElement(AfflictionData data, AfflictionCallbackType type, InstantiationEffect effect){
			this.type = type;
			this.effect = effect;

			affliction = data;
		}
	}

	[RequireComponent(typeof(Actor)), DisallowMultipleComponent, AddComponentMenu("Ares/Actor Instantiation", 21)]
	public class ActorInstantiation : ActorComponent {
		public ActorInstantiationEventElement[] EventCallbacks {get{return eventCallbacks;}}
		public ActorInstantiationAbilityElement[] AbilityCallbacks {get{return abilityCallbacks;}}
		public List<ActorInstantiationItemElement> ItemCallbacks {get{return itemCallbacks;}}
		public List<ActorInstantiationAfflictionElement> AfflictionCallbacks {get{return afflictionCallbacks;}}

		[SerializeField] bool allowDefaultAbilityInstantiations = true;
		[SerializeField] bool allowDefaultAfflictionInstantiations = true;
		[SerializeField] bool allowDefaultItemInstantiations = true;
		[SerializeField] ActorAbilityCallbackMode abilityCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;
		[SerializeField] ActorAbilityCallbackMode itemCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;
		[SerializeField] ActorAbilityCallbackMode afflictionCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;

		[SerializeField, HideInInspector] ActorInstantiationEventElement[] eventCallbacks;
		[SerializeField, HideInInspector] ActorInstantiationAbilityElement[] abilityCallbacks;
		[SerializeField, HideInInspector] List<ActorInstantiationItemElement> itemCallbacks;
		[SerializeField, HideInInspector] List<ActorInstantiationAfflictionElement> afflictionCallbacks;

		public void Init(bool allowDefaultAbilityInstantiations, ActorAbilityCallbackMode abilityCallbackMode, bool allowDefaultItemInstantiations,
		   ActorAbilityCallbackMode itemCallbackMode, bool allowDefaultAfflictionInstantiations, ActorAbilityCallbackMode afflictionCallbackMode){
			this.allowDefaultAbilityInstantiations = allowDefaultAbilityInstantiations;
			this.abilityCallbackMode = abilityCallbackMode;
			this.allowDefaultItemInstantiations = allowDefaultItemInstantiations;
			this.itemCallbackMode = itemCallbackMode;
			this.allowDefaultAfflictionInstantiations = allowDefaultAfflictionInstantiations;
			this.afflictionCallbackMode = afflictionCallbackMode;

			Reset();
		}

		protected override void Reset(){
			base.Reset();

			abilityCallbacks = new ActorInstantiationAbilityElement[Actor.Abilities.Length];
			afflictionCallbacks = new List<ActorInstantiationAfflictionElement>();
			itemCallbacks = new List<ActorInstantiationItemElement>();

			for(int i=0; i<Actor.Abilities.Length; i++){
				abilityCallbacks[i] = new ActorInstantiationAbilityElement();
			}

			SetupEventCallbacks();
		}

		void Start(){
			Actor.OnHPChange.AddListener(newHP => {
				if(newHP < Actor.HP){
					ProcessCallbackElement(EventCallbackType.TakeDamage, Actor);
				}
				else if(Actor.HP == 0){
					ProcessCallbackElement(EventCallbackType.Revive, Actor);
				}
				else{
					ProcessCallbackElement(EventCallbackType.Heal, Actor);
				}
			});

			Actor.OnStatBuff.AddListener((stat, newStage) => {
				ProcessCallbackElement(EventCallbackType.BuffStat, Actor);
			});

			Actor.OnStatDebuff.AddListener((stat, newStage) => {
				ProcessCallbackElement(EventCallbackType.DebuffStat, Actor);
			});

			Actor.OnAbilityActionAvoid.AddListener((caster, abilityInfo, action) => {
				ProcessCallbackElement(EventCallbackType.AvoidAbility, Actor);
			});

			Actor.OnHPDeplete.AddListener(() => {
				ProcessCallbackElement(EventCallbackType.Die, Actor);
			});

			Actor.OnAbilityStart.AddListener((ability, targets) => {
				ability.ConsumeInstantiationUseEvent();
				ProcessCallbackElement(abilityCallbacks[abilityIndices[ability]], ability.Data.Instantiation, Actor, targets, ability.Data);
			});

			Actor.OnAfflictionObtain.AddListener(affliction => {
				ActorInstantiationAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.Obtain));

				affliction.ConsumeInstantiationObtainEvent();
				ProcessCallbackElement(callback, affliction.Data.ObtainInstantiation, Actor, affliction.Afflicter, affliction.Data);
			});

			Actor.OnAfflictionStart.AddListener(affliction => {
				ActorInstantiationAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.Trigger));

				affliction.ConsumeInstantiationTriggerEvent();
				ProcessCallbackElement(callback, affliction.Data.TriggerInstantiation, Actor, affliction.Afflicter, affliction.Data);
			});

			Actor.OnAfflictionStageChange.AddListener((affliction, newStage) => {
				if(newStage > affliction.Stage){
					ActorInstantiationAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
						(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.StageIncrease));

					affliction.ConsumeInstantiationStageIncreaseEvent();
					ProcessCallbackElement(callback, affliction.Data.StageIncreaseInstantiation, Actor, affliction.Afflicter, affliction.Data);
				}
				else{
					ActorInstantiationAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
						(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.StageDecrease));

					affliction.ConsumeInstantiationStageDecreaseEvent();
					ProcessCallbackElement(callback, affliction.Data.StageDecreaseInstantiation, Actor, affliction.Afflicter, affliction.Data);
				}
			});

			Actor.OnAfflictionEnd.AddListener(affliction => {
				ActorInstantiationAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.End));

				affliction.ConsumeInstantiationEndEvent(); 
				ProcessCallbackElement(callback, affliction.Data.EndInstantiation, Actor, affliction.Afflicter, affliction.Data);
			});

			Actor.OnItemStart.AddListener((item, targets) => {
				item.ConsumeInstantiationUseEvent();
				ProcessCallbackElement(itemCallbacks.FirstOrDefault(c => c.Item == item.Data), item.Data.Instantiation, Actor, targets, item.Data);
			});

			Actor.OnAbilityPreparationStart.AddListener((ability, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Preparation.Instantiations[0], Actor, null, ability.Data);
			});

			Actor.OnAbilityPreparationUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Preparation.Instantiations[ability.Data.Preparation.Turns - turnsRemaining], Actor, null, ability.Data);
			});

			Actor.OnAbilityRecoveryStart.AddListener(ability => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Recovery.Instantiations[0], Actor, null, ability.Data);
			});

			Actor.OnAbilityRecoveryUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Recovery.Instantiations[ability.Data.Recovery.Turns - turnsRemaining], Actor, null, ability.Data);
			});

			Actor.OnItemPreparationStart.AddListener((item, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Preparation.Instantiations[0], Actor, null, item.Data);
			});

			Actor.OnItemPreparationUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Preparation.Instantiations[item.Data.Preparation.Turns - turnsRemaining], Actor, null, item.Data);
			});

			Actor.OnItemRecoveryStart.AddListener(item => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Recovery.Instantiations[0], Actor, null, item.Data);
			});

			Actor.OnItemRecoveryUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Recovery.Instantiations[item.Data.Recovery.Turns - turnsRemaining], Actor, null, item.Data);
			});
		}

		public override void SetupEventCallbacks(){
			if(eventCallbacks == null){
				eventCallbacks = new ActorInstantiationEventElement[0];
			}

			SetupEventCallbacks<ActorInstantiationEventElement>(ref eventCallbacks, eventCallbacks.Select(c => c.Type).ToArray(), t => new ActorInstantiationEventElement(t));
		}

		public ActorInstantiationEventElement GetEventCallback(EventCallbackType callbackType){
			return eventCallbacks.FirstOrDefault(c => c.Type == callbackType);
		}

		public ActorInstantiationAbilityElement GetAbilityCallback(Ability ability){
			return GetAbilityCallback(ability.Data);
		}

		public ActorInstantiationAbilityElement GetAbilityCallback(AbilityData data){
			Ability ability = Actor.Abilities.FirstOrDefault(a => a.Data == data);

			if(ability == null && Actor.FallbackAbility.Data == data){
				ability = Actor.FallbackAbility;
			}

			return ability == null ? null : abilityCallbacks[abilityIndices[ability]];
		}

		public ActorInstantiationItemElement GetItemCallback(ItemData data){
			return itemCallbacks.FirstOrDefault(c => c.Item == data);
		}

		public ActorInstantiationItemElement[] GetItemCallbacks(ItemData data){
			return itemCallbacks.Where(c => c.Item == data).ToArray();
		}

		public ActorInstantiationAfflictionElement GetAfflictionCallback(AfflictionData data){
			return afflictionCallbacks.FirstOrDefault(c => c.Affliction == data);
		}

		public ActorInstantiationAfflictionElement[] GetAfflictionCallbacks(AfflictionData data){
			return afflictionCallbacks.Where(c => c.Affliction == data).ToArray();
		}

		public ActorInstantiationItemElement AddItemCallback(ItemData data, InstantiationEffect effect){
			ActorInstantiationItemElement callbackElement = new ActorInstantiationItemElement(data, effect);

			itemCallbacks.Add(callbackElement);

			return callbackElement;
		}

		public ActorInstantiationAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, InstantiationEffect effect){
			ActorInstantiationAfflictionElement callbackElement = new ActorInstantiationAfflictionElement(data, type, effect);

			afflictionCallbacks.Add(callbackElement);

			return callbackElement;
		}
		
		public override void OnAbilityAdded(){
			ActorInstantiationAbilityElement newElement = new ActorInstantiationAbilityElement();

			OnAbilityAdded(ref abilityCallbacks, newElement);
		}
		
		public override void OnAbilityRemoved(int index){
			OnAbilityRemoved(ref abilityCallbacks, index);
		}
		
		public override void OnAbilityMoved(int from, int to){
			OnAbilityMoved(ref abilityCallbacks, from, to);
		}

		void ProcessCallbackElement(EventCallbackType eventType, Actor actor){
			ActorInstantiationEventElement callback = eventCallbacks[(int)eventType];

			if(callback.Effect == null || !callback.Effect.enabled || ShouldIgnore(eventType, callback.ignoreEvents)){
				return;
			}

			callback.Effect.Trigger<EventCallbackType>(actor, new Actor[]{actor}, eventType);
		}

		void ProcessCallbackElement(ActorInstantiationAfflictionElement callback, InstantiationEffect defaultEffect, Actor actor, Actor afflicter, AfflictionData affliction){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultAfflictionInstantiations && defaultEffect.enabled && (afflictionCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(afflicter, new Actor[]{actor}, affliction);
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger<AfflictionData>(afflicter, new Actor[]{actor}, affliction);
		}

		void ProcessCallbackElement(ActorInstantiationItemElement callback, InstantiationEffect defaultEffect, Actor actor, Actor[] targets, ItemData item){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultItemInstantiations && defaultEffect.enabled && (itemCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(actor, new Actor[]{actor}, item);
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger<ItemData>(actor, targets, item);
		}

		void ProcessCallbackElement(ActorInstantiationAbilityElement callback, InstantiationEffect defaultEffect, Actor actor, Actor[] targets, AbilityData ability){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultAbilityInstantiations && defaultEffect.enabled && (abilityCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(actor, targets, ability);
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger<AbilityData>(actor, targets, ability);
		}

		[ContextMenu("Fix Missing Fallback Ability")]
		void FixMissingFallbackAbility(){
			FixMissingFallbackAbility<ActorInstantiationAbilityElement>(ref abilityCallbacks);
		}
	}
}