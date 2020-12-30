using UnityEngine;

namespace Ares {
	public class PowerData : ScriptableObject {
		public float PowerIncrement {get{return powerIncrement;}}
		public float PowerMultiplier {get{return powerMultiplier;}}
		public string PowerFormula {get{return powerFormula;}}
		public PowerScaling PowerScalingMode {get{return powerScalingMode;}}
		public int MinStage {get{return minStage;}}
		public int MaxStage {get{return maxStage;}}

		[SerializeField] float powerIncrement = 1f;
		[SerializeField] float powerMultiplier = 1f;
		[SerializeField] string powerFormula;
		[SerializeField] PowerScaling powerScalingMode;
		[SerializeField] int minStage = -5;
		[SerializeField] int maxStage = 5;

		public float GetScaledPowerFloat(float basePower, int stage, bool allowNegative = false){
			float result = 0f;

			switch(powerScalingMode){
				case PowerScaling.Linear:
					result = GetPowerLinear(basePower, stage);
					break;
				case PowerScaling.ExponentialSimple:
					result = GetPowerExponentialSimple(basePower, stage);
					break;
				case PowerScaling.ExponentialSimpleSymmetric:
					result = GetPowerExponentialSimpleSymmetric(basePower, stage);
					break;
				case PowerScaling.ExponentialComplex:
					result = GetPowerExponentialComplex(basePower, stage);
					break;
				case PowerScaling.ExponentialComplexSymmetric:
					result = GetPowerExponentialComplexSymmetric(basePower, stage);
					break;
				case PowerScaling.Custom:
					result = GetPowerCustom(stage);
					break;
			}

			if(!allowNegative){
				result = Mathf.Max(0f, result);
			}

			return result;
		}

		public float GetScaledPowerInt(float basePower, int stage, bool allowNegative = false){
			return Mathf.RoundToInt(GetScaledPowerFloat(basePower, stage, allowNegative));
		}

		float GetPowerLinear(float basePower, int stage){
			return basePower + stage * powerIncrement;
		}

		float GetPowerExponentialSimple(float basePower, int stage){
			return basePower * Mathf.Pow(powerIncrement, stage);
		}

		float GetPowerExponentialSimpleSymmetric(float basePower, int stage){
			float absPower = basePower * Mathf.Pow(powerIncrement, Mathf.Abs(stage));
			return stage > -1 ? absPower : (basePower - (absPower - basePower));
		}

		float GetPowerExponentialComplex(float basePower, int stage){
			return basePower + powerMultiplier * Mathf.Pow(powerIncrement, stage);
		}

		float GetPowerExponentialComplexSymmetric(float basePower, int stage){
			float absPower = basePower + powerMultiplier * Mathf.Pow(powerIncrement, Mathf.Abs(stage));
			return stage > -1 ? absPower : (basePower - (absPower - basePower));
		}

		float GetPowerCustom(int stage){
			string formula = powerFormula.Replace("STAGE", stage.ToString());

			return FormulaParser.Parse(formula);
		}
	}
}