using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ares {
	public class LinearInventory : Inventory {
		public List<Item> Items {get; private set;}

		static Dictionary<SortMode, Dictionary<SortOrder, System.Func<Item, Item, int>>> sortingFunctions;
		static Dictionary<Item, int> itemAmounts; //For sort by amount

		static LinearInventory(){
			itemAmounts = new Dictionary<Item, int>();

			sortingFunctions = new Dictionary<SortMode, Dictionary<SortOrder, System.Func<Item, Item, int>>>{
				{SortMode.Name, new Dictionary<SortOrder, System.Func<Item, Item, int>>{
						{SortOrder.Ascending, (Item a, Item b) => {return a.Data.DisplayName.CompareTo(b.Data.DisplayName);}},
						{SortOrder.Descending, (Item a, Item b) => {return b.Data.DisplayName.CompareTo(a.Data.DisplayName);}}
					}
				},
				{SortMode.Amount, new Dictionary<SortOrder, System.Func<Item, Item, int>>{
						{SortOrder.Ascending, (Item a, Item b) => itemAmounts[a] - itemAmounts[b]},
						{SortOrder.Descending, (Item a, Item b) => itemAmounts[b] - itemAmounts[a]}
					}
				}
			};
		}

		public LinearInventory(){
			Items = new List<Item>();

			OnItemAdd = new Item_IntEvent();
			OnItemRemove = new Item_IntEvent();
		}

		public override void AddItem(Item itemTemplate, int amount){
			base.AddItem(itemTemplate, amount);

			AddItem(itemTemplate.Data, amount);
		}

		public override void AddItem(ItemData itemData, int amount){
			for(int i = 0; i < amount; i++){
				Item item = new Item(itemData);

				item.OnConsumed.AddListener(oldRemainingUses =>{
					if(item.RemainingUses <= 0){
						Items.Remove(item);
					}
				});

				OnItemAdd.Invoke(item, amount);

				Items.Add(item);
			}
		}

		public void RemoveItem(int index){
			OnItemRemove.Invoke(Items[index], 1);

			Items.RemoveAt(index);
		}

		public void RemoveItems(ItemData itemData){
			Item[] items = Items.Where(i => i.Data == itemData).ToArray();

			if(items.Length > 0){
				OnItemRemove.Invoke(items[0], items.Length);

				foreach(Item item in items){
					Items.Remove(item);
				}
			}
		}

		public void RemoveItem(ItemData itemData, int occurence){
			int[] itemIndices = Enumerable.Range(0, Items.Count).Where(i => Items[i].Data == itemData).ToArray();

			OnItemRemove.Invoke(Items[itemIndices[occurence]], 1);
			
			Items.RemoveAt(itemIndices[occurence]);
		}

		public override void Sort(SortMode mode, SortOrder order){
			if(mode == SortMode.Amount){ //Bit of prep needed
				var sortedGroups = Items.GroupBy(i => i, (Item key, IEnumerable<Item> collection) => new {Item = key, Count = collection.Count()});

				foreach(var group in sortedGroups){
					itemAmounts.Add(group.Item, group.Count);
				}
			}

			Items.Sort((a, b) => sortingFunctions[mode][order](a, b));
		}

		public override void Clear(){
			Items.Clear();
		}

		public Item[] GetFilteredItems(Filter filter){
			switch(filter){
				case Filter.All:
					return Items.ToArray();
				case Filter.ExcludePending:
					return Items.Where(i => i.PendingUseActors.Count == 0).ToArray();
				case Filter.PendingOnly:
					return Items.Where(i => i.PendingUseActors.Count > 0).ToArray();
			}

			throw new System.NotImplementedException(string.Format("Filter \"{0}\" not implemented", filter));
		}

		public override void UnmarkAllPending(){
			Items.ForEach(i => i.PendingUseActors.Clear());
		}

		public override string ToString(){
			return string.Format("[Linear Inventory ({0} {1})]\n-------------------------\n{2}",
				Items.Count,
				Items.Count > 1 ? "items" : "item",
				string.Join("\n", Items.Select(i => i.Data.DisplayName).ToArray())
			);
		}
	}
}