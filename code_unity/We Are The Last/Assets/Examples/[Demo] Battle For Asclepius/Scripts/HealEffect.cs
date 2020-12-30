using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Ares.Examples {
	public class HealEffect : InstantiationEffectInstance {
		[SerializeField] float startScale = .5f;
		[SerializeField] float endScale = 10f;
		[SerializeField] float endRotationY = 10f;
		[SerializeField] float animationTime = 4f;
		[SerializeField] AnimationCurve animationProgression;
		[SerializeField] AnimationCurve softFactorOverTime;

		Vector3 startScaleV3;
		Vector3 endScaleV3;
		Quaternion startRotation;
		Quaternion endRotation;
		float startTime;
		Material[] mats;

		void Start(){
			startScaleV3 = Vector3.one * startScale;
			endScaleV3 = Vector3.one * endScale;
			startRotation = transform.rotation;
			endRotation = Quaternion.Euler(startRotation.eulerAngles.x, endRotationY, startRotation.eulerAngles.z);
			startTime = Time.time;

			mats = GetComponentsInChildren<MeshRenderer>().Select(r => r.material).ToArray();

			Update();
		}

		void Update(){
			float t = (Time.time - startTime) / animationTime;
			float _t = animationProgression.Evaluate(t);

			transform.rotation = Quaternion.Lerp(startRotation, endRotation, _t);
			transform.localScale = Vector3.Lerp(startScaleV3, endScaleV3, _t);

			foreach(Material mat in mats) {
				mat.SetFloat("_InvFade", softFactorOverTime.Evaluate(t));
			}
		}
	}
}