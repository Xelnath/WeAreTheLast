using UnityEngine;

namespace Ares {
	[System.Serializable]
	public class AfflictionAction : ChainableAction {
		public enum TargetType {Afflicted, Afflicter}

		public TargetType TargetMode {get{return targetType;}}

		[SerializeField] TargetType targetType;
	}
}