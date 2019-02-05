using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Gesture : MonoBehaviour
{
    public GameObject responsibleUI;

    public Transform rthumb, rindex, rmiddle, rring, rpinky, rjoint_ti, rjoint_im, rjoint_mr, rjoint_rp;
    public Transform lthumb, lindex, lmiddle, lring, lpinky, ljoint_ti, ljoint_im, ljoint_mr, ljoint_rp;
    public Transform left_hand, right_hand, index;
    private List<Transform> rfingers = new List<Transform>(), lfingers = new List<Transform>(), fingers = new List<Transform>();
    private List<List<float>> degreesOfIsolation = new List<List<float>> { new List<float> { 0, 15f, 0, 0 }, new List<float> { 0, 15.5f, 23f, 0 }, new List<float> { 0, 14.5f, 23.5f, 32f }, new List<float> { 37f, 14.5f, 26f, 33f } };
    private List<Transform> raccuracyJoints = new List<Transform>(), laccuracyJoints = new List<Transform>(), accuracyJoints = new List<Transform>();
    //Thumb-Index, Index-Middle, Middle-Ring, Ring-Pinky
    private const float accuracyConst = 0;
    private int previousNum = 0;


    private int wantedNumber = 1;
    private float startTime;

    private void next()
    {
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(false);
        wantedNumber = Random.Range(1, 6);
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(true);
        startTime = Time.realtimeSinceStartup;
        Debug.Log("Lift up "+ wantedNumber+" fingers");
    }

    void Start()
    {
        Debug.Log("Starting");
        next();
        rfingers.Add(rthumb);
        rfingers.Add(rindex);
        rfingers.Add(rmiddle);
        rfingers.Add(rring);
        rfingers.Add(rpinky);

        lfingers.Add(lthumb);
        lfingers.Add(lindex);
        lfingers.Add(lmiddle);
        lfingers.Add(lring);
        lfingers.Add(lpinky);

        laccuracyJoints.Add(ljoint_ti);
        laccuracyJoints.Add(ljoint_im);
        laccuracyJoints.Add(ljoint_mr);
        laccuracyJoints.Add(ljoint_rp);

        raccuracyJoints.Add(rjoint_ti);
        raccuracyJoints.Add(rjoint_im);
        raccuracyJoints.Add(rjoint_mr);
        raccuracyJoints.Add(rjoint_rp);

    }

    // Update is called once per frame
    int curIter = 0;
    int correctTimes = 0;
    int i = 0;
    float totAccuracy = 0;
    int score = 0;

    void Update()
    {
        if (right_hand.gameObject.activeSelf)
        {
            fingers = rfingers;
            accuracyJoints = raccuracyJoints;
            index = rindex;
        }
        else
        {
            fingers = lfingers;
            accuracyJoints = laccuracyJoints;
            index = lindex;
        }

        int numFings = 0;
        foreach (Transform curFing in fingers)
        {
            Transform nextFing = curFing;
            while (true)
            {
                if (nextFing.childCount == 0)
                {
                    numFings++;
                    break;
                }
                else if (nextFing.position.y >= nextFing.GetChild(0).position.y)
                {
                    break;
                }
                else if (Mathf.Rad2Deg * (Mathf.Atan(Mathf.Abs((nextFing.GetChild(0).position.y - nextFing.position.y) / (nextFing.GetChild(0).position.x - nextFing.position.x)))) < 50)
                {
                    break;
                }
                else
                {
                    nextFing = nextFing.GetChild(0);
                }
            }
        }
        previousNum = numFings;

        float accuracy;
        if (previousNum > 1)
        {
            accuracy = Accuracy();
        }
        else
        {
            accuracy = AccuracyFirst();
        }

        if (wantedNumber != numFings){
            accuracy = Mathf.Abs(accuracy / (wantedNumber - numFings));
        }
       

        totAccuracy += accuracy;

        if (i == 20)
        {
            i = 0;
            accuracy = totAccuracy / 20;
            totAccuracy = 0;
            

            if (accuracy > 40 && wantedNumber == numFings)
            {
                correctTimes++;
                if (correctTimes >= 2)
                {
                    Debug.Log("Good job,  you took " + (Time.realtimeSinceStartup - startTime) + " seconds");
                    score += (int)(accuracy / (Time.realtimeSinceStartup - startTime));
                    next();
                }
            }

            curIter++;
            if (curIter >= 3)
            {
                curIter = 0;
                correctTimes = 0;
            }

            responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString();
            responsibleUI.transform.Find("Accuracy").GetComponent<TextMesh>().text = "Accuracy: " + ((int)(accuracy)).ToString() + "%";
            responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString();
        }

        i++;
        
    }

    float AccuracyFirst()
    {
        float DeviationFromStandard = 0, change_x = 0, change_z = 0;
        Transform startTransform = index.GetChild(0);

        while (true)
        {
            if (startTransform.childCount == 0)
            {
                break;
            }
            else
            {
                change_x += Mathf.Abs(startTransform.position.x - startTransform.GetChild(0).position.x) / startTransform.position.x;
                change_z += Mathf.Abs(startTransform.position.z - startTransform.GetChild(0).position.z) / startTransform.position.z;
                DeviationFromStandard += 100 - change_z * 100 - change_x * 100;
                startTransform = startTransform.GetChild(0);
            }
        }
        float accuracy = (DeviationFromStandard / 3);
        accuracy = Mathf.Max(0, accuracy);
        accuracy = Mathf.Min(100, accuracy);
        return accuracy + 15;
    }
    float Accuracy()
    {
        float DeviationFromStandard = 0;
        for (int i = 1; i < fingers.Count; i++)
        {
            Transform endPoint_1 = FindEndPoint(fingers[i]);
            Transform endPoint_2 = FindEndPoint(fingers[i - 1]);
            float Side_InBetweenEndPoints = Vector3.Distance(endPoint_1.position, endPoint_2.position);
            float Side_F1toJoint = Vector3.Distance(endPoint_1.position, accuracyJoints[i - 1].position);
            float Side_F2toJoint = Vector3.Distance(endPoint_2.position, accuracyJoints[i - 1].position);
            float theta = Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(Side_InBetweenEndPoints, 2) - Mathf.Pow(Side_F1toJoint, 2) - Mathf.Pow(Side_F2toJoint, 2)) / (-2 * Side_F2toJoint * Side_F1toJoint));
            
            if (degreesOfIsolation[previousNum - 2][i - 1] != 0)
            {
                float change = Mathf.Abs(degreesOfIsolation[previousNum - 2][i - 1] - theta) / degreesOfIsolation[previousNum - 2][i - 1];
                DeviationFromStandard += 100 - change * 100;
            }

        }
        float accuracy = (DeviationFromStandard / (previousNum - 1));
        accuracy = Mathf.Max(0, accuracy);
        accuracy = Mathf.Min(100, accuracy);


        return accuracy;
    }
    float dist(Vector3 p1, Vector3 p2)
    {
        return Vector3.Distance(p1, p2);
    }
    Transform FindEndPoint(Transform startFinger)
    {
        while (startFinger.childCount != 0)
        {
            startFinger = startFinger.GetChild(0);
        }
        return startFinger;
    }

    private void OnDestroy()
    {
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text.Substring(6));
        Debug.Log(lastScore);
        Debug.Log(score);
        if (lastScore  < score + 100)
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString();
        }
        
        StreamWriter writer = new StreamWriter("Assets/Finger_Test.txt", true);
        writer.WriteLine(System.DateTime.Now + " - Score: "+ score);
        writer.Close();
        
    }
}
