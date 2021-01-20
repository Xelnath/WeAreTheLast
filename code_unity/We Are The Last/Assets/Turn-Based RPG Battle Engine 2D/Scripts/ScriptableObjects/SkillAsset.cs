using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using UnityEditor;

//This script stores object references for easier access
public class SkillAsset : ScriptableObject
{
	public skill Skill;
	
	[Sirenix.OdinInspector.ShowInInspector]
	public void ImportSkillFunctions()
	{
		if (Skill.functionCalls != null)
		{
			Skill.functionsToCall = SkillTextParser.parseFunctionsToCall(Skill.functionCalls);
			Skill.endOfRound = SkillTextParser.parseEndOfRound(Skill.functionCalls);
			Skill.sacrificeActions = SkillTextParser.parseSacrifice(Skill.functionCalls);
			EditorUtility.SetDirty(this);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
	[Sirenix.OdinInspector.ShowInInspector]
	public void ExportSkillFunctions()
	{
		Skill.functionCalls = SkillTextExporter.Export(this);
		if (Skill.functionCalls == null)
		{
			Debug.Log("Whoopsies");
		}
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}
