using System.Collections;
using System.Collections.Generic;
using ClassDB;
using Sirenix.Utilities;
using UnityEngine;

public class SkillTextParser : MonoBehaviour
{
    public static List<callInfo> parseFunctionsToCall(TextAsset skillText)
    {
        return parseText(skillText, "FUNCTIONS");
    }
    
    
    public static List<callInfo> parseEndOfRound(TextAsset skillText)
    {
        return parseText(skillText, "ENDOFROUND");
    }
    
    public static List<callInfo> parseSacrifice(TextAsset skillText)
    {
        return parseText(skillText, "SACRIFICE");
    }

    public static List<callInfo> parseTarget(TextAsset skillText, int i)
    {
        return parseText(skillText, "TARGET"+i);
    }

    // Terrible, inefficient... don't care
    public static int parseTargetCount( TextAsset skillText )
    {
        int i = 0;
        while ( skillText.text.Contains( "TARGET" + i ) ) i++;

        return i;
    }

    static List<callInfo> parseText(TextAsset skillText, string startingLine)
    {
        List<callInfo> functionCalls = new List<callInfo>();

        var skillTextLines = skillText.text.Split('\n');
        bool parsing = false;
        foreach (var line in skillTextLines)
        {
            if((line.Contains("SACRIFICE") || line.Contains("ENDOFROUND") || line.Contains("FUNCTIONS") || line.Contains("TARGET") ) && parsing)
            {
                break;
            }
            if (line.Contains(startingLine))
            {
                parsing = true;
                continue;
            }

            if (parsing)
            {
                var splitLine = line.Split(',');
                if (splitLine[0].IsNullOrWhitespace())
                {
                    continue;
                }
                //functionName
                callInfo lineInfo = new callInfo()
                {
                    functionName = splitLine[0],
                };
                //waitForPrevious
                if (splitLine.Length > 1)
                {
                    lineInfo.waitForPreviousFunction = !splitLine[1].IsNullOrWhitespace();
                }
                //isCoRoutine
                if (splitLine.Length > 2)
                {
                    lineInfo.isCoroutine = !splitLine[2].IsNullOrWhitespace();
                }
                //isRunning
                if (splitLine.Length > 3)
                {
                    lineInfo.isRunning = !splitLine[3].IsNullOrWhitespace();
                }
                
                //functionParameters
                if (splitLine.Length > 4)
                {
                    List<string> functionParameters = new List<string>();
                    
                    for (int i = 4; i < splitLine.Length; i++)
                    {
                        if(!splitLine[i].Trim().IsNullOrWhitespace())
                            functionParameters.Add(splitLine[i].Trim());
                    }

                    lineInfo.parametersArray = functionParameters;
                } 

                functionCalls.Add(lineInfo);
            }
        }
        return functionCalls;
    }
}
