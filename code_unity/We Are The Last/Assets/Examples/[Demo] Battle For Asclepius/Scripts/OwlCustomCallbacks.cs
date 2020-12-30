using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ares;

namespace Ares.Examples {
	public class OwlCustomCallbacks : MonoBehaviour {
		[SerializeField] SkinnedMeshRenderer eyesRenderer;
		[SerializeField] AbilityData mindBurst;
		[SerializeField] Color chargedEyeColor;
		[SerializeField] Color[] chargedColors;

		Material eyeMaterial;
		Material[] glowMaterials;
		Color startEyeColor;

		void Start(){
			eyeMaterial = eyesRenderer.materials[6];
			glowMaterials = new Material[]{eyesRenderer.materials[0], eyesRenderer.materials[1]};
			startEyeColor = eyeMaterial.color;

			GetComponent<Actor>().OnAbilityStart.AddListener((ability, targets) => {
				if(ability.Data == mindBurst){
					StartCoroutine(CRLerpEyeColor(.4f, .6f, .5f, .3f));
				}
				else{
					StartCoroutine(CRLerpWingEmission(0f, .6f, ability.Data.Actions.Where(a => !a.IsChildEffect).Sum(a => a.Duration) - 0.3f, .6f));
				}
			});
		}

		IEnumerator CRLerpEyeColor(float holdTime1, float toTime, float holdTime2, float backTime){
			yield return new WaitForSeconds(holdTime1);

			float startTime = Time.time;
			float t = 0f;

			while(t < 1f){
				t = (Time.time - startTime) / toTime;
				eyeMaterial.color = Color.Lerp(startEyeColor, chargedEyeColor, t);
				yield return null;
			}

			yield return new WaitForSeconds(holdTime2);

			startTime = Time.time;
			t = 0f;

			while(t < 1f){
				t = (Time.time - startTime) / backTime;
				eyeMaterial.color = Color.Lerp(chargedEyeColor, startEyeColor, t);
				yield return null;
			}
		}

		IEnumerator CRLerpWingEmission(float holdTime1, float toTime, float holdTime2, float backTime){
			yield return new WaitForSeconds(holdTime1);

			Color[] startColors = new Color[glowMaterials.Length];

			for(int i = 0; i < glowMaterials.Length; i++){
				startColors[i] = glowMaterials[i].GetColor("_EmissionColor");
			}

			float startTime = Time.time;
			float t = 0f;

			while(t < 1f){
				t = (Time.time - startTime) / toTime;

				for(int i = 0; i < glowMaterials.Length; i++){
					glowMaterials[i].SetColor("_EmissionColor", Color.Lerp(startColors[i], chargedColors[i], t));
				}

				yield return null;
			}

			yield return new WaitForSeconds(holdTime2);

			startTime = Time.time;
			t = 0f;

			while(t < 1f){
				t = (Time.time - startTime) / backTime;

				for(int i = 0; i < glowMaterials.Length; i++){
					glowMaterials[i].SetColor("_EmissionColor", Color.Lerp(chargedColors[i], startColors[i], t));
				}

				yield return null;
			}
		}
	}
}