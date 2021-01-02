using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;

namespace GoogleSheetsForUnity
{
    public static class AwaitExtensions
    {
        public static TaskAwaiter GetAwaiter(this TimeSpan timeSpan)
        {
            return Task.Delay(timeSpan).GetAwaiter();
        }
    }


    [CreateAssetMenu(fileName = "DefaultDriveConnection", menuName = "Google Sheets For Unity/Drive Connection Asset", order = 0)]
    public class DriveConnection : ScriptableObject
    {
        public ConnectionData connectionData;

        private static DriveConnection m_instance;
        public static DriveConnection Instance
        {
            get {
                if (m_instance == null)
                {
                    m_instance = AssetDatabaseHelper.FindAssetsByType<DriveConnection>()[0];
                }
                return m_instance;
            }
        }

        public static async void ExecuteRequest(UnityWebRequest www, Dictionary<string, string> postData)
        {

            await CoExecuteRequest(www, postData);
        }

        private static async Task CoExecuteRequest(UnityWebRequest www, Dictionary<string, string> postData)
        {
            www.SendWebRequest();

            float elapsedTime = 0.0f;

            while (!www.isDone)
            {
                await TimeSpan.FromSeconds(0.1f);
                elapsedTime += 0.1f;
                if (elapsedTime >= DriveConnection.Instance.connectionData.timeOutLimit)
                {
                    Drive.HandleError("Operation timed out, connection aborted. Check your internet connection and try again.", elapsedTime);
                    break;
                }
            }

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Drive.HandleError("Connection error after " + elapsedTime.ToString() + " seconds: " + www.error, elapsedTime);
                return;
            }

            Drive.ProcessResponse(www.downloadHandler.text, elapsedTime);
        }

        //public void ExecuteRequest(UnityWebRequest www, Dictionary<string, string> postData)
        //{
        //    StartCoroutine(CoExecuteRequest(www, postData));
        //}

        //private IEnumerator CoExecuteRequest(UnityWebRequest www, Dictionary<string, string> postData)
        //{
        //    www.SendWebRequest();

        //    float elapsedTime = 0.0f;

        //    while (!www.isDone)
        //    {
        //        elapsedTime += Time.deltaTime;
        //        if (elapsedTime >= connectionData.timeOutLimit)
        //        {
        //            Drive.HandleError("Operation timed out, connection aborted. Check your internet connection and try again.", elapsedTime);
        //            yield break;
        //        }

        //        yield return null;
        //    }

        //    if (www.isNetworkError)
        //    {
        //        Drive.HandleError("Connection error after " + elapsedTime.ToString() + " seconds: " + www.error, elapsedTime);
        //        yield break;
        //    }

        //    Drive.ProcessResponse(www.downloadHandler.text, elapsedTime);
        //}

    }
}
