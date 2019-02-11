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
    float score;
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

    float totAccuracy = 0;//total accuracy of the steadiness of the hand

    void Update()
    {
        if (right_hand.gameObject.activeSelf && hand != right_hand)//if right hand becomes active set right hand variables.
        {
            FrameCounter = 0;
            hand = right_hand;
            palm = right_palm;
        }
        else if (left_hand.gameObject.activeSelf && hand != left_hand)//if left hand becomes active set left hand variables
        {
            FrameCounter = 0;
            hand = left_hand;
            palm = left_hand;
        }
        if (left_hand.gameObject.activeSelf || right_hand.gameObject.activeSelf)//if either hand is active and moving we calculate the score
        {
            if (FrameCounter == 0)//if the counter is 0, we clear the HandPos, and we make the position list from HandPos
            {
                HandPos.Clear();//Clear previous
                Make_PosList(HandPos);//make new based off current position
                Total_Deviation = 0;//Total Deviation is set to 0 again
            }
            else
            {
                Update_PosList();//Update Position list to new values
                Total_Deviation += Current_Deviation / HandPos.Count; //add average of all deviations to total deviation
                Current_Deviation = 0;//current deviation gets set to 0 to reset
            }

            FrameCounter += 1;//add to frame counter each frame
            if (FrameCounter % FrameLength == 0){//if its time to reset 

                Total_Deviation = Total_Deviation / FrameLength;//set total deviation to be averages of all total deviations
                score = Total_Deviation;//set score
                //Display Time and Score
                responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString();
                responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString();
                Total_Deviation = 0;//reset total deviation
            }

        }

        
    }
    void Make_PosList(List<Vector3> PosList)//Makes HandPos List from scratch
    {
        Queue<Transform> parts = new Queue<Transform>();//parts
        parts.Enqueue(palm);//add the palm as start point
        while (parts.Count != 0)//For each child we add parts to the HandPos List for the position
        {
            Transform cur_P = parts.Dequeue();//Pop last one
            HandPos.Add(cur_P.position);//add posiition of current part
            foreach (Transform child in cur_P)//for each child in current position add it to parts
            {
                parts.Enqueue(child);
            }
        }
    }
    void Update_PosList()//Updates HandPos List and calculates deviation that is used for score
    {
        Queue<Transform> parts = new Queue<Transform>();//add a queue
        parts.Enqueue(palm);//add start point as palm
        int current_Part = 0;//we start at the first item in HandPos
        while (parts.Count != 0)
        {
            Transform cur_P = parts.Dequeue();//pop last item in parts
            Current_Deviation += Vector3.Distance(HandPos[current_Part], cur_P.position);//add to deviation teh distance from last position in list to its current position (steadiness)
            HandPos[current_Part] = cur_P.position;//update position
            current_Part++;//go to next part
            foreach (Transform child in cur_P)
            {
                parts.Enqueue(child);//queue up the next parts from the children of the current part
            }
        }
    }

    private void OnDestroy()
    {
        // when this is destroyed, they returned to the other menu. Check if this score is higher than their high score and set a goal accordingly
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("balance - score").gameObject.GetComponent<TextMesh>().text.Substring(6));// the old goal
        if (lastScore < score + 100)// checking if our score is a high score
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("balance - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString();// change the goal text to be a 100 above high score
        }
        //When user exits the game write to the text file that stores user scores along with the current date and time
        StreamWriter writer = new StreamWriter("Assets/Balance_Test.txt", true);
        writer.WriteLine(System.DateTime.Now + " - Score: " + score);
        writer.Close();

    }
}
