using UnityEngine;

namespace Ares {
	public class TemporaryBuff {
		public StatData Stat {get{return stat;}}
		public int Stages {get{return stages;}}
		public int TurnsRemaining {get{return turnsRemaining;} set{turnsRemaining = Mathf.Max(0, value);}}

		[SerializeField] StatData stat;
		[SerializeField] int stages = 0;
		[SerializeField] int turnsRemaining = 0;

		public TemporaryBuff(StatData stat, int stages, int duration){
			this.stat = stat;
			this.stages = stages;
			turnsRemaining = duration;
		}
	}
}