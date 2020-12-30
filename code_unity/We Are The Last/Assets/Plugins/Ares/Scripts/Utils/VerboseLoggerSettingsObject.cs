using UnityEngine;

namespace Ares.Development {
	// [CreateAssetMenu(fileName="New Logger Settings", menuName="Ares/Verbose Logger", order=10)]
	public class VerboseLoggerSettingsObject : ScriptableObject {
		public Color RegularColor {get{return regularColor;}}
		public Color State1Color {get{return state1Color;}}
		public Color State2Color {get{return state2Color;}}
		public Color ActionColor {get{return actionColor;}}
		public Color UnimportantColor {get{return unimportantColor;}}

		[SerializeField] Color regularColor;
		[SerializeField] Color state1Color;
		[SerializeField] Color state2Color;
		[SerializeField] Color actionColor;
		[SerializeField] Color unimportantColor;
	}

	public static class VerboseLoggerSettings {
		public static Color RegularColor {get{return settingsObject.RegularColor;}}
		public static Color State1Color {get{return settingsObject.State1Color;}}
		public static Color State2Color {get{return settingsObject.State2Color;}}
		public static Color ActionColor {get{return settingsObject.ActionColor;}}
		public static Color UnimportantColor {get{return settingsObject.UnimportantColor;}}

		static VerboseLoggerSettingsObject settingsObject;

		static VerboseLoggerSettings(){
			settingsObject = Resources.Load<VerboseLoggerSettingsObject>("Verbose Logger Settings");
		}
	}
}