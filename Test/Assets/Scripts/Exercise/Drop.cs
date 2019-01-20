using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drop : MonoBehaviour
{
    public GameObject responsibleUI;
    public Transform rindex_end, rthumb_end, rmiddle_end, rpinky_end, rring_end, rpalm, palm;
    public Transform lindex_end, lthumb_end, lmiddle_end, lpinky_end, lring_end, lpalm, rhand, lhand;
    private List<Transform> fingers = new List<Transform>(), lfingers = new List<Transform>(), rfingers = new List<Transform>();
    float HandPos1 = 0, HandPos2 = 0;

    // Use this for initialization
    float startTime;
    void Start()
    {
        startTime = Time.realtimeSinceStartup;
        rfingers.Add(rindex_end);
        rfingers.Add(rthumb_end);
        rfingers.Add(rmiddle_end);
        rfingers.Add(rring_end);
        rfingers.Add(rpinky_end);

        lfingers.Add(lindex_end);
        lfingers.Add(lthumb_end);
        lfingers.Add(lmiddle_end);
        lfingers.Add(lring_end);
        lfingers.Add(lpinky_end);
    }

    // Update is called once per frame


    // Update is called once per frame
    int i = 0;
    float totAccuracy = 0;
    int score = 0;

    float high_score = 150;
    float low_score = 150;

    void Update()
    {
        i++;
        if (i >= 1000)
        {
            i = 0;
            high_score = 150;
            low_score = 150;
        }

        if (rhand.gameObject.activeSelf)
        {
            palm = rpalm;
            fingers = rfingers;
        }
        else if (lhand.gameObject.activeSelf)
        {
            palm = lpalm;
            fingers = lfingers;
        }

        //last_score = cur_score;
        float cur_score = 100 * (GetHandPos()) / 0.31f;
        if (cur_score < low_score)
        {
            low_score = cur_score;
            score += Mathf.Abs((int)((high_score - low_score) / 10));
        }else if (cur_score > high_score)
        {
            high_score = cur_score;
            score += Mathf.Abs((int)((high_score - low_score) / 10));
        }
        responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString();
        responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString();

        //Debug.Log(Vector3.Distance(palm.position, pinky_end.position));

        // Debug.Log(GetHandPos());
        
    }
    float GetHandPos()
    {
        float sumVecs = 0;
        foreach (Transform fing in fingers)
        {
            sumVecs += Vector3.Distance(fing.position, palm.position);
        }
        return sumVecs;

    }


}