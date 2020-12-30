using UnityEngine;
using UnityEngine.UI;

namespace Ares.Examples {
	public class AbilityButton : Button {
		Image background;
		Image icon;
		Text label;

		protected override void Awake(){
			base.Awake();
			background = GetComponent<Image>();
			icon = transform.GetChild(1).GetComponent<Image>();
			label = transform.GetChild(2).GetComponent<Text>();
		}

		public void SetAppearance(Color color, Sprite icon, string text){
			background.color = color;
			this.icon.sprite = icon;
			this.icon.enabled = icon != null;
			label.text = text;
		}
	}
}