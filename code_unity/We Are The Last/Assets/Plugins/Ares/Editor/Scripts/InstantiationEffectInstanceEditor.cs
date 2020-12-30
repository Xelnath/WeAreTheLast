using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Ares.Editor {
	[CustomEditor(typeof(InstantiationEffectInstance), true)]
	public class InstantiationEffectInstanceEditor : AresEditor {
		public override void OnInspectorGUI(){
			serializedObject.Update();
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();

			InstantiationEffectEditor.lastInspectorWindowShown = serializedObject.targetObject;
		}
	}
}