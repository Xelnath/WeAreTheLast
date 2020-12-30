using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Ares {
	public class StackedItem {
		public Item Item {get; private set;}
		public int Amount {
			get{
				return amount;
			}
			set{
				if(value <= 0){
					OnRunOut.Invoke();
					amount = value;
				}
				else{
					amount = value;
				}
			}
		}

		public UnityEvent OnRunOut {get; private set;}

		int amount;

		public StackedItem(Item item, int amount){
			Item = item;
			Amount = amount;

			OnRunOut = new UnityEvent();
		}

		public void Consume(int amount){
			Amount -= amount;

		}

		public void ConsumeAll(){
			Consume(amount);
		}

		public int GetFilteredAmount(Inventory.Filter filter){
			switch(filter){
				case Inventory.Filter.All:
					return Amount;
				case Inventory.Filter.ExcludePending:
					return Amount - Item.PendingUseActors.Count;
				case Inventory.Filter.PendingOnly:
					return Item.PendingUseActors.Count;
			}

			throw new System.NotImplementedException(string.Format("Filter \"{0}\" not implemented", filter));
		}
	}

	public class StackedInventory : Inventory {
		public List<StackedItem> Items {get; private set;}

		static Dictionary<SortMode, Dictionary<SortOrder, System.Func<StackedItem, StackedItem, int>>> sortingFunctions;

		static StackedInventory(){
			sortingFunctions = new Dictionary<SortMode, Dictionary<SortOrder, System.Func<StackedItem, StackedItem, int>>>{
				{SortMode.Name, new Dictionary<SortOrder, System.Func<StackedItem, StackedItem, int>>{
						{SortOrder.Ascending, (StackedItem a, StackedItem b) => a.Item.Data.DisplayName.CompareTo(b.Item.Data.DisplayName)},
						{SortOrder.Descending, (StackedItem a, StackedItem b) => b.Item.Data.DisplayName.CompareTo(a.Item.Data.DisplayName)}
					}
				},
				{SortMode.Amount, new Dictionary<SortOrder, System.Func<StackedItem, StackedItem, int>>{
						{SortOrder.Ascending, (StackedItem a, StackedItem b) => a.Amount - b.Amount},
						{SortOrder.Descending, (StackedItem a, StackedItem b) => b.Amount - a.Amount}
					}
				}
			};
		}

		public StackedInventory(){
			Items = new List<StackedItem>();

			OnItemAdd = new Item_IntEvent();
			OnItemRemove = new Item_IntEvent();
		}

		public override void AddItem(Item itemTemplate, int amount){
			base.AddItem(itemTemplate, amount);

			StackedItem inventoryItem = Items.FirstOrDefault(i => i.Item.Data == itemTemplate.Data && i.Item.RemainingUses == itemTemplate.RemainingUses);

			if(inventoryItem != null){
				inventoryItem.Amount += amount;
			}
			else{
				StackedItem stackedItem = new StackedItem(new Item(itemTemplate.Data, itemTemplate.RemainingUses), amount);

				HandleItemAdd(stackedItem, amount);
			}
		}

		public override void AddItem(ItemData itemData, int amount){
			StackedItem inventoryItem = Items.FirstOrDefault(i => i.Item.Data == itemData && i.Item.RemainingUses == itemData.Uses);

			if(inventoryItem != null){
				inventoryItem.Amount += amount;
			}
			else{
				StackedItem stackedItem = new StackedItem(new Item(itemData), amount);

				HandleItemAdd(stackedItem, amount);
			}
		}

		void HandleItemAdd(StackedItem stackedItem, int amount){
			stackedItem.Item.OnConsumed.AddListener(oldRemainingUses => {
				stackedItem.Item.Replenish(1, false);

				if(stackedItem.Item.RemainingUses > 1){ //Move to new stack
					Item newItem = (Item)stackedItem.Item.Clone();
					newItem.Consume(1, false);
					AddItem(newItem, 1);
				}

				stackedItem.Consume(1); //Consume 1 item
			});

			stackedItem.OnRunOut.AddListener(() => {
				Items.Remove(stackedItem);
			});

			OnItemAdd.Invoke(stackedItem.Item, amount);

			Items.Add(stackedItem);
		}

		public void RemoveItem(Item item, int amount){
			StackedItem inventoryItem = Items.FirstOrDefault(i => i.Item.Data == item.Data && i.Item.RemainingUses == item.RemainingUses);

			if(inventoryItem != null){
				inventoryItem.Amount -= amount;

				if(inventoryItem.Amount <= 0){
					OnItemRemove.Invoke(inventoryItem.Item, 1);

					Items.Remove(inventoryItem);
				}
			}
		}

		public void RemoveItem(ItemData itemData, int amount){
			StackedItem inventoryItem = Items.FirstOrDefault(i => i.Item.Data == itemData && i.Item.RemainingUses == itemData.Uses);

			if(inventoryItem != null){
				inventoryItem.Amount -= amount;

				if(inventoryItem.Amount <= 0){
					OnItemRemove.Invoke(inventoryItem.Item, 1);

					Items.Remove(inventoryItem);
				}
			}
		}

		public override void Sort(SortMode mode, SortOrder order){
			Items.Sort((a, b) => sortingFunctions[mode][order](a, b));
		}

		public override void Clear(){
			Items.Clear();
		}

		public StackedItem[] GetFilteredItems(Filter filter){
			switch(filter){
				case Filter.All:
					return Items.ToArray();
				case Filter.ExcludePending:
					return Items.Where(i => i.GetFilteredAmount(filter) > 0).ToArray();
				case Filter.PendingOnly:
					return Items.Where(i => i.GetFilteredAmount(filter) > 0).ToArray();
			}

			throw new System.NotImplementedException(string.Format("Filter \"{0}\" not implemented", filter));
		}

		public override void UnmarkAllPending(){
			Items.ForEach(i => i.Item.PendingUseActors.Clear());
		}

		public override string ToString(){
			return string.Format("[Stacked Inventory ({0} {1})]\n-------------------------\n{2}",
				Items.Sum(i => i.Amount),
				(Items.Count > 1 || (Items.Count > 0 && Items[0].Amount > 1)) ? "items" : "item",
				string.Join("\n", Items.Select(i => string.Format("{0}    ({1})",
					i.Item.Data.DisplayName,
					i.Amount)).ToArray()
				));
		}
	}
}