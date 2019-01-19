using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class SoundListener : MonoBehaviour
{
    private KeywordRecognizer keywordRecognizer;
   
    [SerializeField]
    private Text m_Hypotheses;

    [SerializeField]
    private Text m_Recognitions;

    private DictationRecognizer recognition;

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
                foreach (string str in commands)
                {
                    Debug.Log("T1");
                    if (text.Contains(str))
                    {
                        Debug.Log(str);
                        // reset the hypotheses and forget them
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
