using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ares.Development;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Item", menuName="Ares/Item", order=53)]
	public class ItemData : BattleInteractorData<ItemAction> {
		public override string FullCategoryPath {get{return InteractorCategories.GetCategoryForId(InteractorCategories.CategoryType.Item, category);}}

		public int Uses {get{return uses;}}
		public bool MockUserAsTarget {get{return mockUserAsTarget;}}

		[SerializeField, InteractorCategoryAttribute(InteractorCategories.CategoryType.Item)] int category;
		[SerializeField] int uses = 1;
		[SerializeField, Tooltip("When checked, the item's user will always be set to the target, even if it was used by another actor on the other actor's turn.")]
		bool mockUserAsTarget;
	}

	public class Item : BattleInteractor<ItemData, ItemAction>, System.ICloneable {
		public IntEvent OnConsumed {get; private set;}
		public IntEvent OnReplenished {get; private set;}
		public int RemainingUses {get{return remainingUses;}}
		public List<Actor> PendingUseActors {get; private set;}

		int remainingUses = 1;

		public Item(ItemData data) : this(data, data.Uses){}

		public Item(ItemData data, int remainingUses){
			Data = data;
			this.remainingUses = remainingUses;

			OnConsumed = new IntEvent();
			OnReplenished = new IntEvent();
			PendingUseActors = new List<Actor>();
		}

		public void Consume(int amount, bool fireOnConsumed = true){
			int oldRemainingUses = remainingUses;

			remainingUses = Mathf.Max(0, remainingUses - amount);

			if(fireOnConsumed){
				OnConsumed.Invoke(oldRemainingUses);
			}
		}

		public void Replenish(int amount, bool fireOnReplenished = true){
			int oldRemainingUses = remainingUses;

			remainingUses = remainingUses + amount;

			if(fireOnReplenished){
				OnReplenished.Invoke(oldRemainingUses);
			}
		}

		public string GetDetailedDisplayName(string usesFormat = "(# uses)"){
			return Data.DisplayName + " " + usesFormat.Replace("#", RemainingUses.ToString());
		}

		public object Clone(){
			return new Item(Data, remainingUses);
		}
	}
}