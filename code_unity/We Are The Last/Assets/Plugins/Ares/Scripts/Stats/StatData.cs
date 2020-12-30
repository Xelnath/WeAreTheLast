using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Stat", menuName="Ares/Stat", order=30)]
	public class StatData : PowerData {
		public static StatData[] All{
			get{
				#if UNITY_EDITOR
				return Resources.LoadAll<StatData>("Stats"); //always reload
				#else
				if(all == null){
					all = Resources.LoadAll<StatData>("Stats");
				}

				return all;
				#endif
			}
		}

		static StatData[] all;

		public string DisplayName {get{return displayName;}}

		public float GetValue(int baseValue, int stage){
			return GetScaledPowerFloat(baseValue, stage, true);
		}

		[SerializeField] string displayName;
	}

	[System.Serializable]
	public class Stat {
		public int Stage {get{return stage;}}
		public StatData Data {get{return data;}}
		public float Value {get{return data.GetValue(baseValue, stage);}}

		public int baseValue = 50;

		[SerializeField] StatData data;
		[SerializeField] int stage = 0;

		public Stat(StatData data){
			this.data = data;
		}

		public void Buff(int stages){
			stage = Mathf.Clamp(stage + stages, data.MinStage, data.MaxStage);
		}

		public void Debuff(int stages){
			stage = Mathf.Clamp(stage - stages, data.MinStage, data.MaxStage);
		}
	}
}