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
    public Transform exerciseParent;
    //public Camera mainCam;

    private KeywordRecognizer keywordRecognizer;

    [SerializeField]
    private Text m_Hypotheses;

    [SerializeField]
    private Text m_Recognitions;

    private DictationRecognizer recognition;

    private string menu = "Main Menu";
    private string oldMenu;
    private bool inExercise = false;

    private Dictionary<string, List<string>> menuCommands = new Dictionary<string, List<string>> {
        { "Main Menu",  new List<string> { "start", "goal" } },
        { "Exercise Selection",  new List<string> { "finger", "balance", "stretch" } }
    };

    private Dictionary<string, string> commandActions = new Dictionary<string, string> {
        { "start", "Exercise Selection" },
        {"goal", "Goal Menu"},
        { "finger", "Exercise: Fingers"},
        { "balance", "Exercise: Balance"},
        { "stretch", "Exercise: Drop"}
    };
    

    void Start()
    {
        // testing hit
        Debug.Log("Starting Voice Recognition");

        recognition = new DictationRecognizer();
        GameObject exerciseScript;

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
                if (text.Contains("return"))
                {
                    if (inExercise) // remove exercise code
                    {
                        Debug.Log("Remove stuff");
                        try
                        {
                            Destroy(uiParent.transform.Find(menu).Find("balance(Clone)").gameObject);
                        }
                        catch
                        {}
                        try
                        {
                            Destroy(uiParent.transform.Find(menu).Find("stretch(Clone)").gameObject);
                        }
                        catch
                        {}
                        try
                        {
                            Destroy(uiParent.transform.Find(menu).Find("finger(Clone)").gameObject);
                        }
                        catch
                        { }
                        inExercise = false;
                    }

                    uiParent.Find(menu).gameObject.SetActive(false);


                    menu = oldMenu;
                    oldMenu = "Main Menu";

                    Transform newMen = uiParent.Find(menu);
                    newMen.gameObject.SetActive(true);

                    
                }

                try
                {
                    foreach (string str in menuCommands[menu])
                    {
                        try
                        {
                            if (text.Contains(str))
                            {
                                Debug.Log(str);
                                uiParent.Find(menu).gameObject.SetActive(false);


                                oldMenu = menu;
                                menu = commandActions[str];

                                Transform newMen = uiParent.Find(menu);
                                newMen.gameObject.SetActive(true);

                                if (oldMenu.Equals("Exercise Selection")) // run exercise
                                {

                                    inExercise = true;
                                    GameObject newScript = Instantiate(exerciseParent.Find(str).gameObject, newMen);
                                    newScript.SetActive(true);

                                    exerciseScript = newScript;
                                }

                                break;
                            }
                        }
                        catch
                        {

                        }

                    }
                }
                catch
                {

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


    public void select(GameObject button, Collider hit) // called by other objects
    {
        Debug.Log(hit.name);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
