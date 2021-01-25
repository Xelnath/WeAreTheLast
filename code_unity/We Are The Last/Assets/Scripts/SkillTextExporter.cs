using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using ClassDB;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public class SkillTextExporter : MonoBehaviour
{
    public static TextAsset Export(SkillAsset skillAsset)
    {
        skill skill = skillAsset.Skill;
        string skillName = skillAsset.name;
        string path = $"Assets/Resources/ScriptableObjects/Skills/SkillTextAssets/{skillName}.txt";
        
        //Create File or clear it
        if  ( !File.Exists( path ) )
        {
            using ( File.Create( path ) ) ;
        }

        File.WriteAllText(path, String.Empty);
        for ( int i = 0; i < skill.targetProviders.Count; ++i )
        {
            File.AppendAllText(path, $"TARGET{i}");
            AppendFunctions( path, skill.targetProviders[i].targetCalls );
        }

        //Content of the file
        string header = "FUNCTIONS\n\n";
        File.AppendAllText(path, header);
        AppendFunctions( path, skill.functionsToCall );

        header = "\n\nENDOFROUND\n\n";
        File.AppendAllText(path, header);
        AppendFunctions( path, skill.endOfRound );

        header = "\n\nSACRIFICE\n\n";
        File.AppendAllText(path, header);
        AppendFunctions( path, skill.sacrificeActions );
        
        AssetDatabase.Refresh();
        
        string resourcesPath = path.Substring(17, path.Length - 21);
        TextAsset textAsset = Resources.Load<TextAsset>(resourcesPath);
        if (textAsset == null)
        {
            Debug.Log("Not found");
        }

        return textAsset;
    }

    private static void AppendFunctions(string path, List<callInfo> calls)
    { 
        foreach (callInfo function in calls )
        {
            string name = function.functionName;
            string wait = "";
            if (function.waitForPreviousFunction) wait = "W";
            string coroutine = "";
            if (function.isCoroutine) coroutine = "C";
            string running = "";
            if (function.isRunning) running = "R";
            string parameters = "";
            foreach (var parameter in function.parametersArray)
            {
                parameters += $"{parameter},";
            }
            if (!parameters.IsNullOrWhitespace()) parameters = parameters.Substring(0, parameters.Length - 1);

            string line = $"{name},{wait},{coroutine},{running},{parameters}\n";
            File.AppendAllText(path, line);
        }
    }
}
