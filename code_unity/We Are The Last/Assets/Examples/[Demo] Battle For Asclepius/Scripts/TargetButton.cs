using UnityEngine;
using UnityEngine.UI;

namespace Ares.Examples {
	public class TargetButton : Button {
		Image background;
		Text label;

		protected override void Awake(){
			base.Awake();
			background = GetComponent<Image>();
			label = transform.GetChild(1).GetComponent<Text>();
		}

		public void SetAppearance(Color color, string text){
			background.color = color;
			label.text = text;
		}
	}
}