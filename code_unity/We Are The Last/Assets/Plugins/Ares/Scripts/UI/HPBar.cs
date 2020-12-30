/* A simple HP Bar component that can be used with
 * Unity UI's "Filled" type Images.
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Ares.UI {
	[RequireComponent(typeof(Image))]
	public class HPBar : BattleDelayElement {
		public enum TweenMode {Absolute, Relative}

		[SerializeField] Actor actor;
		[SerializeField] Gradient colors;
		[SerializeField] TweenMode tweenMode;
		[SerializeField] float tweenSpeed = 50f;

		Image fill;
		
		public void Init(Actor actor){
			this.actor = actor;
		}

		public void Init(Actor actor, Gradient colors, TweenMode tweenMode, float tweenSpeed){
			this.actor = actor;
			this.colors = colors;
			this.tweenMode = tweenMode;
			this.tweenSpeed = tweenSpeed;
		}

		void Awake(){
			fill = GetComponent<Image>();
		}

		protected override void Start(){
			base.Start();

			actor.OnHPChange.AddListener(UpdateValue);

			float t = (float)actor.HP / actor.MaxHP;
			fill.fillAmount = t;
			fill.color = colors.Evaluate(t);
		}

		public void UpdateValue(int newHp){
			float t = (float)newHp / actor.MaxHP;

			float tweenTime = tweenMode == TweenMode.Absolute ?
				(float)Mathf.Abs(actor.HP - newHp) / tweenSpeed :
				((float)Mathf.Abs(actor.HP - newHp) / actor.MaxHP) / tweenSpeed;

			StartCoroutine(TweenValueImage((float)actor.HP / actor.MaxHP, t, tweenTime));
			RequestBattleDelay(tweenTime, DelayRequestReason.UIEvent);
		}

		IEnumerator TweenValueImage(float from, float to, float time){
			float startTime = Time.time;

			while(Time.time - startTime < time){
				fill.fillAmount = from + (to - from) * (Time.time - startTime) / time;
				fill.color = colors.Evaluate(fill.fillAmount);
				yield return null;
			}

			fill.fillAmount = to;
			fill.color = colors.Evaluate(to);
		}
	}
}