using System.Collections;
using UnityEngine;

namespace Ares.Examples {
	public class PotionEffect : InstantiationEffectInstance {
		[SerializeField] Vector3 tossForce;
		[SerializeField] float growTime;
		[SerializeField] float shrinkTime;

		float spawnTime;
		Vector3 startScale;

		void Start(){
			spawnTime = Time.time;

			startScale = transform.localScale;
			transform.localScale = Vector3.zero;
		}

		public void Grow(){
			StartCoroutine(CRChangeSize(startScale, growTime, 0f));
		}

		public void Toss(){
			Rigidbody rigidbody = GetComponent<Rigidbody>();
			rigidbody.isKinematic = false;
			rigidbody.AddForce(tossForce, ForceMode.Impulse);

			StartCoroutine(CRChangeSize(Vector3.zero, shrinkTime, Lifetime - (Time.time - spawnTime) - shrinkTime));
		}

		IEnumerator CRChangeSize(Vector3 to, float time, float delay){
			if(delay > 0f){
				yield return new WaitForSeconds(delay);
			}

			Vector3 from = transform.localScale;
			float startTime = Time.time;

			while(Time.time < startTime + time){
				transform.localScale = Vector3.Lerp(from, to, (Time.time - startTime) / time);
				yield return null;
			}

			transform.localScale = to;
		}
	}
}