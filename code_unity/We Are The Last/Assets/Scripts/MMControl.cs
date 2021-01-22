using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MMControl : MonoBehaviour
{
    public TextMeshProUGUI AttemptsLabel;
    public string LevelScene;
    public string MenuScene;

    [Serializable]
    public class FailureLabel
    {
        public int MinFailures = 0;
        public string Label = "Failures: {0}";
    }

    public FailureLabel[] Failures = new FailureLabel[0];
    
    // Start is called before the first frame update
    void Start()
    {
        int deathCount = PlayerPrefs.GetInt( "FAILURES", 0 );

        for ( int i = Failures.Length - 1; i >= 0; i-- )
        {
            if ( Failures[i].MinFailures < deathCount )
            {
                AttemptsLabel.text = string.Format( Failures[i].Label, deathCount );
                break;
            }
        }
    }

    public void StartScene()
    {
        SceneManager.LoadScene(LevelScene);
    }

    public void ResetEscapes()
    {
        PlayerPrefs.SetInt( "FAILURES", 0 );
        PlayerPrefs.Save();
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(MenuScene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
