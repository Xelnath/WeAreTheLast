using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.Examples {
	public class ImperialBlast : MonoBehaviour {
		[SerializeField] float moveSpeed = 1f;
		[SerializeField] float textureSpeed = 1f;
		[SerializeField] float startSize = .2f;
		[SerializeField] float endSize = 1f;
		[SerializeField] float growTime = .5f;

		new Renderer renderer;
		float offset;

		void Awake(){
			renderer = GetComponent<Renderer>();

			transform.localScale = new Vector3(startSize, startSize, startSize);

			StartCoroutine(CRGrow());
		}

		IEnumerator CRGrow(){
			float startTime = Time.time;
			float endTime = startTime + growTime;

			while(Time.time < endTime){
				float t = (Time.time - startTime) / growTime;
				float s = Mathf.Lerp(startSize, endSize, t);

				transform.localScale = new Vector3(s, s, s);

				yield return null;
			}
		}

		void Update(){
			transform.Translate(-transform.forward * moveSpeed * Time.deltaTime);

			offset = (offset + textureSpeed * Time.deltaTime) % 1f;
			renderer.material.SetTextureOffset("_MainTex", new Vector2(0f, -offset));
		}
	}
}