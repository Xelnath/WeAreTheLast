using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ares {
	[Serializable]
	public class ChainableAction : ActionChainValueEvaluator {
		public enum EnvironmentVariableSetType {Set, Unset}

		public ChainEvaluator.ActionType Action {get{return action;}}
		public PowerType PowerMode {get{return powerType;}}
		public PowerType SpecialMode {get{return specialType; } }
		public AfflictionData Affliction {get{return affliction;}}
		public StatData Stat {get{return stat;}}
		public EnvironmentVariableData EnvironmentVariable {get{return environmentVariable;}}
		public EnvironmentVariableSetType EnvironmentVariableSetMode {get{return environmentVariableSetType;}}
		public float HitChance {get{return chance;}}
		public bool IsChildEffect {get{return isChildEffect;}}
		public float Duration {get{return duration;}}
		public float NormalizedProcessTime {get{return normalizedProcessTime;}}
		public bool BreaksChainOnMiss {get{return breaksChainOnMiss;}}

		[SerializeField] ChainEvaluator.ActionType action;
		[SerializeField] PowerType powerType;
		[SerializeField] PowerType specialType;
		[SerializeField] StatData stat;
		[SerializeField] EnvironmentVariableData environmentVariable;
		[SerializeField] EnvironmentVariableSetType environmentVariableSetType;
		[SerializeField] AfflictionData affliction;
		[SerializeField] bool isChildEffect = false;
		[SerializeField] float duration = 1f;
		[SerializeField, Range(0f, 1f)] float normalizedProcessTime = 1f;
		[SerializeField, Range(0f, 1f)] float chance = 1f;
		[SerializeField] float power1;
		[SerializeField] float power2;
		[SerializeField] string powerFormula;
		[SerializeField] float special1;
		[SerializeField] float special2;
		[SerializeField] string specialFormula;
		[SerializeField] bool breaksChainOnMiss;

		public float EvaluatePower(Dictionary<string, float> evaluatedValues){
			return EvaluateValues(powerType, evaluatedValues, power1, power2, powerFormula);
		}

		public float EvaluateSpecial(Dictionary<string, float> evaluatedValues){
			return EvaluateValues(specialType, evaluatedValues, special1, special2, specialFormula);
		}

	}
}