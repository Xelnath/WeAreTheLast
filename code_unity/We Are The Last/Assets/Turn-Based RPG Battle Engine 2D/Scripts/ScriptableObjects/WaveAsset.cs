using System;
using System.Collections;
using System.Collections.Generic;
using ClassDB;
using UnityEngine;
using UnityEditor;

[Serializable]
public class WaveAction
{
	public List<callInfo> actions;
}

[Serializable]
public class Wave
{
	public List<int> Creatures = new List<int>();
	public List<WaveAction> onSpawn = new List<WaveAction>();
}

public class WaveAsset : ScriptableObject
{
	public int OrderID = 1;
	public bool Disabled = false;

	public Wave wave;
}
