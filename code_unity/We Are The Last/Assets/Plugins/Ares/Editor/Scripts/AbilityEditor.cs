using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(AbilityData), true)]
	public class AbilityEditor : BattleInteractorEditor {
		readonly string[] customProperties = {"priority"};

		protected override string[] GetPropertyGroup1(){
			List<string> propertyGroup1 = base.GetPropertyGroup1().ToList();

			propertyGroup1.Insert(4, "priority");

			return propertyGroup1.ToArray();
		}

		protected override string[] GetExcludedProperties(){
			return base.GetExcludedProperties().Concat(customProperties).ToArray();
		}
	}
}