using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
#endif
public class DelayedLog
{
    static DelayedLog()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

    private static List<Action> m_delayedDebugLogs = new List<Action>();

    public static void RegisterDelayedDebugLog(Action action)
    {
        m_delayedDebugLogs.Add(action);
    }

    private static void EditorUpdate()
    {
        Profiler.BeginSample("DelayedLog.EditorUpdate");

        for (int i = 0; i < m_delayedDebugLogs.Count; i++)
        {
            m_delayedDebugLogs[i]();
        }
        m_delayedDebugLogs.Clear();

        Profiler.EndSample();
    }
}