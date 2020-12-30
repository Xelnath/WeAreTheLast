using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ares {
	public class EnvironmentVariableCallback {
		public Action<Battle, EnvironmentVariable> SetCallback {get; private set;}
		public Action<Battle, EnvironmentVariable> UnsetCallback {get; private set;}
		public Action<Battle, EnvironmentVariable> ProcessCallback {get; private set;}

		public EnvironmentVariableCallback(Action<Battle, EnvironmentVariable> setCallback, Action<Battle, EnvironmentVariable> unsetCallback,
			Action<Battle, EnvironmentVariable> processCallback){
			SetCallback = setCallback;
			UnsetCallback = unsetCallback;
			ProcessCallback = processCallback;
		}
	}

	public static class EnvironmentVariableCallbacks {
		static Dictionary<string, EnvironmentVariableCallback> callbacks;

		static EnvironmentVariableCallbacks(){
			//NOTE: The callback key is the EnvironmentVariableData object's name; NOT the display name!
			callbacks = new Dictionary<string, EnvironmentVariableCallback>(StringComparer.OrdinalIgnoreCase){
				//Example:
				//{"darkness", new EnvironmentVariableCallback(OnSetDarkness, OnUnsetDarkness, (battle, envVar) => {DamageAll(battle, envVar, [customParams]);})}
			};
		}

		public static void RegisterCallback(string identifier, Action<Battle, EnvironmentVariable> setCallback, Action<Battle, EnvironmentVariable> unsetCallback,
			Action<Battle, EnvironmentVariable> processCallback){
			callbacks.Add(identifier, new EnvironmentVariableCallback(setCallback, unsetCallback, processCallback));
		}

		public static void RegisterCallback(string identifier, EnvironmentVariableCallback callback){
			callbacks.Add(identifier, callback);
		}

		public static void HandleSet(Battle battle, EnvironmentVariable envVar){
			VerboseLogger.Log("Processing environment variable set callback for data: " + envVar.Data.name);

			if(callbacks.ContainsKey(envVar.Data.name)){
				HandleCallback(callbacks[envVar.Data.name].SetCallback, battle, envVar);
			}
		}

		public static void HandleUnset(Battle battle, EnvironmentVariable envVar){
			VerboseLogger.Log("Processing environment variable unset callback for data: " + envVar.Data.name);

			if(callbacks.ContainsKey(envVar.Data.name)){
				HandleCallback(callbacks[envVar.Data.name].UnsetCallback, battle, envVar);
			}
		}

		public static void HandleProcess(Battle battle, EnvironmentVariable envVar){
			VerboseLogger.Log("Processing environment variable process callback for data: " + envVar.Data.name);

			if(callbacks.ContainsKey(envVar.Data.name)){
				HandleCallback(callbacks[envVar.Data.name].ProcessCallback, battle, envVar);
			}
		}

		static void HandleCallback(System.Action<Battle, EnvironmentVariable> callback, Battle battle, EnvironmentVariable envVar){
			if(callback != null){
				callback(battle, envVar);
			}
		}
	}
}