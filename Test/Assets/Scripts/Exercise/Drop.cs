using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Drop : MonoBehaviour
{
    private GameObject responsibleUI;//Changes UI based on movement of fingers, time, etc.

    //Variables to track which hand is moving
    private Transform rpalm, palm;
    private Transform lpalm, rhand, lhand;

    private List<Transform> fingers = new List<Transform>(), lfingers = new List<Transform>(), rfingers = new List<Transform>(); // Lists that hold the fingers on each hand. Fingers gets set to lfingers or rfingers depending on which hand is active. The finger parts are then checked to see if user has dropped the object.

    float startTime;//get start time to get total time for display

    int frame_counter = 0;//starts with frame_counter at -
    int score = 0;//current score is 0

    float high_score = 150;// highest stretch score - largest stretch hand
    float low_score = 150;// lowest stretch score - smallest fist

    void Start()
    {
        //Initialization of Variables -------------------------------
        responsibleUI = GameObject.Find("Canvas");//gets the UI interface
        //Left Hand & Palm
        lhand = GameObject.Find("Left Hand").transform;
        lpalm = lhand.Find("Left Palm");
        //Right Hand & Palm
        rhand = GameObject.Find("Right Hand").transform;
        rpalm = rhand.Find("Right Palm");

        palm = rpalm;//Right hand starts as active. Then based on movement we set palm to be whatever is active. 

        startTime = Time.realtimeSinceStartup;//get time for displaying time during course

        foreach (Transform right_finger in rhand)//For each finger in the right hand we add that to the right fingers
        {
            rfingers.Add(right_finger);
        }
        foreach(Transform l_finger in lhand)//For each finger in the left hand we add that to the left fingers
        {
            lfingers.Add(l_finger);
        }
        fingers = rfingers;//set fingers to rhand at start
    }

    void Update()
    {
        frame_counter++;//add one frame counter every frame
        //when frame counter is greater then 1000, a decent number of tries has  happened, so reset high score and low score so user can try to stretch an compress hand again.
        if (frame_counter >= 1000)
        {
            frame_counter = 0;//set frame_counter to 0 again
            
            //set high score and low score to 0 so that score will not increase until maximum difference is created
            high_score = 150;
            low_score = 150;
        }

        if (rhand.gameObject.activeSelf)//if right hand is active set palm and fingers to right
        {
            palm = rpalm;
            fingers = rfingers;
        }
        else if (lhand.gameObject.activeSelf)//else set palm and fingers to left
        {
            palm = lpalm;
            fingers = lfingers;
        }
        float cur_score = 100 * (GetHandPos());//current score calculates by the position of the hand

        if (cur_score < low_score)//if the current score is less than the low score, set low score to be greater than cur_score
        {
            low_score = cur_score;
            score += Mathf.Abs((int)((high_score - low_score) / 10));//add to score the difference between the scores divided by a stabilization constant
        }
        else if (cur_score > high_score)//if the current score is greater than the high score we set the high score
        {
            high_score = cur_score;
            score += Mathf.Abs((int)((high_score - low_score) / 10));//add to score the difference between the scores divided by a stabilization constant
        }
        responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString();//Update Time of what player sees
        responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString();//update score of player
    }

    float GetHandPos()//Gets the Hand Position Value by taking the sums of all finger position from the palm position 
    {
        float sumVecs = 0;//starts at 0
        foreach (Transform fing in fingers)
        {
            sumVecs += Vector3.Distance(fing.position, palm.position);//adds distance to each finger, thus seeing if it is completely stretched out or a fist
        }
        return sumVecs;//return current position value

    }

    private void OnDestroy()
    {
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("stretch - score").gameObject.GetComponent<TextMesh>().text.Substring(6));
        if (lastScore < score + 100)
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("stretch - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString();
        }

        //When user exits the game write to the text file that stores user scores along with the current date and time
        StreamWriter writer = new StreamWriter("Assets/Stretch_Test.txt", true);
        writer.WriteLine(System.DateTime.Now + " - Score: " + score);
        writer.Close();

    }


}