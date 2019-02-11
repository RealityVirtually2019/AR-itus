//Importing Libraries
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using Unity;
using UnityEngine;


public class SoundListen : MonoBehaviour // our sound listen class
{
    // this script will listen to the voice commands and activate UIs accordingly

    // public variables that are accessing other objects
    public Transform uiParent;
    public Transform exerciseParent;

    // the recognizer buffers we put sound into 
    private KeywordRecognizer keywordRecognizer;
    private DictationRecognizer recognition;

    // private fields that we need serialization 
    [SerializeField]
    private Text m_Hypotheses;

    [SerializeField]
    private Text m_Recognitions;

    // private old
    private string menu = "Main Menu";
    private string oldMenu;
    private bool inExercise = false;

    // Dictionary of all the keywords that will be detected sorted by menu
    private Dictionary<string, List<string>> menuCommands = new Dictionary<string, List<string>> {
        { "Main Menu",  new List<string> { "start", "goal" } },
        { "Exercise Selection",  new List<string> { "finger", "balance", "stretch" } }
    };

    // Directory of keywords to corresponding menu
    private Dictionary<string, string> commandActions = new Dictionary<string, string> {
        { "start", "Exercise Selection" },
        {"goal", "Goal Menu"},
        { "finger", "Exercise: Fingers"},
        { "balance", "Exercise: Balance"},
        { "stretch", "Exercise: Stretch"}
    };


    void Start()
    {
        Debug.Log("Starting Voice Recognition");

        // Initilize the recognizer 
        recognition = new DictationRecognizer();
        GameObject exerciseScript;

        recognition.DictationResult += (text, confidence) => // once there is a result from the voice listener 
        {
            try // must be wrapped in a try catch in case of errors in speech recognition 
            {
                // log the results and start the next sentence
                Debug.LogFormat("Dictation result: {0}", text);
                m_Recognitions.text += text + "\n";

            }
            catch
            {
                // speech recognition is breaking
                Debug.Log("Problems, problems, problems");

            }

        };

        recognition.DictationHypothesis += (text) =>
        {
            // Before the program decides what was said, these are possible results. To get immediate response, we detect keywords here.

            if (text.Contains("return")) // they want to return to previous menu
            {
                if (inExercise) // remove exercise code
                {
                    Destroy(uiParent.transform.Find(menu).Find("balance(Clone)").gameObject);
                    Destroy(uiParent.transform.Find(menu).Find("stretch(Clone)").gameObject);
                    Destroy(uiParent.transform.Find(menu).Find("finger(Clone)").gameObject);
                    inExercise = false;
                }

                uiParent.Find(menu).gameObject.SetActive(false); // set current menu to no longer visible

                // switch menu variables
                menu = oldMenu;
                oldMenu = "Main Menu";

                // find new menu and make it visible 
                Transform newMen = uiParent.Find(menu);
                newMen.gameObject.SetActive(true);
            }

            foreach (string str in menuCommands[menu]) // check if they have other keywords
            {
                try // wrap it in try and catch in case speech recognition errors on us
                {
                    if (text.Contains(str)) // if they said the key word
                    {
                        uiParent.Find(menu).gameObject.SetActive(false); // set current menu to invisible

                        // switch menu variables
                        oldMenu = menu;
                        menu = commandActions[str];

                        // find new menu and make it visible
                        Transform newMen = uiParent.Find(menu);
                        newMen.gameObject.SetActive(true);

                        if (oldMenu.Equals("Exercise Selection")) // run exercise
                        {
                            inExercise = true; // set the bool to true, we are now in an exercise 
                            GameObject newScript = Instantiate(exerciseParent.Find(str).gameObject, newMen); // create new script for the exercise 
                            newScript.SetActive(true); // make the script run

                            exerciseScript = newScript;
                        }

                        break; // we found a command, don't do two at once. 
                    }
                }
                catch
                {
                    // speech recognition went wrong
                }
            }
        };
         
        // required dictation events 
        recognition.DictationComplete += (completionCause) =>{};
        recognition.DictationError += (error, hresult) =>{};
				
		// start the speech recognition 
        recognition.Start();
    }
}