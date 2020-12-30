using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares {
	public class BattleDelayElement : MonoBehaviour {
		[SerializeField, Header("Delay element")] protected bool autoLinkToLastActiveBattle = true;
		[SerializeField] protected bool delayAutoLinkByOneFrame;
		[SerializeField] protected bool autoLockBattleAfterLink = false; //auto-unlocks when the object is destroyed
		[SerializeField] protected bool delayAutoLockByOneFrame;
		[SerializeField] protected DelayRequestReason autoLockReason;

		protected Battle battle;

		protected virtual void Start(){
			if(autoLinkToLastActiveBattle){
				if(delayAutoLinkByOneFrame || (autoLockBattleAfterLink && delayAutoLockByOneFrame)){
					StartCoroutine(CRAddToLastActiveBattle());
				}
				else{
					TryAutoLock();
				}
			}
		}

		void OnDestroy(){
			if(battle != null){
				battle.ReleaseProgressDelayLock(this);
			}
		}
		 
		public void LinkToBattle(Battle battle){
			this.battle = battle;

			if(autoLockBattleAfterLink){
				battle.RequestProgressDelayLock(this, autoLockReason);
			}
		}

		public bool RequestBattleDelay(float delay, DelayRequestReason reason){
			if(battle != null){
				return battle.RequestProgressDelay(delay, reason);
			}

			return false;
		}

		public bool RequestBattleDelayLock(DelayRequestReason reason){
			if(battle != null){
				return battle.RequestProgressDelayLock(this, reason);
			}

			return false;
		}

		public void ReleaseBattleDelayLock(){
			if(battle != null){
				battle.ReleaseProgressDelayLock(this);
			}
		}
		
		IEnumerator CRAddToLastActiveBattle(){
			if(delayAutoLinkByOneFrame || delayAutoLockByOneFrame){
				yield return null; //wait 1 frame so that all Start() and other setup methods of possible managers etc. have run
			}

			TryAutoLock();
		}

		void TryAutoLock(){
			Battle lastActiveBattle = Battle.LastActiveBattle;

			if(lastActiveBattle != null){
				LinkToBattle(lastActiveBattle);
			}
			else{
				Debug.LogWarning("Battle Delay Element " + gameObject.name + " could not find an active battle to link to.", gameObject);
			}
		}
	}
}