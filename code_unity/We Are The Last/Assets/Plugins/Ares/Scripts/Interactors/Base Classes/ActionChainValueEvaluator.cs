using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ares {
	[System.Serializable]
	public class ActionChainValueEvaluator {
		/* So this is a bit of a weird construction. Ideally this would be a class
		 * that contains the type, value1, value2 and formula members. However, initially this
		 * construct was only used inside ChainableAction, and not having foreseen wanting to
		 * reuse it I simply made them part of that class instead.
		 * In order to keep old projects working and able to upgrade I opted to create this
		 * base class to at least share the functionality part of it between ChainableActions
		 * and ActionTokens.
		 */

		public enum PowerType {Constant, Random, Formula} //Oh how I wish I could rename this to EvaluationType without breaking old assets at least T.T

		static Regex reFormula = new Regex(@"([A-Z_]+[0-9]*(_RAW)*(?![\(A-Z0-9]))"); //Matches variable references like ATTACK1, (but not functions like ABS(...))

		public float EvaluateValues(PowerType type, Dictionary<string, float> evaluatedValues, float value1, float value2, string valueFormula){
			switch(type){
				case PowerType.Constant:
					return value1;
				case PowerType.Random:
					return Random.Range(value1, value2);
				case PowerType.Formula:
					string formula = reFormula.Replace(valueFormula, m => evaluatedValues[m.Value].ToString());

					return FormulaParser.Parse(formula);
			}

			return 0f;
		}
	}
}