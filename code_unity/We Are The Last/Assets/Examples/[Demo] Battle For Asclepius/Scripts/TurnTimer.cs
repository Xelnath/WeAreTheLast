using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ares.Examples {
	public class TurnTimer : MonoBehaviour {
		[SerializeField] RectTransform graphics;
		[SerializeField] Image border;
		[SerializeField] Image inner;
		[SerializeField] Text label;
		[SerializeField] Gradient colors1;
		[SerializeField] Gradient colors2;

		IEnumerator Start(){
			yield return null;

			if(Battle.LastActiveBattle.Rules.TurnTimeout > 0f){
				Battle.LastActiveBattle.OnActorNeedsActionInput.AddListener((a, i) =>{
					graphics.gameObject.SetActive(true);
					SetAppearance(1f);
				});
			
				Battle.LastActiveBattle.OnActorHasGivenAllNeededInput.AddListener(a =>{
					graphics.gameObject.SetActive(false);
				});
			}
		}

		void Update(){
			if(Battle.LastActiveBattle != null && graphics.gameObject.activeSelf){
				float t = Battle.LastActiveBattle.TurnTimeLeft / Battle.LastActiveBattle.Rules.TurnTimeout;
				
				label.text = Battle.LastActiveBattle.TurnTimeLeft.ToString(Battle.LastActiveBattle.TurnTimeLeft > 10f ? "##" : "f1");
				SetAppearance(t);
			}
		}

		void SetAppearance(float t){
			border.fillAmount = t;
			label.color = colors1.Evaluate(t);
			border.color = colors1.Evaluate(t);
			inner.color = colors2.Evaluate(t);
		}
	}
}