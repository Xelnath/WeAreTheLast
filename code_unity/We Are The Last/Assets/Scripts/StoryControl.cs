using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;

public class StoryControl : MonoBehaviour
{

    [Header("GENERAL")]
    public TextAsset inkAsset;
    public Story _inkStory;
    public Bubble bubblePrefab;
    public List<Button> buttons;
    public Button btnPrefab;
    public Transform btnHolder;
    
    public Transform CanvasMain;
    
    public List<Character> Actors;
    public Transform PCParent;
    public Character CurrTalker;

    // Start is called before the first frame update
    void Start()
    {
        
        _inkStory = new Story(inkAsset.text);
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Refresh();
        }
    }

    public void Bark(string Knot, string CharacterID)
    {
        //Debug.Log(Knot);
        _inkStory.variablesState["CurrChar"] = CharacterID;
        _inkStory.ChoosePathString(Knot);
        Refresh();
    }

    public void CheckTags()
    {
        List<string> tags = _inkStory.currentTags;
        if (tags.Count > 0)
        {
            foreach (string t in tags)
            {
                Debug.Log(t + CurrTalker.id);
                TagProcess(t);
            }
        }
        else
        {
        }
        tags.Clear();
    }

    public void TagProcess(string t)
    {
        switch(t)
        {
            case "Wide":
                break;
            case "CU":
                break;
        }
    }

    public void Refresh()
    {
        foreach (Button b in buttons)
        {
            Destroy(b.gameObject);
        }
        buttons.Clear();
        if (_inkStory.canContinue)
        {
            StartCoroutine("Refresher", 1f);
        }
    }

    IEnumerator Refresher(float Delay)
    {
        string[] temp = _inkStory.Continue().Split(';');
        if ( temp.Length > 0 )
        {
            string[] stats = temp[0].Split( ',' );
            // foreach (string s in stats)
            // {
            //     Debug.Log("stat" + s);
            // }
            if (temp.Length > 1)
            { 
            temp[1] = temp[1].Replace("\r\n", "");
            temp[1] = temp[1].Trim();
            //Debug.Log(temp[1]);
            }
            if ( stats.Length > 2 && !string.IsNullOrEmpty(temp[1]))
            {
                yield return new WaitForSeconds( float.Parse( stats[1] ) );
                Bubble b = Instantiate( bubblePrefab );
                BubbleMaker( b, temp[1], stats[0] );

                yield return new WaitForSeconds( float.Parse( stats[2] ) );
                Destroy( b.gameObject );
                if ( _inkStory.canContinue )
                {
                    StartCoroutine( "Refresher", 1f );
                }
            }
        }
    }

    public void BubbleMaker(Bubble b, string text, string actor)
    {
        b.txt.text = text;
        
        foreach (Character c in Actors)
        {
            if ( c == null )
            {
                //Debug.Log( $"Destroyed {c.name}." );
                continue;
            }

            if (actor == c.id)
            {
                CurrTalker = c;
                CheckTags();
                b.transform.position = c.transform.position;
                //Debug.Log(actor);
            }
        }
        
    }

    IEnumerator ButtonMaker()
    {
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

            if (_inkStory.currentChoices.Count > 0)
            {
                for (int i = 0; i < _inkStory.currentChoices.Count; i++)
                {
                    Choice choice = _inkStory.currentChoices[i];
                    Button button = CreateChoiceView(choice.text.Trim(), i);
                    button.GetComponentInChildren<Text>().text = choice.text;
                    // Tell the button what to do when we press it
                    button.onClick.AddListener(delegate
                    {
                        OnClickChoiceButton(choice);
                    });
                    buttons.Add(button);
                }
            }
        
    }


    Button CreateChoiceView(string text, int num)
    {
        // Creates the button from a prefab
        Button choice = Instantiate(btnPrefab, btnHolder) as Button;

        // Gets the text from the button prefab
        Text choiceText = choice.GetComponentInChildren<Text>();
        choiceText.text = text.TrimEnd();

        // Make the button expand to fit the text
        VerticalLayoutGroup layoutGroup = choice.GetComponent<VerticalLayoutGroup>();

        return choice;
    }
    // When we click the choice button, tell the story to choose that choice!
    void OnClickChoiceButton(Choice choice)
    {
        _inkStory.ChooseChoiceIndex(choice.index);
        Refresh();
    }

    

}
