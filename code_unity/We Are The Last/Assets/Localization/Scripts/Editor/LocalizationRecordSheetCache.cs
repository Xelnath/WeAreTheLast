using GoogleSheetsForUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationRecordSheetCache", menuName = "Google Sheets For Unity/Localization Sheet Cache")]
public class LocalizationRecordSheetCache : ScriptableObject
{
    public ConnectionData connectionData;

    public string[] Sheets;
}

