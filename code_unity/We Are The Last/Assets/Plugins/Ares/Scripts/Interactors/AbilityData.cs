using UnityEngine;
using Ares.Development;

namespace Ares {
	[CreateAssetMenu(fileName="New Ares Ability", menuName="Ares/Ability", order=50)]
	public class AbilityData : BattleInteractorData<AbilityAction> {
		public override string FullCategoryPath {get{ return InteractorCategories.GetCategoryForId(InteractorCategories.CategoryType.Ability, category);}}
		public int Priority {get{return priority;}}

		[SerializeField, InteractorCategoryAttribute(InteractorCategories.CategoryType.Ability)] int category;
		[SerializeField, Tooltip("Determines whether an ability holds priority over others. Higher priority abilities get evaluated before lower ones.")] int priority;
	}

	[System.Serializable] //Gets serialized on Actors
	public class Ability : BattleInteractor<AbilityData, AbilityAction> {
		public bool Enabled {get{return enabled;}}
		public bool OverrideBaseDuration {get{return overrideBaseDuration;}}
		public float BaseDuration {get{return OverrideBaseDuration ? baseDuration : Data.BaseDuration;}}

		[SerializeField] bool enabled;
		[SerializeField] bool overrideBaseDuration;
		[SerializeField] float baseDuration;
		
		public Ability(AbilityData data, bool enabled, bool overrideBaseDuration, float baseDuration){
			Data = data;

			this.enabled = enabled;
			this.overrideBaseDuration = overrideBaseDuration;
			this.baseDuration = baseDuration;
		}

		public Ability(AbilityData data) : this(data, true, false, 0f){}
	}
}