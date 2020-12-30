using System.Collections;
using UnityEngine;
using System.Linq;

namespace Ares.Examples {
	[CreateAssetMenu(fileName="Darkness", menuName="Ares/Examples/Darkness Environment Variable", order=1500)]
	public class Darkness : EnvironmentVariableData {
		public override void OnProcess(Battle battle, Actor setter){
			BattleMonoBehaviour.Instance.StartCoroutine(CROnProcess(battle, setter));
		}

		IEnumerator CROnProcess(Battle battle, Actor setter){
			setter.TakeDamage(Mathf.RoundToInt(setter.MaxHP * .2f));

			//We can hook into the battle's delay requests to wait for the HP bar to finish tweening before continuing.
			//Only do this at times where you're 100% sure of what's going on however, so as not to get stuck waiting forever on unforseen delays or locks e.g.
			yield return new WaitWhile(() => {return battle.ProgressDelayRequests.Sum(d => d.TimeRemaining) > 0f;});

			float randomValue = Random.value;

			foreach(Actor actor in setter.Group.Actors){
				if(actor.HP > 0){
					if(randomValue < .333f){
						actor.BuffStat(actor.Stats["speed"], 1);
					}
					else if(randomValue < .666f){
						actor.BuffStat(actor.Stats["attack"], 1);
					}
					else{
						setter.BuffStat(actor.Stats["defense"], 1);
					}
				}
			}
		}
	}
}