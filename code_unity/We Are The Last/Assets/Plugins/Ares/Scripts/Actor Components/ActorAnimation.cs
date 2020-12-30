using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Ares.ActorComponents {
	[System.Serializable]
	public class ActorAnimationAbilityElement : ActorAbilityCallbackElementBase {
		public AnimationEffect Effect {get{return effect;}}

		public bool Enabled {get{return effect.enabled;} set{effect.enabled = value;}}// Convenience wrapper for Effect.Enabled

		[SerializeField] protected AnimationEffect effect;

		public ActorAnimationAbilityElement(){
			effect = new AnimationEffect();
		}
	}

	[System.Serializable]
	public class ActorAnimationEventElement : ActorAnimationAbilityElement {
		public EventCallbackType Type {get{return type;}}

		[EnumFlagsAttribute] public EventIgnoreFlags ignoreEvents;

		[SerializeField] EventCallbackType type;

		public ActorAnimationEventElement(EventCallbackType type){
			this.type = type;
		}
	}
	
	[System.Serializable]
	public class ActorAnimationAfflictionElement : ActorAnimationAbilityElement {
		public AfflictionData Affliction {get{return affliction;}}
		public AfflictionCallbackType Type {get{return type;}}

		[SerializeField] AfflictionData affliction;
		[SerializeField] AfflictionCallbackType type;

		public ActorAnimationAfflictionElement(AfflictionData data, bool enabled){
			affliction = data;
			Enabled = enabled;
		}
	}

	[System.Serializable]
	public class ActorAnimationItemElement : ActorAnimationAbilityElement {
		public ItemData Item {get{return item;}}

		[SerializeField] ItemData item;

		public ActorAnimationItemElement(ItemData data, bool enabled){
			item = data;
			Enabled = enabled;
		}
	}

	[RequireComponent(typeof(Actor)), DisallowMultipleComponent, AddComponentMenu("Ares/Actor Animation", 20)]
	public class ActorAnimation : ActorComponent {
		public enum ParamaterResetType {PreviousValue, DefaultValue}

		public ActorAnimationEventElement[] EventCallbacks {get{return eventCallbacks;}}
		public ActorAnimationAbilityElement[] AbilityCallbacks {get{return abilityCallbacks;}}
		public List<ActorAnimationAfflictionElement> AfflictionCallbacks {get{return afflictionCallbacks;}}
		public List<ActorAnimationItemElement> ItemCallbacks {get{return itemCallbacks;}}

		[SerializeField] Animator animator;
		[SerializeField] bool resetParametersAfterSet;
		[SerializeField] ParamaterResetType intParameterResetType;
		[SerializeField] int intParameterResetValue = 0;
		[SerializeField] ParamaterResetType floatParameterResetType;
		[SerializeField] float floatParameterResetValue = 0f;
		[SerializeField] ParamaterResetType boolParameterResetType;
		[SerializeField] bool boolParameterResetValue = false;
		[SerializeField, Space()] bool allowDefaultAbilityAnimation = true;
		[SerializeField] bool allowDefaultItemAnimation = true;
		[SerializeField] bool allowDefaultAfflictionAnimation = true;
		[SerializeField, HideInInspector] ActorAnimationEventElement[] eventCallbacks;
		[SerializeField, HideInInspector] ActorAnimationAbilityElement[] abilityCallbacks;
		[SerializeField, HideInInspector] List<ActorAnimationAfflictionElement> afflictionCallbacks;
		[SerializeField, HideInInspector] List<ActorAnimationItemElement> itemCallbacks;
		[SerializeField] ActorAnimationAbilityElement defaultAbilityCallback;
		[SerializeField] ActorAnimationItemElement defaultItemCallback;
		[SerializeField] ActorAnimationAfflictionElement defaultAfflictionCallback;

		int delayLockCoroutines = 0;
		BattleDelayElement delayer;

		public void Init(bool allowDefaultAbilityAnimation, bool allowDefaultItemAnimation, bool allowDefaultAfflictionAnimation, bool resetParametersAfterSet,
		   ParamaterResetType intParameterResetType, int intParameterResetValue, ParamaterResetType floatParameterResetType,
		   float floatParameterResetValue, ParamaterResetType boolParameterResetType, bool boolParameterResetValue){
			this.allowDefaultAbilityAnimation = allowDefaultAbilityAnimation;
			this.allowDefaultItemAnimation = allowDefaultItemAnimation;
			this.allowDefaultAfflictionAnimation = allowDefaultAfflictionAnimation;
			this.resetParametersAfterSet = resetParametersAfterSet;
			this.intParameterResetType = intParameterResetType;
			this.intParameterResetValue = intParameterResetValue;
			this.floatParameterResetType = floatParameterResetType;
			this.floatParameterResetValue = floatParameterResetValue;
			this.boolParameterResetType = boolParameterResetType;
			this.boolParameterResetValue = boolParameterResetValue;

			Reset();
		}

		public void Init(bool allowDefaultAbilityAnimation, bool allowDefaultItemAnimation, bool allowDefaultAfflictionAnimation, bool resetParametersAfterSet){
			Init(allowDefaultAbilityAnimation, allowDefaultItemAnimation, allowDefaultAfflictionAnimation, resetParametersAfterSet, ParamaterResetType.DefaultValue, 0,
				ParamaterResetType.DefaultValue, 0f, ParamaterResetType.DefaultValue, false);
		}

		protected override void Reset(){
			base.Reset();

			animator = GetComponent<Animator>();

			abilityCallbacks = new ActorAnimationAbilityElement[Actor.Abilities.Length];
			afflictionCallbacks = new List<ActorAnimationAfflictionElement>();
			itemCallbacks = new List<ActorAnimationItemElement>();

			for(int i=0; i<Actor.Abilities.Length; i++){
				abilityCallbacks[i] = new ActorAnimationAbilityElement();
				abilityCallbacks[i].Effect.Reset();
			}

			SetupEventCallbacks();
		}

		void Start(){
			delayer = Actor.gameObject.AddComponent<BattleDelayElement>();
			delayer.LinkToBattle(Actor.Battle);

			Actor.OnHPChange.AddListener(newHP => {
				if(newHP < Actor.HP){
					ProcessCallbackElement(Actor, EventCallbackType.TakeDamage);
				}
				else if(Actor.HP == 0){
					ProcessCallbackElement(Actor, EventCallbackType.Revive);
				}
				else{
					ProcessCallbackElement(Actor, EventCallbackType.Heal);
				}
			});

			Actor.OnStatBuff.AddListener((stat, newStage) => {
				ProcessCallbackElement(Actor, EventCallbackType.BuffStat);
			});

			Actor.OnStatDebuff.AddListener((stat, newStage) => {
				ProcessCallbackElement(Actor, EventCallbackType.DebuffStat);
			});

			Actor.OnAbilityActionAvoid.AddListener((target, abilityInfo, action) => {
				ProcessCallbackElement(Actor, EventCallbackType.AvoidAbility);
			});

			Actor.OnHPDeplete.AddListener(() => {
				ProcessCallbackElement(Actor, EventCallbackType.Die);
			});

			Actor.OnAbilityStart.AddListener((ability, targets) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, ability.Data.Animation, allowDefaultAbilityAnimation, abilityCallbacks[abilityIndices[ability]]);
			});

			Actor.OnAfflictionObtain.AddListener(affliction => {
				affliction.ConsumeAnimationObtainEvent();
				ProcessCallbackElement(Actor, affliction.Data.ObtainAnimation, allowDefaultAfflictionAnimation,
					afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data && c.Type == AfflictionCallbackType.Obtain));
			});

			Actor.OnAfflictionStart.AddListener(affliction => {
				affliction.ConsumeAnimationTriggerEvent();
				ProcessCallbackElement(Actor, affliction.Data.TriggerAnimation, allowDefaultAfflictionAnimation,
					afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data && c.Type == AfflictionCallbackType.Trigger));
			});

			Actor.OnAfflictionStageChange.AddListener((affliction, newStage) => {
				if(newStage > affliction.Stage){
					affliction.ConsumeAnimationStageIncreaseEvent();
					ProcessCallbackElement(Actor, affliction.Data.StageIncreaseAnimation, allowDefaultAfflictionAnimation,
						afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data && c.Type == AfflictionCallbackType.StageIncrease));
				}
				else{
					affliction.ConsumeAnimationStageDecreaseEvent();
					ProcessCallbackElement(Actor, affliction.Data.StageDecreaseAnimation, allowDefaultAfflictionAnimation,
						afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data && c.Type == AfflictionCallbackType.StageDecrease));
				}
			});

			Actor.OnAfflictionEnd.AddListener(affliction => {
				affliction.ConsumeAnimationEndEvent();
				ProcessCallbackElement(Actor, affliction.Data.EndAnimation, allowDefaultAfflictionAnimation,
					afflictionCallbacks.FirstOrDefault(c => c.Affliction == affliction.Data && c.Type == AfflictionCallbackType.End));
			});

			Actor.OnItemStart.AddListener((item, targets) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, item.Data.Animation, allowDefaultItemAnimation, itemCallbacks.FirstOrDefault(c => c.Item == item.Data));
			});

			Actor.OnAbilityPreparationStart.AddListener((ability, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, ability.Data.Preparation.Animations[0], true, null);
			});

			Actor.OnAbilityPreparationUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, ability.Data.Preparation.Animations[ability.Data.Preparation.Turns - turnsRemaining], true, null);
			});

			Actor.OnAbilityRecoveryStart.AddListener(ability => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, ability.Data.Recovery.Animations[0], true, null);
			});

			Actor.OnAbilityRecoveryUpdate.AddListener((ability, turnsRemaining, message) => {
				ability.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, ability.Data.Recovery.Animations[ability.Data.Recovery.Turns - turnsRemaining], true, null);
			});

			Actor.OnItemPreparationStart.AddListener((item, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, item.Data.Preparation.Animations[0], true, null);
			});

			Actor.OnItemPreparationUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, item.Data.Preparation.Animations[item.Data.Recovery.Turns - turnsRemaining], true, null);
			});

			Actor.OnItemRecoveryStart.AddListener(item => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, item.Data.Recovery.Animations[0], true, null);
			});

			Actor.OnItemRecoveryUpdate.AddListener((item, turnsRemaining, message) => {
				item.ConsumeAnimationUseEvent();
				ProcessCallbackElement(Actor, item.Data.Recovery.Animations[item.Data.Recovery.Turns - turnsRemaining], true, null);
			});
		}

		public override void SetupEventCallbacks(){
			if(eventCallbacks == null){
				eventCallbacks = new ActorAnimationEventElement[0];
			}

			SetupEventCallbacks<ActorAnimationEventElement>(ref eventCallbacks, eventCallbacks.Select(c => c.Type).ToArray(), t => new ActorAnimationEventElement(t));
		}

		public ActorAnimationEventElement GetEventCallback(EventCallbackType callbackType){
			return eventCallbacks.FirstOrDefault(c => c.Type == callbackType);
		}
		
		public ActorAnimationAbilityElement GetAbilityCallback(Ability ability){
			return GetAbilityCallback(ability.Data);
		}

		public ActorAnimationAbilityElement GetAbilityCallback(AbilityData data){
			Ability ability = Actor.Abilities.FirstOrDefault(a => a.Data == data);

			if(ability == null && Actor.FallbackAbility.Data == data){
				ability = Actor.FallbackAbility;
			}

			return ability == null ? null : abilityCallbacks[abilityIndices[ability]];
		}

		public ActorAnimationItemElement GetItemCallback(ItemData data){
			return itemCallbacks.FirstOrDefault(c => c.Item == data);
		}

		public ActorAnimationItemElement[] GetItemCallbacks(ItemData data){
			return itemCallbacks.Where(c => c.Item == data).ToArray();
		}

		public ActorAnimationAfflictionElement GetAfflictionCallback(AfflictionData data){
			return afflictionCallbacks.FirstOrDefault(c => c.Affliction == data);}

		public ActorAnimationAfflictionElement[] GetAfflictionCallbacks(AfflictionData data){
			return afflictionCallbacks.Where(c => c.Affliction == data).ToArray();
		}

		/// <summary>
		/// Adds an item callback of animation type Trigger.
		/// </summary>
		public ActorAnimationItemElement AddItemCallback(ItemData data, string parameterName, bool enabled){
			ActorAnimationItemElement callback = new ActorAnimationItemElement(data, enabled);
			callback.Effect.SetAsTrigger(parameterName);

			return AddItemCallback(callback);
		}
		
		/// <summary>
		/// Adds an item callback of animation type Bool.
		/// </summary>
		public ActorAnimationItemElement AddItemCallback(ItemData data, string parameterName, bool parameterValue, bool enabled){
			ActorAnimationItemElement callback = new ActorAnimationItemElement(data, enabled);
			callback.Effect.SetAsBool(parameterName, parameterValue);

			return AddItemCallback(callback);
		}

		/// <summary>
		/// Adds an item callback of animation type Integer.
		/// </summary>
		public ActorAnimationItemElement AddItemCallback(ItemData data, string parameterName, int parameterValue, bool enabled){
			ActorAnimationItemElement callback = new ActorAnimationItemElement(data, enabled);
			callback.Effect.SetAsInt(parameterName, parameterValue);

			return AddItemCallback(callback);
		}

		/// <summary>
		/// Adds an item callback of animation type Float.
		/// </summary>
		public ActorAnimationItemElement AddItemCallback(ItemData data, string parameterName, float parameterValue, bool enabled){
			ActorAnimationItemElement callback = new ActorAnimationItemElement(data, enabled);
			callback.Effect.SetAsFloat(parameterName, parameterValue);

			return AddItemCallback(callback);
		}
		
		ActorAnimationItemElement AddItemCallback(ActorAnimationItemElement callbackElement){
			itemCallbacks.Add(callbackElement);

			return callbackElement;
		}

		/// <summary>
		/// Adds an item callback of animation type Trigger.
		/// </summary>
		public ActorAnimationAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, string parameterName, bool enabled){
			ActorAnimationAfflictionElement callback = new ActorAnimationAfflictionElement(data, enabled);
			callback.Effect.SetAsTrigger(parameterName);

			return AddAfflictionCallback(callback);
		}

		/// <summary>
		/// Adds an item callback of animation type Bool.
		/// </summary>
		public ActorAnimationAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, string parameterName, bool parameterValue, bool enabled){
			ActorAnimationAfflictionElement callback = new ActorAnimationAfflictionElement(data, enabled);
			callback.Effect.SetAsBool(parameterName, parameterValue);

			return AddAfflictionCallback(callback);
		}

		/// <summary>
		/// Adds an item callback of animation type Integer.
		/// </summary>
		public ActorAnimationAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, string parameterName, int parameterValue, bool enabled){
			ActorAnimationAfflictionElement callback = new ActorAnimationAfflictionElement(data, enabled);
			callback.Effect.SetAsInt(parameterName, parameterValue);

			return AddAfflictionCallback(callback);
		}

		/// <summary>
		/// Adds an item callback of animation type Float.
		/// </summary>
		public ActorAnimationAfflictionElement AddAfflictionCallback(AfflictionData data, AfflictionCallbackType type, string parameterName, float parameterValue, bool enabled){
			ActorAnimationAfflictionElement callback = new ActorAnimationAfflictionElement(data, enabled);
			callback.Effect.SetAsFloat(parameterName, parameterValue);

			return AddAfflictionCallback(callback);
		}

		ActorAnimationAfflictionElement AddAfflictionCallback(ActorAnimationAfflictionElement callbackElement){
			afflictionCallbacks.Add(callbackElement);

			return callbackElement;
		}
		
		public override void OnAbilityAdded(){
			ActorAnimationAbilityElement newElement = new ActorAnimationAbilityElement();
			newElement.Effect.Reset();
			
			OnAbilityAdded(ref abilityCallbacks, newElement);
		}
		
		public override void OnAbilityRemoved(int index){
			OnAbilityRemoved(ref abilityCallbacks, index);
		}
		
		public override void OnAbilityMoved(int from, int to){
			OnAbilityMoved(ref abilityCallbacks, from, to);
		}

		bool ShouldIgnore(ActorAnimationEventElement callback){
			return ShouldIgnore(callback.Type, callback.ignoreEvents);
		}

		void ProcessCallbackElement(Actor actor, EventCallbackType eventType){
			ActorAnimationEventElement callback = eventCallbacks[(int)eventType];

			if(callback == null || callback.Effect == null || !callback.Enabled || ShouldIgnore(eventType, callback.ignoreEvents)){
				return;
			}

			ProcessEffect(actor, callback.Effect);
		}

		void ProcessCallbackElement(Actor actor, ActorAnimationAbilityElement callback){
			if(callback == null || callback.Effect == null || !callback.Enabled){
				return;
			}

			ProcessEffect(actor, callback.Effect);
		}

		void ProcessCallbackElement(Actor actor, AnimationEffect defaultEffect, bool allowDefaultEffect, ActorAnimationAbilityElement callback){
			if(callback == null || callback.Effect == null || !callback.Enabled){
				if(allowDefaultEffect && defaultEffect != null && defaultEffect.enabled){
					ProcessEffect(actor, defaultEffect);
				}

				return;
			}

			ProcessEffect(actor, callback.Effect);
		}

		public void ProcessEffect(Actor actor, AnimationEffect effect){
			if(effect == null || !effect.enabled){
				return;
			}

			switch(effect.ParameterType){
				case AnimatorControllerParameterType.Trigger:
					animator.SetTrigger(effect.ParameterName);
					HandleCallbackDelayRequest(effect);
					break;
				case AnimatorControllerParameterType.Bool:
					if(resetParametersAfterSet){
						if(boolParameterResetType == ParamaterResetType.PreviousValue){
							boolParameterResetValue = animator.GetBool(effect.ParameterName);
						}

						animator.SetBool(effect.ParameterName, effect.ValueBool);
						HandleCallbackDelayRequest(effect);
						animator.Update(0f);
						animator.SetBool(effect.ParameterName, boolParameterResetValue);
					}
					else{
						animator.SetBool(effect.ParameterName, effect.ValueBool);
						HandleCallbackDelayRequest(effect);
					}
					break;
				case AnimatorControllerParameterType.Int:
					if(resetParametersAfterSet){
						if(intParameterResetType == ParamaterResetType.PreviousValue){
							intParameterResetValue = animator.GetInteger(effect.ParameterName);
						}

						animator.SetInteger(effect.ParameterName, (int)effect.ValueFloat);
						HandleCallbackDelayRequest(effect);
						animator.Update(0f);
						animator.SetInteger(effect.ParameterName, intParameterResetValue);
					}
					else{
						animator.SetInteger(effect.ParameterName, (int)effect.ValueFloat);
						HandleCallbackDelayRequest(effect);
					}
					break;
				case AnimatorControllerParameterType.Float:
					if(resetParametersAfterSet){
						if(floatParameterResetType == ParamaterResetType.PreviousValue){
							floatParameterResetValue = animator.GetInteger(effect.ParameterName);
						}

						animator.SetFloat(effect.ParameterName, floatParameterResetValue);
						HandleCallbackDelayRequest(effect);
						animator.Update(0f);
						animator.SetFloat(effect.ParameterName, intParameterResetValue);
					}
					else{
						animator.SetFloat(effect.ParameterName, floatParameterResetValue);
						HandleCallbackDelayRequest(effect);
					}
					break;
			}
		}

		void HandleCallbackDelayRequest(AnimationEffect effect){
			switch(effect.DelayType){
				case DelayType.Time:
					delayer.RequestBattleDelay(effect.DelayTime, DelayRequestReason.AnimationEvent);
					break;
				case DelayType.Animation:
					if(delayer.RequestBattleDelayLock(DelayRequestReason.AnimationEvent)){
						StartCoroutine(CRMonitorLockCondition(effect));
					}
					break;
			}
		}

		 void OnParameterSet(Actor actor){
			animator.Update(0f);
		}

		IEnumerator CRMonitorLockCondition(AnimationEffect effect){
			delayLockCoroutines++;

			switch(effect.ParameterType){
				case AnimatorControllerParameterType.Int:
					int startInt = animator.GetInteger(effect.ParameterName);
					yield return new WaitUntil(() => startInt != animator.GetInteger(effect.ParameterName));
					break;
				case AnimatorControllerParameterType.Float:
					float startFloat = animator.GetFloat(effect.ParameterName);
					yield return new WaitUntil(() => startFloat != animator.GetFloat(effect.ParameterName));
					break;
				default:
					bool startBool = animator.GetBool(effect.ParameterName);
					yield return new WaitUntil(() => startBool != animator.GetBool(effect.ParameterName));
					break;
			}

			yield return new WaitUntil(() => !animator.IsInTransition(0));

			AnimatorStateInfo currentStateInfo;
			int startStateHash = animator.GetCurrentAnimatorStateInfo(effect.DelayAnimationLayer).fullPathHash;

			yield return new WaitUntil(() => {
				currentStateInfo = animator.GetCurrentAnimatorStateInfo(effect.DelayAnimationLayer);
				return currentStateInfo.fullPathHash != startStateHash || currentStateInfo.normalizedTime >= 1f;
				});

			delayLockCoroutines--;

			if(delayLockCoroutines == 0){
				delayer.ReleaseBattleDelayLock();
			}
		}

		[ContextMenu("Fix Missing Fallback Ability")]
		void FixMissingFallbackAbility(){
			FixMissingFallbackAbility<ActorAnimationAbilityElement>(ref abilityCallbacks);
		}
	}
}