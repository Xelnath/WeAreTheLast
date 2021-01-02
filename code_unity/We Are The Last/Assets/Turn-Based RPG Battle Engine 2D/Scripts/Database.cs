using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ClassDB;
using Sirenix.OdinInspector;

[Guid("0A9B4ADA-B1D7-4857-B633-C100308C4D83")]
public class Database : MonoBehaviour {

	public static DatabaseScriptableObject staticDB;

	public DatabaseScriptableObject database;

	public static DatabaseScriptableObject dynamic;

	void Awake () { 
		if (dynamic == null) {
			staticDB = database;
			dynamic = ScriptableObject.CreateInstance<DatabaseScriptableObject>();
			dynamic.Copy( staticDB );
		} 
	}

}


//(c) Cination - Tsenkilidis Alexandros