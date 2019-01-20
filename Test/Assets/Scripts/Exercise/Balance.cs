using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balance : MonoBehaviour
{
    public GameObject responsibleUI;

    List<Vector3> HandPos = new List<Vector3>();
    List<Vector3> startHandPos = new List<Vector3>();

    public Transform right_hand, left_hand, right_palm, left_palm, hand, palm;

    float Current_Deviation = -1, Total_Deviation;

    int FrameCounter = 0, FrameLength = 240;
    float startTime;

    // Use this for initialization
    void Start()
    {
        startTime = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    int i = 0;
    float totAccuracy = 0;
    int score = 0;

    void Update()
    {
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
}
