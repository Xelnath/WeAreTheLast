using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;
using Ares.Extensions;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Inventory Generator", menuName="Ares/Inventory Generator", order=71)]
	public class InventoryGenerator : ScriptableObject {
		public enum InventoryType {Linear, Stacked}
		public enum GroupSelectMode {ItemsPerGroup, EntireGroup}
		public enum SelectionLimit {None, MinimumAmount, MaximumAmount, Range}
		public enum SampleMode {Random, Sequential}

		[SerializeField] GuaranteedItemData[] guaranteedItems;
		[SerializeField] ChanceItemData[] chanceItems;
		[SerializeField] GroupItemData[] itemGroups;
		[SerializeField] int minChanceItems;
		[SerializeField] int maxChanceItems;
		[SerializeField] int minGroupPicks;
		[SerializeField] int maxGroupPicks;
		[SerializeField] SelectionLimit chanceSelection;
		[SerializeField] SampleMode chanceSampling;
		[SerializeField] bool allowChanceDuplicates;
		[SerializeField] GroupSelectMode groupSelectMode = GroupSelectMode.EntireGroup;
		[SerializeField] SelectionLimit groupSelection;
		[SerializeField] bool allowGroupDuplicates;

		public T GenerateInventory<T>() where T : Inventory, new(){
			T inventory = new T();
			bool breakAtMinimum = false;

			foreach(GuaranteedItemData guaranteedItemData in guaranteedItems){
				inventory.AddItem(guaranteedItemData.itemData, guaranteedItemData.amount);
			}

			// Random items
			if(chanceSelection == SelectionLimit.None){
				foreach(ChanceItemData chanceItemData in chanceItems){
					if(chanceItemData.chance > 0f && Random.value <= chanceItemData.chance){
						inventory.AddItem(chanceItemData.itemData, 1);
					}
				}
			}
			else{
				List<ChanceItemData> chanceItemCandidates = chanceItems.Where(i => i.itemData != null).ToList();

				if(chanceItemCandidates.Count > 0){
					chanceItemCandidates.Shuffle();

					int chanceItemsAdded = 0;
					breakAtMinimum = false;

					switch(chanceSelection){
						case SelectionLimit.MinimumAmount:
							while(chanceItemsAdded < minChanceItems){
								for(int i = chanceItemCandidates.Count - 1; i > -1; i--){
									ChanceItemData chanceItemData = chanceItemCandidates[i];

									if(chanceItemData.chance > 0f && Random.value <= chanceItemData.chance){
										inventory.AddItem(chanceItemData.itemData, 1);
										chanceItemsAdded++;

										if(breakAtMinimum && chanceItemsAdded == minChanceItems){
											break;
										}
										else
										if(!allowChanceDuplicates){
											chanceItemCandidates.RemoveAt(i);
										}
									}
								}

								breakAtMinimum = true;
							}
							break;
						case SelectionLimit.MaximumAmount:
							foreach(ChanceItemData chanceItemData in chanceItemCandidates){
								if(chanceItemData.chance > 0f && Random.value <= chanceItemData.chance){
									inventory.AddItem(chanceItemData.itemData, 1);
									chanceItemsAdded++;

									if(chanceItemsAdded == maxChanceItems){
										break;
									}
								}
							}
							break;
						case SelectionLimit.Range:
							Debug.Assert(minChanceItems < maxChanceItems);

							while(chanceItemsAdded < minChanceItems){
								for(int i = chanceItemCandidates.Count - 1; i > -1; i--){
									ChanceItemData chanceItemData = chanceItemCandidates[i];

									if(chanceItemData.chance > 0f && Random.value <= chanceItemData.chance){
										inventory.AddItem(chanceItemData.itemData, 1);
										chanceItemsAdded++;

										if((breakAtMinimum && chanceItemsAdded == minChanceItems) || chanceItemsAdded == maxChanceItems){
											break;
										}
										else
										if(!allowChanceDuplicates){
											chanceItemCandidates.RemoveAt(i);
										}
									}
								}

								breakAtMinimum = true;
							}
							break;
					}
				}
			}

			// Item groups
			List<GroupItemData> groupCandidates = itemGroups.Where(g => g.group != null).ToList();
	
			if(groupCandidates.Count > 0){
				groupCandidates.Shuffle();

				int groupsProcessed = 0;
				breakAtMinimum = false;

				System.Action<GroupItemData, Inventory> processItemGroupDelegate = null;

				switch(groupSelectMode){
					case GroupSelectMode.ItemsPerGroup:	processItemGroupDelegate = ProcessItemGroupIndividualItems;	break;
					case GroupSelectMode.EntireGroup:	processItemGroupDelegate = ProcessItemGroupInEntirity;		break;
				}

				switch(groupSelection){
					case SelectionLimit.None:
						foreach(GroupItemData groupItemData in itemGroups){
							if(groupItemData.chance > 0f && Random.value < groupItemData.chance){ //allow for min/ max groups?
								processItemGroupDelegate(groupItemData, inventory);
							}
						}
						break;
					case SelectionLimit.MinimumAmount:
						while(groupsProcessed < minGroupPicks){
							for(int i = groupCandidates.Count - 1; i > -1; i--){
								processItemGroupDelegate(groupCandidates[i], inventory);
								groupsProcessed++;

								if(breakAtMinimum && groupsProcessed == minGroupPicks){
									break;
								}
								else
									if(!allowGroupDuplicates){
										groupCandidates.RemoveAt(i);
									}
							}

							breakAtMinimum = true;
						}
						break;
					case SelectionLimit.MaximumAmount:
						for(int i = groupCandidates.Count - 1; i > -1; i--){
							processItemGroupDelegate(groupCandidates[i], inventory);
							groupsProcessed++;

							if(breakAtMinimum && groupsProcessed == maxGroupPicks){
								break;
							}
							else
								if(!allowGroupDuplicates){
									groupCandidates.RemoveAt(i);
								}
						}

						breakAtMinimum = true;
						break;
					case SelectionLimit.Range:
						while(groupsProcessed < minGroupPicks){
							for(int i = groupCandidates.Count - 1; i > -1; i--){
								processItemGroupDelegate(groupCandidates[i], inventory);
								groupsProcessed++;

								if((breakAtMinimum && groupsProcessed == minChanceItems) || groupsProcessed == maxChanceItems){
									break;
								}
								else
									if(!allowChanceDuplicates){
										groupCandidates.RemoveAt(i);
									}
							}

							breakAtMinimum = true;
						}
						break;
				}
			}
			return inventory;
		}

		void ProcessItemGroupIndividualItems(GroupItemData groupItemData, Inventory inventory){
			List<ItemData> groupItemCandidates = groupItemData.group.ItemData.Where(i => i != null).ToList();
			int itemsToPick = Random.Range(groupItemData.minItems, groupItemData.maxItems + 1);

			for(int i = 0; i < itemsToPick; i++){
				int index = Random.Range(0, groupItemCandidates.Count);

				inventory.AddItem(groupItemCandidates[index], 1);

				if(!groupItemData.allowDuplicatePicks && itemsToPick <= groupItemData.group.ItemData.Length){
					groupItemCandidates.RemoveAt(index);
				}
			}
		}

		void ProcessItemGroupInEntirity(GroupItemData groupItemData, Inventory inventory){
			foreach(ItemData itemData in groupItemData.group.ItemData){
				inventory.AddItem(itemData, 1);
			}
		}
	}
}