using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.Examples {
	enum ShieldState {Attached, FlyingTowardsTarget, Returning, Dropped}

	public class ShadowAresCustomCallbacks : BattleDelayElement {
		[SerializeField, Header("References")] Transform shieldDeformBone;
		[SerializeField] Transform shieldHandleBone;
		[SerializeField] Transform spearBone;
		[SerializeField] Transform heldItemBone;
		[SerializeField] Transform dropTarget;
		[SerializeField, Header("Shield throw parameters")] AbilityData shieldThrowData;
		[SerializeField] Vector3 dropRotation = new Vector3(15f, 90f, 20f);
		[SerializeField] float flyMoveSpeed;
		[SerializeField] float flyRotateSpeed;
		[SerializeField] float dropMoveSpeed;
		[SerializeField] float dropRotateSpeed;
		[SerializeField] float postShieldThrowDelay;

		Transform shieldParent;
		ShieldState shieldState;
		Actor shieldTarget;
		Vector3 shieldOverridePosition;
		Vector3 shieldOverrideScale;
		Quaternion shieldOverrideRotation;
		Vector3 shieldRotationAxis;
		Quaternion shieldDroppedRotation;

		Vector3 spearOverridePosition;
		Quaternion spearOverrideRotation;
		bool resetSpear;

		void Awake(){
			shieldParent = shieldDeformBone.parent;
		}

		protected override void Start(){
			base.Start();

			GetComponent<Actor>().OnAbilityStart.AddListener(OnAbilityStart);

			shieldDroppedRotation = Quaternion.Euler(dropRotation);
		}

		void LateUpdate(){
			switch(shieldState){
				case ShieldState.FlyingTowardsTarget:
					shieldOverridePosition = Vector3.MoveTowards(shieldOverridePosition, shieldTarget.transform.position, flyMoveSpeed * Time.deltaTime);
					shieldOverrideRotation *= Quaternion.Euler(shieldRotationAxis * flyRotateSpeed * Time.deltaTime);

					shieldDeformBone.position = shieldOverridePosition;
					shieldDeformBone.localScale = shieldOverrideScale;
					shieldDeformBone.rotation = shieldOverrideRotation;

					if(Vector3.Distance(shieldOverridePosition, shieldTarget.transform.position) < .02f) {
						shieldState = ShieldState.Returning;
					}
					break;
				case ShieldState.Returning:
					shieldOverridePosition = Vector3.MoveTowards(shieldOverridePosition, shieldHandleBone.transform.position, flyMoveSpeed * Time.deltaTime);
					shieldOverrideRotation *= Quaternion.Euler(shieldRotationAxis * flyRotateSpeed * Time.deltaTime);

					if(Vector3.Distance(shieldOverridePosition, shieldHandleBone.transform.position) < .02f) {
						shieldDeformBone.SetParent(shieldParent, true);
						shieldDeformBone.localPosition = shieldHandleBone.localPosition;
						shieldDeformBone.localRotation = shieldHandleBone.localRotation;
						shieldState = ShieldState.Attached;

						StartCoroutine(CRReleaseBattleDelayLock(postShieldThrowDelay));
					}
					else{
						shieldDeformBone.position = shieldOverridePosition;
						shieldDeformBone.localScale = shieldOverrideScale;
						shieldDeformBone.rotation = shieldOverrideRotation;
					}
					break;
				case ShieldState.Dropped:
					shieldOverridePosition = Vector3.MoveTowards(shieldOverridePosition, dropTarget.position, dropMoveSpeed * Time.deltaTime);
					shieldOverrideRotation = Quaternion.Slerp(shieldOverrideRotation, shieldDroppedRotation, dropRotateSpeed * Time.deltaTime);

					shieldDeformBone.position = shieldOverridePosition;
					shieldDeformBone.localScale = shieldOverrideScale;
					shieldDeformBone.rotation = shieldOverrideRotation;
					break;
			}

			if(resetSpear){
				spearBone.position = spearOverridePosition;
				spearBone.rotation = spearOverrideRotation;
			}
		}

		void OnAbilityStart(Ability ability, Actor[] targets){
			if(ability.Data == shieldThrowData){
				shieldTarget = targets[0];
			}
		}

		public void ThrowShield(){
			shieldState = ShieldState.FlyingTowardsTarget;
			shieldDeformBone.SetParent(null, true);
			shieldOverridePosition = shieldDeformBone.position;
			shieldOverrideScale = shieldDeformBone.localScale;
			shieldOverrideRotation = shieldDeformBone.rotation;
			shieldRotationAxis = shieldDeformBone.up;

			RequestBattleDelayLock(DelayRequestReason.AbilityEvent);
		}

		public void DropShield(){
			shieldState = ShieldState.Dropped;
			shieldDeformBone.SetParent(null, true);
			shieldOverridePosition = shieldDeformBone.position;
			shieldOverrideScale = shieldDeformBone.localScale;
			shieldOverrideRotation = shieldDeformBone.rotation;
		}

		public void GrowHeldItem(AnimationEvent @event){
			heldItemBone.GetComponentInChildren<PotionEffect>().Grow();
		}

		public void TossHeldItem(AnimationEvent @event){
			heldItemBone.GetComponentInChildren<PotionEffect>().Toss();
		}

		public void ReleaseSpear(AnimationEvent @event){
			spearOverridePosition = spearBone.position;
			spearOverrideRotation = spearBone.rotation;

			StartCoroutine(CRReleaseSpear(@event.animatorClipInfo.clip.length));
		}

		IEnumerator CRReleaseSpear(float regrabTime){
			resetSpear = true;

			yield return new WaitForSeconds(regrabTime);

			resetSpear = false;
		}

		IEnumerator CRReleaseBattleDelayLock(float delay){
			yield return new WaitForSeconds(delay);

			ReleaseBattleDelayLock();
		}
	}
}