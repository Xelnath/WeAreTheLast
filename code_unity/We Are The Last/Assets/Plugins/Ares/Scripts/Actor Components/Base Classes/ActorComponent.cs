using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Ares.ActorComponents {
	[System.Flags] public enum EventIgnoreFlags {Abilities = 1, Items = 2, Afflictions = 4}

	public enum ActorAbilityCallbackMode {ReplaceDefault, AlongsideDefault}
	public enum AfflictionCallbackType {Obtain, Trigger, StageIncrease, StageDecrease, End, Always}
	public enum EventCallbackType {TakeDamage, Heal, BuffStat, DebuffStat, AvoidAbility, Die, Revive}

	[System.Serializable]
	public abstract class ActorAbilityCallbackElementBase {}

	[RequireComponent(typeof(Actor))]
	public abstract class ActorComponent : MonoBehaviour {
		#if UNITY_EDITOR
		#pragma warning disable CS0414 //Used in Inspector code only; let's suppress these "unused field" warnings
		[SerializeField, HideInInspector] bool showAbilities = true;
		[SerializeField, HideInInspector] bool showEvents = true;
		[SerializeField, HideInInspector] bool showAfflictions = true;
		[SerializeField, HideInInspector] bool showItems = true;
		#pragma warning restore CS0414
		#endif

		public Actor Actor {get; private set;}
		protected Dictionary<Ability, int> abilityIndices = new Dictionary<Ability, int>();

		protected virtual void Reset(){
			Actor = GetComponent<Actor>();
		}

		protected virtual void Awake(){
			Actor = GetComponent<Actor>();

			for(int i=0; i<Actor.Abilities.Length; i++){
				abilityIndices.Add(Actor.Abilities[i], i);
			}

			if(Actor.FallbackAbility != null){
				abilityIndices.Add(Actor.FallbackAbility, Actor.Abilities.Length);
			}

			SetupEventCallbacks();
		}

		public void OnAbilityAdded<T>(ref T[] abilityCallbacks, T newElement){
			System.Array.Resize(ref abilityCallbacks, abilityCallbacks.Length + 1);

			abilityCallbacks[abilityCallbacks.Length - 1] = abilityCallbacks[abilityCallbacks.Length - 2];
			abilityCallbacks[abilityCallbacks.Length - 2] = newElement;
		}

		protected void OnAbilityRemoved<T>(ref T[] abilityCallbacks, int index){
			for(int i = index; i < abilityCallbacks.Length - 1; i++){
				abilityCallbacks[i] = abilityCallbacks[i + 1];
			}

			System.Array.Resize(ref abilityCallbacks, abilityCallbacks.Length - 1);
		}

		public void OnAbilityMoved<T>(ref T[] abilityCallbacks, int from, int to){
			if(from < to){
				T temp = abilityCallbacks[from];

				for(int i = from; i < to; i++){
					abilityCallbacks[i] = abilityCallbacks[i + 1];
				}

				abilityCallbacks[to] = temp;
			}
			else{
				var temp = abilityCallbacks[from];

				for(int i = from; i > to; i--){
					abilityCallbacks[i] = abilityCallbacks[i - 1];
				}

				abilityCallbacks[to] = temp;
			}
		}

		protected void SetupEventCallbacks<T>(ref T[] eventCallbacks, EventCallbackType[] types, System.Func<EventCallbackType, T> initializer) where T : ActorAbilityCallbackElementBase{
			System.Array eventCallbackTypes = System.Enum.GetValues(typeof(EventCallbackType));

			List<T> newEventCallbacks = new List<T>(eventCallbackTypes.Length);

			foreach(var eventCallbackType in eventCallbackTypes){
				T existingElement = null;

				for(int i = 0; i < eventCallbacks.Length; i++){
					if(types[i] == (EventCallbackType)eventCallbackType){
						existingElement = eventCallbacks[i];
						break;
					}
				}

				if(existingElement != null){
					newEventCallbacks.Add(existingElement);
				}
				else{
					newEventCallbacks.Add(initializer((EventCallbackType)eventCallbackType));
				}
			}

			eventCallbacks = newEventCallbacks.ToArray();
		}

		protected bool ShouldIgnore(EventCallbackType callbackType, EventIgnoreFlags ignoreEvents){
			return (callbackType == EventCallbackType.TakeDamage || callbackType == EventCallbackType.Heal || callbackType == EventCallbackType.BuffStat ||
					callbackType == EventCallbackType.DebuffStat) &&
				   (((ignoreEvents & EventIgnoreFlags.Abilities) == EventIgnoreFlags.Abilities && Actor.IsCastingAbility) ||
					((ignoreEvents & EventIgnoreFlags.Items) == EventIgnoreFlags.Items && Actor.IsUsingItem) ||
					((ignoreEvents & EventIgnoreFlags.Afflictions) == EventIgnoreFlags.Afflictions && Actor.IsProcessingOwnAffliction));
		}

		protected void FixMissingFallbackAbility<T>(ref T[] abilityCallbacks) where T : new(){
			int numAbilities = GetComponent<Actor>().Abilities.Length;

			if(abilityCallbacks.Length == numAbilities){
				System.Array.Resize(ref abilityCallbacks, abilityCallbacks.Length + 1);
				abilityCallbacks[abilityCallbacks.Length - 1] = new T();
				Debug.Log("Added missing fallback ability.");
			}
			#if UNITY_EDITOR
			else if(UnityEditor.EditorUtility.DisplayDialog("Fixing ability list",
				   "All abilities already seem to be accounted for. Do you wish to continue and reset all ability entries on this component?",
				   "Ok", "Cancel")){
				System.Array.Resize(ref abilityCallbacks, numAbilities + 1);

				for(int i = 0; i < numAbilities + 1; i++){
					abilityCallbacks[i] = new T();
				}
			}
			#endif
		}

		public abstract void OnAbilityAdded();
		public abstract void OnAbilityRemoved(int index);
		public abstract void OnAbilityMoved(int from, int to);
		public abstract void SetupEventCallbacks();
	}
}