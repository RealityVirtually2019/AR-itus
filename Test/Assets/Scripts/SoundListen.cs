using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Unity;
using UnityEngine;


public class SoundListen : MonoBehaviour
{
    public Transform uiParent;
    public GameObject scriptHolder;

    private KeywordRecognizer keywordRecognizer;

    [SerializeField]
    private Text m_Hypotheses;

    [SerializeField]
    private Text m_Recognitions;

    private DictationRecognizer recognition;

    private string menu = "Main Menu";
    private string oldMenu;

    private Dictionary<string, List<string>> menuCommands = new Dictionary<string, List<string>> {
        { "Main Menu",  new List<string> { "start", "goal" } },
        { "Excersice Selection",  new List<string> { "gesture", "balance", "drop" } },
        { "Goal Menu",  new List<string> {} },
        { "Excersize: Fingers", new List<string> { "return" } }
    };

    private Dictionary<string, string> commandActions = new Dictionary<string, string> {
        { "start", "Excersice Selection" },
        {"goal", "Goal Menu"},
        { "gesture", "Excersize: Fingers"},
        { "balance", "Excersize: Balance"},
        { "drop", "Excersize: Drop"}
    };

    public Dictionary<string, Component> scriptDirector = new Dictionary<string, Component> {
        { "gesture", new Gesture() }, { "drop", new Drop() }, {"balance", new Balance() }
    };

    void Start()
    {
        recognition = new DictationRecognizer();
        List<string> commands = new List<string> { "start", "goal" };

        recognition.DictationResult += (text, confidence) =>
        {
            try
            {
                Debug.LogFormat("Dictation result: {0}", text);
                m_Recognitions.text += text + "\n";

            }
            catch
            {
                Debug.Log("Problems, problems, problems");

            }

        };
        recognition.DictationHypothesis += (text) =>
        {
            //Debug.LogFormat("Dictation hypothesis: {0}", text);
            try
            {
                foreach (string str in menuCommands[menu])
                {
                    if (text.Contains(str))
                    {
                        Debug.Log(str);
                        uiParent.Find(menu).gameObject.SetActive(false);

                        if (str.Equals("return"))
                        {
                            menu = commandActions[oldMenu];
                        }
                        else
                        {
                            oldMenu = menu;
                            menu = commandActions[str];
                        }

                        
                        uiParent.Find(menu).gameObject.SetActive(true);

                        if (oldMenu.Equals("start")) // run excersize
                        {
                            uiParent.gameObject.AddComponent(scriptDirector[str].GetType());
                        } 
                        
                        break;
                    }
                }

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }

        };

        recognition.DictationComplete += (completionCause) =>
        {
            // if (completionCause != DictationCompletionCause.Complete)
            //Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
        };

        recognition.DictationError += (error, hresult) =>
        {
            //Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
        };

        recognition.Start();
    }


    // Update is called once per frame
    void Update()
    {

    }
}
