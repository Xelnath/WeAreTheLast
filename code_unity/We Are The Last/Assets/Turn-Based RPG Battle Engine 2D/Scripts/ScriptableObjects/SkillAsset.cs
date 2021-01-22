using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using UnityEditor;
using UnityEngine.Serialization;

//This script stores object references for easier access
public class SkillAsset : ScriptableObject
{
	public skill Skill;

	[FormerlySerializedAs( "functionCalls" )] public TextAsset SkillText;

	[Sirenix.OdinInspector.ShowInInspector]
	public void ImportSkillFunctions()
	{
		if (SkillText != null)
		{
			Skill.functionsToCall = SkillTextParser.parseFunctionsToCall(SkillText);
			Skill.endOfRound = SkillTextParser.parseEndOfRound(SkillText);
			Skill.sacrificeActions = SkillTextParser.parseSacrifice(SkillText);
			EditorUtility.SetDirty(this);
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
	[Sirenix.OdinInspector.ShowInInspector]
	public void ExportSkillFunctions()
	{
		SkillText = SkillTextExporter.Export(this);
		if (SkillText == null)
		{
			Debug.Log("Whoopsies");
		}
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}
