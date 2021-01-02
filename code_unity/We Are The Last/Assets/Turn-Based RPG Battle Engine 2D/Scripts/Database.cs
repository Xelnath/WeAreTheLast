using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ClassDB;
using Sirenix.OdinInspector;

[Guid("0A9B4ADA-B1D7-4857-B633-C100308C4D83")]
public class Database : MonoBehaviour {

	public static DatabaseScriptableObject core;

	public DatabaseScriptableObject database;

	void Awake () { if (core == null) { core = database; } }

}


//(c) Cination - Tsenkilidis Alexandros