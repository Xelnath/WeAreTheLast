using UnityEngine;
using UnityEngine.UI;

namespace Ares.Examples {
	public class ItemButton : Button {
		[SerializeField] Text label;
		[SerializeField] Text amountLabel;

		public void SetLabels(StackedItem item){
			if(item != null){
				label.text = string.Format("{0}\n<size={1}>({2} {3})</size>", item.Item.Data.DisplayName, label.fontSize * .8f,
					item.Item.RemainingUses, item.Item.RemainingUses > 1 ? "uses" : "use");
				
				amountLabel.text = item.GetFilteredAmount(Inventory.Filter.ExcludePending).ToString();
			}
			else{
				label.text = amountLabel.text = "";
			}
		}
	}
}