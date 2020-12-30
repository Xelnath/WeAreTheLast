using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arex.Examples {
	public class GhostShieldEffect : MonoBehaviour {
		[SerializeField] float startScale = 25f;
		[SerializeField] float endScale = 50f;
		[SerializeField] float endOffset = 1f;
		[SerializeField] float animationTime = 1f;
		[SerializeField] AnimationCurve animationProgression;

		Vector3 startScaleV3;
		Vector3 endScaleV3;
		Vector3 startPosition;
		Vector3 endPosition;
		float startTime;
		Color startDiffColor;
		Color endDiffColor;
		Color startEmisColor;
		Color endEmisColor;
		Material[] mats;

		void Start(){
			startScaleV3 = Vector3.one * startScale;
			endScaleV3 = Vector3.one * endScale;
			startPosition = transform.position;
			endPosition = transform.position + Vector3.forward * endOffset;
			startTime = Time.time;

			mats = GetComponentInChildren<Renderer>().materials;
			startDiffColor = mats[0].color;
			endDiffColor = mats[0].color;
			endDiffColor.a = 0f;
			startEmisColor = mats[0].GetColor("_EmissionColor");
			endEmisColor = Color.black;
		}

		void Update(){
			float t = animationProgression.Evaluate((Time.time - startTime) / animationTime);

			transform.position = Vector3.Lerp(startPosition, endPosition, t);
			transform.localScale = Vector3.Lerp(startScaleV3, endScaleV3, t);

			foreach(Material mat in mats) {
				mat.color = Color.Lerp(startDiffColor, endDiffColor, t);
				mat.SetColor("_EmissionColor", Color.Lerp(startEmisColor, endEmisColor, t));
			}
		}
	}
}