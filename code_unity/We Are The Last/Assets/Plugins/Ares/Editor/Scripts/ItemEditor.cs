using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(ItemData), true)]
	public class ItemDataEditor : BattleInteractorEditor {
		readonly string[] customProperties = {"uses", "mockUserAsTarget"};

		protected override string[] GetPropertyGroup1(){
			List<string> propertyGroup1 = base.GetPropertyGroup1().ToList();

			propertyGroup1.Insert(4, "uses");

			if(serializedObject.FindProperty("targetType").enumValueIndex == (int)BattleInteractorData.TargetType.SingleActor){
				propertyGroup1.Insert(propertyGroup1.Count, "mockUserAsTarget");
			}

			return propertyGroup1.ToArray();
		}

		protected override string[] GetExcludedProperties(){
			return base.GetExcludedProperties().Concat(customProperties).ToArray();
		}
	}
}