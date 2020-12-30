using UnityEngine;

namespace Ares {
	[System.Serializable]
	class GuaranteedItemData{
		public ItemData itemData;
		public int amount = 1;

		public GuaranteedItemData(ItemData itemData, int amount){
			this.itemData = itemData;
			this.amount = amount;
		}
	}

	[System.Serializable]
	class ChanceItemData{
		public ItemData itemData;
		[Range(0f, 1f)] public float chance = 1f;

		public ChanceItemData(ItemData itemData, float chance){
			this.itemData = itemData;
			this.chance = chance;
		}
	}

	[System.Serializable]
	class GroupItemData{
		public ItemGroup group;
		[Range(0f, 1f)] public float chance = 1f;
		public int minItems;
		public int maxItems;
		public bool allowDuplicatePicks;

		public GroupItemData(ItemGroup group, float chance, int minItems, int maxItems, bool allowDuplicatePicks){
			this.group = group;
			this.chance = chance;
			this.minItems = minItems;
			this.maxItems = maxItems;
			this.allowDuplicatePicks = allowDuplicatePicks;
		}
	}

	public class InventoryBuilder : MonoBehaviour {
		[SerializeField] InventoryGenerator.InventoryType inventoryType = InventoryGenerator.InventoryType.Stacked;
		[SerializeField] bool assignToActorOnAwake = true;
		[SerializeField] InventoryGenerator generator;

		void Awake(){
			if(assignToActorOnAwake){
				Actor actor = GetComponent<Actor>();

				if(actor != null){
					actor.inventory = GenerateInventory();
				}
			}
		}

		public Inventory GenerateInventory(){
			switch(inventoryType){
				case InventoryGenerator.InventoryType.Linear:
					return generator.GenerateInventory<LinearInventory>();
				case InventoryGenerator.InventoryType.Stacked:
					return generator.GenerateInventory<StackedInventory>();
			}

			return null;
		}

		public T GenerateInventory<T>() where T : Inventory, new() {
			return generator.GenerateInventory<T>();
		}
	}
}