using UnityEngine;
using System.Linq;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Item", menuName="Ares/Item Group", order=70)]
	public class ItemGroup : ScriptableObject {
		public string DisplayName {get{return displayName;}}
		public ItemData[] ItemData {get{return itemData;}}

		[SerializeField, Header("Info")] string displayName;
		[SerializeField, Header("Items")] ItemData[] itemData;
	}
}