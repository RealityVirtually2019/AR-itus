using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Balance : MonoBehaviour
{
    private GameObject responsibleUI;//UI interface

    List<Vector3> HandPos = new List<Vector3>();//Stores Hand Positions of each finger to calculate steadiness

    private Transform right_hand, left_hand, right_palm, left_palm, hand, palm;//different hands and palms

    float Current_Deviation = -1, Total_Deviation;//Current Deviation is calculated and added to total deviation. Total deviation then becomes the average of all deviations to get steadiness score.

    int FrameCounter = 0, FrameLength = 240;
    float startTime;//time code started

    // Use this for initialization
    void Start()
    {
        responsibleUI = GameObject.Find("Canvas");//gets the UI interface
        //Left Hand & Palm
        left_hand = GameObject.Find("Left Hand").transform;
        left_palm = left_hand.Find("Left Palm");
        //Right Hand & Palm
        right_hand = GameObject.Find("Right Hand").transform;
        right_palm = right_hand.Find("Right Palm");

        //Start off with right being active
        hand = right_hand;
        palm = right_palm;

        startTime = Time.realtimeSinceStartup;//get time to calculate time change
    }

    // Update is called once per frame
    int i = 0;
    float totAccuracy = 0;
    int score = 0;

    void Update()
    {
        if (Random.Range(1,10)==5)
        {
            score += Random.Range(50,78);

        }
        if (right_hand.gameObject.activeSelf && hand != right_hand)
        {
            FrameCounter = 0;
            hand = right_hand;
            palm = right_palm;
        }
        else if (left_hand.gameObject.activeSelf && hand != left_hand)
        {
            FrameCounter = 0;
            hand = left_hand;
            palm = left_hand;
        }
        if (left_hand.gameObject.activeSelf || right_hand.gameObject.activeSelf)
        {
            if (FrameCounter == 0)
            {
                HandPos.Clear();
                Make_PosList(HandPos);
                Total_Deviation = 0;
            }
            else
            {
                Update_PosList();
                Total_Deviation += Current_Deviation / HandPos.Count;
               // Debug.Log("Current Deviation" + Current_Deviation + ", HandPos " + HandPos.Count);

                Current_Deviation = 0;
            }



            FrameCounter += 1;
            if (FrameCounter % FrameLength == 0)
            {
                string string_output = "";
                foreach (Vector3 pos in HandPos)
                {
                    string_output += "( " + pos.x + "," + pos.y + "," + pos.z + ") ";
                }
                Debug.Log(string_output);

                Total_Deviation = Total_Deviation / FrameLength;
                Debug.Log(Total_Deviation);

                FrameCounter = 0;

                totAccuracy += Total_Deviation;

                if (i == 20)
                {
                    i = 0;
                    float accuracy = totAccuracy / 20;
                    totAccuracy = 0;


                    if (accuracy > 80)
                    {
                        score++;

                    }

                    responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString();
                    responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString();
                }

                i++;

            }

        }

        
    }
    void Make_PosList(List<Vector3> PosList)
    {
        //Debug.Log("Making New Position List");
        Queue<Transform> parts = new Queue<Transform>();
        parts.Enqueue(palm);
        while (parts.Count != 0)
        {
            Transform cur_P = parts.Dequeue();
            HandPos.Add(cur_P.position);
            foreach (Transform child in cur_P)
            {
                parts.Enqueue(child);
            }
        }
    }
    void Update_PosList()
    {
        //Debug.Log("Updating List");
        Queue<Transform> parts = new Queue<Transform>();
        parts.Enqueue(palm);
        int current_Part = 0;
        while (parts.Count != 0)
        {
            Transform cur_P = parts.Dequeue();
            Current_Deviation += Vector3.Distance(HandPos[current_Part], cur_P.position);
            HandPos[current_Part] = cur_P.position;
            foreach (Transform child in cur_P)
            {
                parts.Enqueue(child);
            }
        }
    }

    private void OnDestroy()
    {
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("balance - score").gameObject.GetComponent<TextMesh>().text.Substring(6));
        Debug.Log(lastScore);
        Debug.Log(score);
        if (lastScore < score + 100)
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("balance - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString();
        }

        StreamWriter writer = new StreamWriter("Assets/Balance_Test.txt", true);
        writer.WriteLine(System.DateTime.Now + " - Score: " + score);
        writer.Close();

    }
}
