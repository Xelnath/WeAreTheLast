/* A very simple name display component that can be used with
 * Unity UI's standard Text.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Ares.UI {
	[RequireComponent(typeof(Text))]
	public class NameDisplay : MonoBehaviour {
		[SerializeField] Actor actor;

		Text text;

		void Awake(){
			text = GetComponent<Text>();
		}

		void Start(){
			text.text = actor.DisplayName;
		}
	}
}