using UnityEngine;

namespace Ares.Examples {
	public class UnparentOnAwake : MonoBehaviour {
		void Awake(){
			transform.SetParent(null, true);
		}
	}
}