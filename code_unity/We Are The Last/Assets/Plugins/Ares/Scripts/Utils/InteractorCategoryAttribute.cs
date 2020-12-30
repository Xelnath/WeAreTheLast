using UnityEngine;
using Ares.Development;

namespace Ares {
	public class InteractorCategoryAttribute : PropertyAttribute {
		public InteractorCategories.CategoryType categoryType;

		public InteractorCategoryAttribute(InteractorCategories.CategoryType categoryType){
			this.categoryType = categoryType;
		}
	}
}