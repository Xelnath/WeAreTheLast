using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ares;

public class MindBurstEffect : InstantiationEffectInstance {
	[SerializeField] Vector3 moveSpeed;
	[SerializeField] float scaleSpeed;

	public override void OnSpawned<T>(Actor caster, Actor[] targets, T origin){
		base.OnSpawned(caster, targets, origin);

		transform.rotation = caster.transform.rotation;
	}

	void Update(){
		transform.Translate(moveSpeed * Time.deltaTime, Space.Self);
		transform.localScale = transform.localScale * (1f + scaleSpeed * Time.deltaTime); //exponential
	}
}
