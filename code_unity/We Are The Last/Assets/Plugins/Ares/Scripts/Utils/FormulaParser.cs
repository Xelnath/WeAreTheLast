using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;

namespace Ares {
	public static class FormulaParser {
		static readonly Regex reParens = new Regex(@"([A-Z]*)\(([^\(\)]+)\)"); //Matches all inner-most parentheses
		static readonly Regex reDice = new Regex(@"(\d+)[dD](\d+)"); //Matches all dice rolls in format ndn or nDn
		static readonly Regex reWhitespace = new Regex(@"\s+");  //Matches all whitespace characters

		public static float Parse(string formula){
			#if ENABLE_ARES_VERBOSE_LOGGING
			string originalFormula = formula;
			#endif

			formula = reDice.Replace(formula, m => {
				int numDice = Convert.ToInt32(m.Groups[1].Value);
				int numSides = Convert.ToInt32(m.Groups[2].Value);
				int result = 0;

				for(int i=0; i<numDice; i++){
					result += UnityEngine.Random.Range(1, numSides+1);
				}

				return result.ToString();
			});

			while(reParens.Matches(formula).Count > 0){
				formula = reParens.Replace(formula, m =>{
					switch(m.Groups[1].Value){
						case "ABS":
							return Mathf.Abs(Calculate(m.Groups[2].Value)).ToString();
						case "MIN":
							string[] minValues = reWhitespace.Replace(m.Groups[2].Value, "").Split(',');
							return Mathf.Min(float.Parse(minValues[0]), float.Parse(minValues[1])).ToString();
						case "MAX":
							string[] maxValues = reWhitespace.Replace(m.Groups[2].Value, "").Split(',');
							return Mathf.Max(float.Parse(maxValues[0]), float.Parse(maxValues[1])).ToString();
						default:
							return Calculate(m.Groups[2].Value).ToString();

					}
				});
			}

			float finalResult = Calculate(formula);

			#if ENABLE_ARES_VERBOSE_LOGGING
				#if UNITY_EDITOR
				if(Application.isPlaying){
				#endif
					VerboseLogger.Log("Power formula `" + originalFormula + "` evaluated to " + finalResult.ToString());
				#if UNITY_EDITOR
				}
			#endif
			#endif

			return finalResult;
		}

		// Credit to user automox1 for the initial implementation:
		// (http://community.monogame.net/t/how-can-i-compute-math-3-2-4-from-string-to-number/7574/4,
		//  https://codedump.io/share/IHPFKE0PrbqU/1/basic-string-calculator)
		static float Calculate(string str){  // solves math strings with +, -, /, and * only
			str = reWhitespace.Replace(str, "");
			List<string> b1 = new List<string>();

			b1.Add(str);
			Match b1Match;
			
			while(true){
				string lastString = b1.Last();
				b1Match = Regex.Match(lastString, @"\d([+-])[+-]?\d"); // break by + -

				if(b1Match == null || b1Match.Value == ""){
					break;
				}
				else{
					int opIndex = b1Match.Groups[1].Index;

					b1[b1.Count - 1] = (lastString.Substring(0, opIndex));
					b1.Add(lastString[opIndex].ToString());
					b1.Add(lastString.Substring(opIndex+1));
				}
			}

			for(int i = 0; i < b1.Count; i++){
				string[] b2 = Regex.Split(b1[i], @"([*/])"); // break by * /

				for(int j = 0; j < b2.Length; j++){
					string[] b3 = Regex.Split(b2[j], @"([\^])"); // break by ^

					while(b3.Length>2){
						// powers get calculated first
						b3[2] = Convert.ToString(Mathf.Pow(Convert.ToSingle(b3[0]), Convert.ToSingle(b3[2])));
						b3 = b3.Skip(2).ToArray();
					}

					b2[j] = b3[0];
				}

				while(b2.Length>2){
					// multiplication and division gets calculated second
					if (b2[1] == "*") b2[2] = Convert.ToString(Convert.ToSingle(b2[0]) * Convert.ToSingle(b2[2]));
					if (b2[1] == "/") b2[2] = Convert.ToString(Convert.ToSingle(b2[0]) / Convert.ToSingle(b2[2]));

					b2 = b2.Skip(2).ToArray();
				}

				b1[i] = b2[0];
			}

			while(b1.Count > 2){
				// addition and subtraction gets calculated last
				if (b1[1] == "+") b1[2] = Convert.ToString(Convert.ToSingle(b1[0]) + Convert.ToSingle(b1[2]));
				if (b1[1] == "-") b1[2] = Convert.ToString(Convert.ToSingle(b1[0]) - Convert.ToSingle(b1[2]));

				b1 = b1.Skip(2).ToList();
			}
			
			return Convert.ToSingle(b1[0]);
		}
	}
}