using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using UnityEditor;

//This script stores object references for easier access
public class AnimationAsset : ScriptableObject
{
	public string Name = "None";
	public bool Looping = false;
	public Sprite[] sprites;

	public int FPS = 6;

	public enum AlternateRule
	{
		LowHealth
	}

	[Serializable]
	public class ConditionalAlternateAnimation
	{
		public AlternateRule Rule; 
		public AnimationAsset Alternate;
	}

	public ConditionalAlternateAnimation[] Alternates;
}
