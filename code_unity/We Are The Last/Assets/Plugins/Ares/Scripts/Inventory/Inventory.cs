using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ares {
	public abstract class Inventory {
		public enum SortMode {Name, Amount}
		public enum SortOrder {Ascending, Descending}
		public enum Filter {All, ExcludePending, PendingOnly}

		public Item_IntEvent OnItemAdd {get; protected set;}
		public Item_IntEvent OnItemRemove {get; protected set;}

		public virtual void AddItem(Item item, int amount){
			OnItemAdd.Invoke(item, amount);
		}
			
		public abstract void AddItem(ItemData item, int amount);
		public abstract void Sort(SortMode mode, SortOrder order);
		public abstract void Clear();
		public abstract void UnmarkAllPending();
	}
}