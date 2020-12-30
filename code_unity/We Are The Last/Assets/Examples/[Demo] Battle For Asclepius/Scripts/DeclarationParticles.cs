using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.Examples {
	public class DeclarationParticles : MonoBehaviour {
		[SerializeField] float cooldownDelay;
		[SerializeField] float cooldownTime;

		IEnumerator Start(){
			ParticleSystem.EmissionModule emission = GetComponent<ParticleSystem>().emission;

			yield return new WaitForSeconds(cooldownDelay);

			float startTime = Time.time;
			float endTime = startTime + cooldownTime;
			float emissionRateStart = emission.rateOverTimeMultiplier;

			while(Time.time < endTime){
				float t = (Time.time - startTime) / cooldownTime;
				emission.rateOverTimeMultiplier = Mathf.Lerp(emissionRateStart, 0f, t);
				yield return null;
			}

			emission.rateOverTimeMultiplier = 0f;
		}
	}
}