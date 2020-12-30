using UnityEngine;

namespace Ares {
	public class BattleMonoBehaviour : MonoBehaviour {
		public static BattleMonoBehaviour Instance {
			get{
				if(instance == null){
					GameObject go = new GameObject("[Ares] Battle GameObject");
					instance = go.AddComponent<BattleMonoBehaviour>();
				}

				return instance;
			}
		}

		static BattleMonoBehaviour instance;
	}
}