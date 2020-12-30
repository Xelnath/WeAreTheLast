using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Ares {
	public class InstantiationEffectInstance : MonoBehaviour {
		public static List<InstantiationEffectInstance> ActiveEffects {get; private set;}

		public float Lifetime {get{return lifetime;}}

		[SerializeField, Tooltip("Values <= 0 will not schedule destruction")] float lifetime;

		static InstantiationEffectInstance(){
			ActiveEffects = new List<InstantiationEffectInstance>();
		}

		public virtual void OnSpawned<T>(Actor caster, Actor[] targets, T origin){
			ActiveEffects.Add(this);

			if(lifetime > 0f){
				StartCoroutine(ScheduleDestruction());
			}
		}

		IEnumerator ScheduleDestruction(){
			yield return new WaitForSeconds(lifetime);

			ActiveEffects.Remove(this);
			Destroy(gameObject);
		}
	}
}