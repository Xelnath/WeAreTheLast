using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	[AddComponentMenu("Ares/AI Actor", 1)]
	public class AIActor : Actor {
		public virtual void SelectAction(ActionInput actionInput){
			VerboseLogger.Log("Selecting AI action");

			//Simplistically select a random valid ability or item to cast. If one does not
			//exist the turn will be skipped. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			if(actionInput.ValidAbilities.Length > 0){
				actionInput.AbilitySelectCallback(actionInput.ValidAbilities[Random.Range(0, actionInput.ValidAbilities.Length)]);
			}
			else if(actionInput.ValidAbilities.Length > 0){
				actionInput.ItemSelectCallback(actionInput.ValidItems[Random.Range(0, actionInput.ValidItems.Length)]);
			}
			else{
				actionInput.SkipCallback();
			}
		}

		public virtual void SelectTarget(Ability ability, TargetInputSingleActor targetInput){
			VerboseLogger.Log(string.Format("Selecting AI target for ability {0}", ability.Data.DisplayName));

			//Select a random valid target to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			targetInput.TargetSelectCallback(targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)]);
		}

		public virtual void SelectTargets(Ability ability, TargetInputNumActors targetInput){
			VerboseLogger.Log(string.Format("Selecting AI targets for ability {0}", ability.Data.DisplayName));

			//Select random valid targets to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			List<int> availableTargetIndices = Enumerable.Range(0, targetInput.ValidTargets.Length).ToList();
			Actor[] chosenTargets = new Actor[Mathf.Min(targetInput.TargetsRequired, targetInput.ValidTargets.Length)];

			for(int i = 0; i < chosenTargets.Length; i++){
				int chosenIndex = Random.Range(0, availableTargetIndices.Count);

				chosenTargets[i] = targetInput.ValidTargets[availableTargetIndices[chosenIndex]];
				availableTargetIndices.RemoveAt(chosenIndex);
			}

			targetInput.TargetSelectCallback(chosenTargets);
		}

		public virtual void SelectTargets(Ability ability, TargetInputGroup targetInput){
			VerboseLogger.Log(string.Format("Selecting AI target for ability {0}", ability.Data.DisplayName));

			//Select a random valid target to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			targetInput.TargetSelectCallback(targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)]);
		}

		public virtual void SelectTarget(Item item, TargetInputSingleActor targetInput){
			VerboseLogger.Log(string.Format("Selecting AI target for item {0}", item.Data.DisplayName));

			//Select a random valid target to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			targetInput.TargetSelectCallback(targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)]);
		}

		public virtual void SelectTargets(Item item, TargetInputNumActors targetInput){
			VerboseLogger.Log(string.Format("Selecting AI targets for item {0}", item.Data.DisplayName));

			//Select random valid targets to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			List<int> availableTargetIndices = Enumerable.Range(0, targetInput.ValidTargets.Length).ToList();
			Actor[] chosenTargets = new Actor[targetInput.TargetsRequired];

			for(int i = 0; i < targetInput.TargetsRequired; i++){
				int chosenIndex = Random.Range(0, availableTargetIndices.Count);

				chosenTargets[i] = targetInput.ValidTargets[availableTargetIndices[chosenIndex]];
				availableTargetIndices.ToList().RemoveAt(chosenIndex);
			}

			targetInput.TargetSelectCallback(chosenTargets);
		}

		public virtual void SelectTargets(Item item, TargetInputGroup targetInput){
			VerboseLogger.Log(string.Format("Selecting AI target for item {0}", item.Data.DisplayName));

			//Select a random valid target to cast the ability on. You will likely want to override or
			//otherwise extend this method to create some more intelligent decision-making.

			targetInput.TargetSelectCallback(targetInput.ValidTargets[Random.Range(0, targetInput.ValidTargets.Length)]);
		}
	}
}