using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Ares.ActorComponents {
	[System.Serializable]
	public class ActorAudioAbilityElement : ActorAbilityCallbackElementBase {
		public AudioEffect Effect {get{return effect;}}
		public bool Enabled {get{return effect.enabled;} set{effect.enabled = value;}}// Convenience wrapper for Effect.Enabled

		[SerializeField] protected AudioEffect effect;

		#if UNITY_EDITOR
		public bool EditorShowExpanded {get{return editorShowExpanded;} set{editorShowExpanded = value;}}

		[SerializeField] bool editorShowExpanded = false;
		#endif

		public ActorAudioAbilityElement(){
			effect = new AudioEffect(null, AudioPlayPosition.MainCamera, 1f);
		}
	}
	
	[System.Serializable]
	public class ActorAudioEventElement : ActorAudioAbilityElement {
		public EventCallbackType Type {get{return type;}}

		[EnumFlagsAttribute] public EventIgnoreFlags ignoreEvents;
		[SerializeField] EventCallbackType type;
		
		public ActorAudioEventElement(EventCallbackType type){
			this.type = type;
		}
	}
	
	[System.Serializable]
	public class ActorAudioItemElement : ActorAudioAbilityElement {
		public ItemData Item {get{return item;}}
		
		[SerializeField] ItemData item;

		public ActorAudioItemElement(ItemData data, AudioEffect effect){
			this.effect = effect;

			item = data;
		}
	}
	
	[System.Serializable]
	public class ActorAudioAfflictionElement : ActorAudioAbilityElement {
		public AfflictionData Affliction {get{return affliction;}}
		public AfflictionCallbackType Type {get{return type;}}

		[SerializeField] AfflictionData affliction;
		[SerializeField] AfflictionCallbackType type;

		public ActorAudioAfflictionElement(AfflictionData data, AfflictionCallbackType type, AudioEffect effect){
			this.type = type;
			this.effect = effect;

			affliction = data;
		}
	}

	[RequireComponent(typeof(Actor)), DisallowMultipleComponent, AddComponentMenu("Ares/Actor Audio", 22)]
	public class ActorAudio : ActorComponent {
		public ActorAudioAbilityElement[] AbilityCallbacks {get{return abilityCallbacks;}}
		public ActorAudioEventElement[] EventCallbacks {get{return eventCallbacks;}}
		public List<ActorAudioItemElement> ItemCallbacks {get{return itemCallbacks;}}
		public List<ActorAudioAfflictionElement> AfflictionCallbacks {get{return afflictionCallbacks;}}

		[SerializeField] bool allowDefaultAbilityAudio = true;
		[SerializeField] bool allowDefaultItemAudio = true;
		[SerializeField] bool allowDefaultAfflictionAudio = true;
		[SerializeField] ActorAbilityCallbackMode abilityCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;
		[SerializeField] ActorAbilityCallbackMode itemCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;
		[SerializeField] ActorAbilityCallbackMode afflictionCallbackMode = ActorAbilityCallbackMode.AlongsideDefault;

		[SerializeField, HideInInspector] ActorAudioAbilityElement[] abilityCallbacks;
		[SerializeField, HideInInspector] ActorAudioEventElement[] eventCallbacks;
		[SerializeField, HideInInspector] List<ActorAudioItemElement> itemCallbacks;
		[SerializeField, HideInInspector] List<ActorAudioAfflictionElement> afflictionCallbacks;

		public void Init(bool allowDefaultAbilityAudio, ActorAbilityCallbackMode abilityCallbackMode, bool allowDefaultItemAudio,
			ActorAbilityCallbackMode itemCallbackMode, bool allowDefaultAfflictionAudio, ActorAbilityCallbackMode afflictionCallbackMode){
			this.allowDefaultAbilityAudio = allowDefaultAbilityAudio;
			this.abilityCallbackMode = abilityCallbackMode;
			this.allowDefaultItemAudio = allowDefaultItemAudio;
			this.itemCallbackMode = itemCallbackMode;
			this.allowDefaultAfflictionAudio = allowDefaultAfflictionAudio;
			this.afflictionCallbackMode = afflictionCallbackMode;

			Reset();
		}

		protected override void Reset(){
			base.Reset();
			
			abilityCallbacks = new ActorAudioAbilityElement[Actor.Abilities.Length + 1];
			afflictionCallbacks = new List<ActorAudioAfflictionElement>();
			itemCallbacks = new List<ActorAudioItemElement>();

			for(int i=0; i<Actor.Abilities.Length; i++){
				abilityCallbacks[i] = new ActorAudioAbilityElement();
			}

			abilityCallbacks[abilityCallbacks.Length - 1] = new ActorAudioAbilityElement();

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
				ability.ConsumeAudioUseEvent();
				ProcessCallbackElement(abilityCallbacks[abilityIndices[ability]], ability.Data.Audio, Actor, targets, ability.Data);
			});

			Actor.OnAfflictionObtain.AddListener(affliction => {
				ActorAudioAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.Obtain));

				affliction.ConsumeAudioObtainEvent();
				ProcessCallbackElement(callback, affliction.Data.ObtainAudio, Actor, affliction.Afflicter, affliction.Data);
			});

			Actor.OnAfflictionStart.AddListener(affliction => {
				ActorAudioAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.Trigger));

				affliction.ConsumeAudioTriggerEvent();
				ProcessCallbackElement(callback, affliction.Data.TriggerAudio, Actor,  affliction.Afflicter,affliction.Data);
			});

			Actor.OnAfflictionStageChange.AddListener((affliction, newStage) => {
				if(newStage > affliction.Stage){
					ActorAudioAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
						(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.StageIncrease));

					affliction.ConsumeAudioStageIncreaseEvent();
					ProcessCallbackElement(callback, affliction.Data.StageIncreaseAudio, Actor, affliction.Afflicter, affliction.Data);
				}
				else{
					ActorAudioAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
						(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.StageDecrease));

					affliction.ConsumeAudioStageDecreaseEvent();
					ProcessCallbackElement(callback, affliction.Data.StageDecreaseAudio, Actor, affliction.Afflicter, affliction.Data);
				}
			});

			Actor.OnAfflictionEnd.AddListener(affliction => {
				ActorAudioAfflictionElement callback = afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data &&
					(c.Type == AfflictionCallbackType.Always || c.Type == AfflictionCallbackType.End));

				affliction.ConsumeAudioEndEvent();
				ProcessCallbackElement(callback, affliction.Data.EndAudio, Actor, affliction.Afflicter, affliction.Data);
			});

			Actor.OnItemStart.AddListener((item, targets) => {
				item.ConsumeAudioUseEvent();
				ProcessCallbackElement(itemCallbacks.FirstOrDefault(c => c.Item == item.Data), item.Data.Audio, Actor, targets, item.Data);
			});

			Actor.OnAbilityPreparationStart.AddListener((ability, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Preparation.Audios[0], Actor, null, ability.Data);
			});

			Actor.OnAbilityPreparationUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Preparation.Audios[ability.Data.Preparation.Turns - turnsRemaining], Actor, null, ability.Data);
			});

			Actor.OnAbilityRecoveryStart.AddListener(ability => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Recovery.Audios[0], Actor, null, ability.Data);
			});

			Actor.OnAbilityRecoveryUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, ability.Data.Recovery.Audios[ability.Data.Recovery.Turns - turnsRemaining], Actor, null, ability.Data);
			});

			Actor.OnItemPreparationStart.AddListener((item, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Preparation.Audios[0], Actor, null, item.Data);
			});

			Actor.OnItemPreparationUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Preparation.Audios[item.Data.Preparation.Turns - turnsRemaining], Actor, null, item.Data);
			});

			Actor.OnItemRecoveryStart.AddListener(item => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Recovery.Audios[0], Actor, null, item.Data);
			});

			Actor.OnItemRecoveryUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(null, item.Data.Recovery.Audios[item.Data.Recovery.Turns - turnsRemaining], Actor, null, item.Data);
			});
		}

		public override void SetupEventCallbacks(){
			if(eventCallbacks == null){
				eventCallbacks = new ActorAudioEventElement[0];
			}

			SetupEventCallbacks<ActorAudioEventElement>(ref eventCallbacks, eventCallbacks.Select(c => c.Type).ToArray(), t => new ActorAudioEventElement(t));
		}

		public ActorAudioEventElement GetEventCallback(EventCallbackType callbackType){
			return eventCallbacks.FirstOrDefault(c => c.Type == callbackType);
		}

		public ActorAudioAbilityElement GetAbilityCallback(Ability ability){
			return GetAbilityCallback(ability.Data);
		}

		public ActorAudioAbilityElement GetAbilityCallback(AbilityData data){
			Ability ability = Actor.Abilities.FirstOrDefault(a => a.Data == data);

			if(ability == null && Actor.FallbackAbility.Data == data){
				ability = Actor.FallbackAbility;
			}

			return ability == null ? null : abilityCallbacks[abilityIndices[ability]];
		}

		public ActorAudioItemElement GetItemCallback(ItemData data){
			return itemCallbacks.FirstOrDefault(c => c.Item == data);
		}

		public ActorAudioItemElement[] GetItemCallbacks(ItemData data){
			return itemCallbacks.Where(c => c.Item == data).ToArray();
		}

		public ActorAudioAfflictionElement GetAfflictionCallback(AfflictionData data){
			return afflictionCallbacks.FirstOrDefault(c => c.Affliction == data);
		}

		public ActorAudioAfflictionElement[] GetAfflictionCallbacks(AfflictionData data){
			return afflictionCallbacks.Where(c => c.Affliction == data).ToArray();
		}

		public ActorAudioItemElement AddItemCallback(ItemData data, AudioEffect effect){
			ActorAudioItemElement callbackElement = new ActorAudioItemElement(data, effect);

			itemCallbacks.Add(callbackElement);

			return callbackElement;
		}

		public ActorAudioAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, AudioEffect effect){
			ActorAudioAfflictionElement callbackElement = new ActorAudioAfflictionElement(data, type, effect);

			afflictionCallbacks.Add(callbackElement);

			return callbackElement;
		}
		
		public override void OnAbilityAdded(){
			ActorAudioAbilityElement newElement = new ActorAudioAbilityElement();

			OnAbilityAdded(ref abilityCallbacks, newElement);
		}
		
		public override void OnAbilityRemoved(int index){
			OnAbilityRemoved(ref abilityCallbacks, index);
		}
		
		public override void OnAbilityMoved(int from, int to){
			OnAbilityMoved(ref abilityCallbacks, from, to);
		}

		void ProcessCallbackElement(EventCallbackType eventType, Actor actor){
			ActorAudioEventElement callback = eventCallbacks[(int)eventType];

			if(callback.Effect == null || !callback.Effect.enabled || ShouldIgnore(eventType, callback.ignoreEvents)){
				return;
			}

			callback.Effect.Trigger(actor, new Actor[]{actor});
		}

		void ProcessCallbackElement(ActorAudioAfflictionElement callback, AudioEffect defaultEffect, Actor actor, Actor afflicter, AfflictionData affliction){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultAfflictionAudio && defaultEffect.enabled && (afflictionCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(afflicter, new Actor[]{actor});
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger(afflicter, new Actor[]{actor});
		}

		void ProcessCallbackElement(ActorAudioItemElement callback, AudioEffect defaultEffect, Actor actor, Actor[] targets, ItemData item){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultItemAudio && defaultEffect.enabled && (itemCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(actor, new Actor[]{actor});
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger(actor, targets);
		}

		void ProcessCallbackElement(ActorAudioAbilityElement callback, AudioEffect defaultEffect, Actor actor, Actor[] targets, AbilityData ability){
			bool ignoreCallbackEffect = callback == null || callback.Effect == null || !callback.Effect.enabled;

			if(allowDefaultAbilityAudio && defaultEffect.enabled && (abilityCallbackMode == ActorAbilityCallbackMode.AlongsideDefault || ignoreCallbackEffect)){
				defaultEffect.Trigger(actor, targets);
			}

			if(ignoreCallbackEffect){
				return;
			}

			callback.Effect.Trigger(actor, targets);
		}

		[ContextMenu("Fix Missing Fallback Ability")]
		void FixMissingFallbackAbility(){
			FixMissingFallbackAbility<ActorAudioAbilityElement>(ref abilityCallbacks);
		}
	}
}