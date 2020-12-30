using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ares.Development {
	[CreateAssetMenu(fileName="New Item Categories Object", menuName="Ares/Item Categories", order=10)]
	public class InteractorCategories : ScriptableObject {
		public enum CategoryType {Ability, Item}

		public CategoryType Type {get{return categoryType;}}
		public int MaxId {get{return maxId;}}

		[SerializeField] CategoryType categoryType;
		[SerializeField] int maxId;
		[SerializeField] List<InteractorCategory> categories;

		public InteractorCategoriesData GetAll(){
			return new InteractorCategoriesData(categories.Select(c => c.path).ToArray(), categories.Select(c => c.id).ToArray());
		}

		public static string GetCategoryForId(CategoryType categoryType, int id){
			InteractorCategory category = Resources.LoadAll<InteractorCategories>("").First(c => c.categoryType == categoryType).categories.FirstOrDefault (c => c.id == id);
			return category.path == "None" ? "" : category.path;
		}
	}

	public class InteractorCategoriesData {
		public string[] Names {get; private set;}
		public int[] Ids {get; private set;}

		public InteractorCategoriesData(string[] names, int[] ids){
			Names = names;
			Ids = ids;
		}
	}

	[System.Serializable]
	public struct InteractorCategory {
		public int id;
		public string path;

		public InteractorCategory(int id, string path){
			this.id = id;
			this.path = path;
		}
	}
}