using System.Collections.Generic;
using System.Linq;

namespace Ares {
	[System.Serializable]
	public abstract class ChainEvaluator {
		public enum ActionType {Damage, Heal, Buff, Afflict, Cure, Environment}

		/* Evaluated action values are stored per target actor. This allows for each
		 * target to use their own stats linked to the keywords. It also leaves the
		 * door open for other damage implementations, like dealing damage per
		 * actor in order, which might change caster stats as well (i.e.: from a
		 * lifesteal-like ability).
		 */

		protected Dictionary<string, float> evaluatedActionTokens; //Constant and Random only
		protected Dictionary<Actor, Dictionary<string, float>> evaluatedActionValues;
		protected Dictionary<ActionType, int> numActionTypes;
		protected Dictionary<ChainableAction, string> actionIdentifiers;

		public List<T> GetChainActionWithChildren<T>(List<T> allActions, T parentAction) where T : ChainableAction{
			List<T> results = new List<T>();
			results.Add(parentAction);

			int actionIndex = allActions.IndexOf(parentAction) + 1;

			while(actionIndex < allActions.Count){
				if(allActions[actionIndex].IsChildEffect){
					results.Add(allActions[actionIndex]);
					actionIndex++;
				}
				else{
					break;
				}
			}

			return results;
		}

		protected void PrepareForChainEvaluation(List<ActionToken> actionTokens, List<ChainableAction> actions, Actor caster, Actor[] targets){
			evaluatedActionTokens = actionTokens.ToDictionary(t => t.ID,
				t => t.EvaluationMode == ActionChainValueEvaluator.PowerType.Formula ? 0 : t.Evaluate(null));

			if(actionIdentifiers == null){
				actionIdentifiers = new Dictionary<ChainableAction, string>();
				evaluatedActionValues = new Dictionary<Actor, Dictionary<string, float>>();
			}
			else{
				actionIdentifiers.Clear();
				evaluatedActionValues.Clear();
			}

			numActionTypes = new Dictionary<ActionType, int>();

			foreach(ActionType type in System.Enum.GetValues(typeof(ActionType))){
				numActionTypes.Add(type, 0);
			}

			foreach(ChainableAction action in actions){
				ActionType type = action.Action;

				numActionTypes[type]++;

				actionIdentifiers.Add(action, type.ToString().ToUpper() + numActionTypes[type].ToString());
			}

			AddUninitializedDefaultTokens(caster, targets);
		}

		public float GetCachedEvaluatedPower(AbilityAction action, Actor target){
			return evaluatedActionValues[target][actionIdentifiers[action]];
		}

		public abstract void PrepareForChainEvaluation(Actor caster, Actor[] targets);
		protected abstract void AddUninitializedDefaultTokens(Actor caster, Actor[] targets);
	}
}