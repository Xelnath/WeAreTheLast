using System.Collections;
using UnityEngine;
using Ares.Extensions;

namespace Ares.Examples {
	public class NeutralZoneDome : InstantiationEffectInstance {
		[SerializeField] AnimationCurve initRotationAndEmission;
		[SerializeField] AnimationCurve initTransparency;
		[SerializeField] float metallicAndSmoothnessLerpTime;
		[SerializeField] float rotationSpeedMultiplier;
		[SerializeField] float maxEmissionValue;
		[SerializeField] float initTime;

		float spawnTime;
		Material material;
		Color originalColor;
		Color transparentColor;
		Color originalEmission;
		Color highestEmission;
		float originalMetallic;
		float originalSmoothness;

		public override void OnSpawned<T>(Actor caster, Actor[] targets, T origin){
			base.OnSpawned<T>(caster, targets, origin);

			EnvironmentVariable envVar = (EnvironmentVariable)(object)origin;
			caster.Battle.OnEnvironmentVariableUnset.AddOneTimeListener(e => {
//				Debug.LogErrorFormat("Blaaaaaa unset: {0} ----> {1}", e.Data.DisplayName, (e == envVar));
				if(e == envVar){
//					Destroy(this.gameObject);
					StartCoroutine(CRInit(false));
				}
			});

			spawnTime = Time.time;

			caster.Battle.RequestProgressDelay(initTime, DelayRequestReason.AbilityEvent);

			material = GetComponent<MeshRenderer>().material;
			originalSmoothness = material.GetFloat("_Glossiness");
			originalMetallic = material.GetFloat("_Metallic");
			originalColor = transparentColor = material.color;
			originalEmission = material.GetColor("_EmissionColor");
			transparentColor.a = 0f;

			float h, s, v;
			Color.RGBToHSV(originalEmission, out h, out s, out v);

			originalEmission = Color.HSVToRGB(h, s, 0.01f);
			highestEmission = Color.HSVToRGB(h, s, maxEmissionValue);

			StartCoroutine(CRInit(true));
		}

		void Update(){
			transform.Rotate(Vector3.up * initRotationAndEmission.Evaluate((Time.time - spawnTime) / initTime) * rotationSpeedMultiplier * Time.deltaTime);
		}

		IEnumerator CRInit(bool forward){
			float startTime = Time.time;
			float t1 = 0f;
			float t2 = 0f;
			float t3 = 0f;
			bool done = false;

			while(!done){
				t1 = (Time.time - startTime) / initTime;
				t2 = t1;
				t3 = (Time.time - startTime) / metallicAndSmoothnessLerpTime;
				done = t1 >= 1f || t3 >= 1f;

				if(forward){
					material.SetColor("_EmissionColor", Color.Lerp(originalEmission, highestEmission, initRotationAndEmission.Evaluate(t1)));
				}
				else{
					t1 = 1f - t1;
					t2 = t1 - .5f;
					t3 = 1f - t3;
				}

				material.color = Color.Lerp(transparentColor, originalColor, initTransparency.Evaluate(t2));
				material.SetFloat("_Glossiness", Mathf.Lerp(1f, originalSmoothness, t3));
				material.SetFloat("_Metallic", Mathf.Lerp(0f, originalMetallic, t3));

				yield return null;
			}

			if(!forward){
				Destroy(gameObject);
			}
		}
	}
}