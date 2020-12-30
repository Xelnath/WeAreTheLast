using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.Examples {
	public class NeutralZoneWall : InstantiationEffectInstance {
		[SerializeField] float spawnOffsetFromTarget;
		[SerializeField] float expandTime;
		[SerializeField] float cooldownTime;
		[SerializeField] float cooldownSpeed;
		[SerializeField] float emissionRateStart;

		public override void OnSpawned<T>(Actor caster, Actor[] targets, T origin){
			StartCoroutine(CROnSpawned());

			Vector3 spawnPosition = transform.position;
			spawnPosition.z = targets[0].transform.position.z + targets[0].transform.forward.z * spawnOffsetFromTarget;
			transform.position = spawnPosition;
		}

		IEnumerator CROnSpawned(){
			ParticleSystem.EmissionModule emission = GetComponent<ParticleSystem>().emission;

			float startTime = Time.time;
			float endTime = startTime + expandTime;
			float emissionRateEnd = emission.rateOverTimeMultiplier;

			while(Time.time < endTime){
				float t = (Time.time - startTime) / expandTime;
				transform.localScale = new Vector3(Mathf.Lerp(0f, 1f, t), 1f, 1f);
				emission.rateOverTimeMultiplier = Mathf.Lerp(emissionRateStart, emissionRateEnd, t);
				yield return null;
			}

			transform.localScale = Vector3.one;
			emission.rateOverTimeMultiplier = emissionRateEnd;

			yield return new WaitForSeconds(Lifetime - endTime - cooldownTime);

			startTime = Time.time;
			endTime = startTime + cooldownSpeed;

			while(Time.time < endTime){
				float t = (Time.time - startTime) / cooldownSpeed;
				emission.rateOverTimeMultiplier = Mathf.Lerp(emissionRateEnd, 0f, t);
				yield return null;
			}

			emission.rateOverTimeMultiplier = 0f;
		}
	}
}