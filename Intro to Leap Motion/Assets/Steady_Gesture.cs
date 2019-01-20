using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steady_Gesture : MonoBehaviour {
    List<Vector3> HandPos = new List<Vector3>();
    List<Vector3> startHandPos = new List<Vector3>();

    public Transform right_hand, left_hand, right_palm, left_palm, hand, palm;

    float Current_Deviation = -1, Total_Deviation;

    int FrameCounter = 0, FrameLength = 240;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (right_hand.gameObject.activeSelf && hand != right_hand)
        { 
            Debug.Log("Change to Right Hand");
            FrameCounter = 0;
            hand = right_hand;
            palm = right_palm;
        }
        else if (left_hand.gameObject.activeSelf && hand != left_hand)
        {
            Debug.Log("Change to Left Hand");
            FrameCounter = 0;
            hand = left_hand;
            palm = left_hand;
        }
        if (left_hand.gameObject.activeSelf || right_hand.gameObject.activeSelf)
        {
            if (FrameCounter == 0)
            {
                Debug.Log("New Set");
                HandPos.Clear();
                Make_PosList(HandPos);
                Total_Deviation = 0;
            }
            else
            {
                Debug.Log("Old Set");
                Update_PosList();
                Total_Deviation += Current_Deviation / HandPos.Count;
                Debug.Log("Current Deviation" + Current_Deviation + ", HandPos " + HandPos.Count);
                
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
                
            }
            
        }
	}
    void Make_PosList(List<Vector3> PosList)
    {
        Debug.Log("Making New Position List");
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
        Debug.Log("Updating List");
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
