using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ares {
	[Serializable]
	public class ActionToken : ActionChainValueEvaluator {
		public string ID { get { return id; } }
		public PowerType EvaluationMode { get { return valueType; } }

		[SerializeField] string id;
		[SerializeField] PowerType valueType;
		[SerializeField] float value1;
		[SerializeField] float value2;
		[SerializeField] string valueFormula;

		public float Evaluate(Dictionary<string, float> evaluatedValues){
			return EvaluateValues(valueType, evaluatedValues, value1, value2, valueFormula);
		}
	}
}