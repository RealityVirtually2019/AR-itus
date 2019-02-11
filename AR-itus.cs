//Anish Aggarwal, Noor Nasri, Zhehai Zhang
//December 11th 2019
//ICS4U-01
//Mr. McKenzie
//A script that loads other scripts based on voice commands.

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

//FULL WORKING VIDEO OF OUR WORK
//https://www.youtube.com/watch?v=tKCXJWlN_xw
//https://www.youtube.com/watch?v=X7Yiz422z7g

//Github: https://github.com/RealityVirtually2019/AR-itus

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
        recognition.DictationComplete += (completionCause) => { };
        recognition.DictationError += (error, hresult) => { };

        // start the speech recognition 
        recognition.Start();
    }
}

//Anish Aggarwal, Noor Nasri, Zhehai Zhang
//December 11th 2019
//ICS4U-01
//Mr. McKenzie
//A gesture script that randomizes number gestures by the hand. It gives points based on the accuracy of the gesture 
//and won't move on to the next gesture until the gesture is at least over 50% accuracy.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Gesture : MonoBehaviour
{
    private GameObject responsibleUI; //Changes UI based on movement of fingers, time, etc.

    private Transform left_hand_joints, right_hand_joints;//This controls the joints in each finger of the right and left hands respectively
    private Transform left_hand, right_hand, hand;//These are the parent object for left and right hand while the hand variable will be set to the active hand.

    private List<Transform> rfingers = new List<Transform>(), lfingers = new List<Transform>(), fingers = new List<Transform>();//rfingers holds the right fingers. while lfingers holds the left fingers. fingers is set to the active one.
    private List<List<float>> referenceValues = new List<List<float>> { new List<float> { 0, 15f, 0, 0 }, new List<float> { 0, 15.5f, 23f, 0 }, new List<float> { 0, 14.5f, 23.5f, 32f }, new List<float> { 37f, 14.5f, 26f, 33f } }; //values that should happen for the joints depending on how many fingers are up.
    private List<Transform> raccuracyJoints = new List<Transform>(), laccuracyJoints = new List<Transform>(), accuracyJoints = new List<Transform>();//accuracy joints to test how accurate or proper the finger position is in. Same format as fingers

    private int wantedNumber = 1; //The number of fingers that the program asks use to put up
    private float startTime; //Start Time for Start Calculation

    private void next()
    {
        // switch to the next hand model 
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(false);  // turn off current one
        wantedNumber = Random.Range(1, 6); // choose new model
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(true // set new model as visible
        startTime = Time.realtimeSinceStartup; // adjust time variable 
        Debug.Log("Lift up " + wantedNumber + " fingers"); // tell us how many fingers we need 
    }

    private void Start()
    {
        responsibleUI = GameObject.Find("Canvas");//gets the UI interface
        //Left Hand & Palm Objects
        left_hand = GameObject.Find("Left Hand").transform;
        left_hand_joints = GameObject.Find("Left Hand Joints").transform;
        //Right Hand & Palm Objects
        right_hand = GameObject.Find("Right Hand").transform;
        right_hand_joints = GameObject.Find("Right Hand Joints").transform;

        hand = right_hand;//Strart with right hand active

        next();//Set new values for num fingers for user to put up

        foreach (Transform right_finger in right_hand)//For each finger in the right hand we add that to the right fingers
        {
            rfingers.Add(right_finger);
        }
        foreach (Transform l_finger in left_hand)//For each finger in the left hand we add that to the left fingers
        {
            lfingers.Add(l_finger);
        }
        fingers = rfingers;//set fingers to rhand at start

        foreach (Transform left_joint in left_hand_joints)//Fore each joint on left_hand add joints to l accuracy points
        {
            laccuracyJoints.Add(left_joint);
        }
        foreach (Transform right_joint in right_hand_joints)//For each joint on right hand add the joints to r accuracy points
        {
            laccuracyJoints.Add(right_joint);
        }
        accuracyJoints = raccuracyJoints;//set accuracy joints to the right hand to start

    }

    int curIter = 0;
    int correctTimes = 0;
    int i = 0;
    float totAccuracy = 0;
    int score = 0;

    private void Update()
    {
        if (right_hand.gameObject.activeSelf)//if user has right hand on screen, judge based off of right hand
        {
            //Set all varuables to r values
            fingers = rfingers;
            accuracyJoints = raccuracyJoints;
            hand = right_hand;
        }
        else//if user has left hand on screen, judge based off of left hand
        {
            //Set all values to l values
            hand = left_hand;
            fingers = lfingers;
            accuracyJoints = laccuracyJoints;
        }

        int numFings = 0;//number of fingers currently up starts at 0 and then we count which ones are up
        int angleStabilizer = 50; //if the angle of the finger to the horizontal is greater than this value then we will count it up. Otherwise, it will not be considered as being up.
        foreach (Transform curFing in fingers)//foreach finger on the hand we loop through the parts of the finger which are its children
        {
            Transform nextFing = curFing;//set the next finger to check to the current finger
            while (true)//keep looping through children while it has children or parts
            {
                if (nextFing.childCount == 0)//if it has no more children we break and add one to the num fingers because that means it has not broken until it got to the last part which means the finger is up
                {
                    numFings++;
                    break;
                }
                else if (nextFing.position.y >= nextFing.GetChild(0).position.y)//if the current position of the piece is higher up then the next segment of the finger then that means the finger is not put up correctly 
                {
                    break;
                }
                else if (Mathf.Rad2Deg * (Mathf.Atan(Mathf.Abs((nextFing.GetChild(0).position.y - nextFing.position.y) / (nextFing.GetChild(0).position.x - nextFing.position.x)))) < angleStabilizer)//if finger does not pass angle test to horizonta it is not considered to be up
                {
                    break;
                }
                else //if it has passed all tests then we move on to next child/segment
                {
                    nextFing = nextFing.GetChild(0);
                }
            }
        }

        float accuracy = 0;//Gets accuracy of the fingers that are up
        if (numFings > 1)
        {
            accuracy = Accuracy(numFings);//given number of fingers up finds accuracy
        }
        else if (numFings == 1)
        {
            accuracy = AccuracyFirst();//If the number of fingers =  1 then we just find accuracy of 1 finger based off of horizontal and not other fingers.
        }

        int accuracyStabilizerConst = Mathf.Abs(wantedNumber + 1 - numFings);//if wanted number == numFings up then we want this to be one, otherwise our accuracy will be decreased base on how off it is
        accuracy = accuracy / accuracyStabilizerConst; //divide accuracy by stabilizer constant
        totAccuracy += accuracy; //Add the current accuracy to the total accuracy

        if (i == 20) // every 20 times, take the average accuracy 
        {
            i = 0; // reset the counter
            accuracy = totAccuracy / 20; // get average acuracy 
            totAccuracy = 0; // reset total accuracy 


            if (accuracy > 40 && wantedNumber == numFings) // they are close enough to pass
            {
                correctTimes++; // add 1 to the correct # of attempts 
                if (correctTimes >= 2) // if they got it twice, they can move on. This makes it consistent so outlier calculations don't kill it
                {
                    Debug.Log("Good job,  you took " + (Time.realtimeSinceStartup - startTime) + " seconds");
                    score += (int)(accuracy / (Time.realtimeSinceStartup - startTime)); // add to their score
                    next(); // switch to the next hand model
                }
            }

            curIter++; // add to the counter
            if (curIter >= 3) // if they hit three times and still didn't move to next, then reset. They need 2/3 right.
            {
                // reset variables
                curIter = 0;
                correctTimes = 0;
            }

            // Set all the text to be accurate
            responsibleUI.transform.Find("Timer").GetComponent<TextMesh>().text = "Time: " + ((int)(Time.realtimeSinceStartup - startTime)).ToString(); // amount of time so far
            responsibleUI.transform.Find("Accuracy").GetComponent<TextMesh>().text = "Accuracy: " + ((int)(accuracy)).ToString() + "%"; // the current accuracy 
            responsibleUI.transform.Find("Score").GetComponent<TextMesh>().text = "Score: " + score.ToString(); // their current score
        }

        i++;

    }

    private float AccuracyFirst()//Gets the accuracy for 1 finger up
    {
        float DeviationFromStandard = 0, change_x = 0, change_z = 0;
        Transform startTransform = hand.Find("index");//We need to get the index finger for 1 finger as that is the finger that should be up and we are testing for

        while (true)
        {
            if (startTransform.childCount == 0)//if the parent has nor children we are done just like before
            {
                break;
            }
            else
            {
                change_x += Mathf.Abs(startTransform.position.x - startTransform.GetChild(0).position.x) / startTransform.position.x;//add the change in x from current position to the next
                change_z += Mathf.Abs(startTransform.position.z - startTransform.GetChild(0).position.z) / startTransform.position.z;//add the change in z from current position to nex
                DeviationFromStandard += 100 - change_z * 100 - change_x * 100;//add to the deviation from standard to be 100 - 100*(changex+changex) so we encompass both change values
                startTransform = startTransform.GetChild(0);//go to next segment
            }
        }
        float accuracy = DeviationFromStandard;
        accuracy = Mathf.Max(0, accuracy);//accuracy cant be less than 0
        accuracy = Mathf.Min(100, accuracy);//accuracy cant be greater than 100
        return accuracy;
    }
    private float Accuracy(int curFingersUp)//Gets accuracy for current fingers based off of what fingers are up and compares them to refferen points
    {
        float DeviationFromStandard = 0;//Start with deviation being 0
        for (int i = 1; i < fingers.Count; i++)//for each finger starting at 1 which to the end. Because we compare it to the one before it. We get the angle between the fingers to test whether or not the fingers are accurate.
        {
            Transform endPoint_1 = FindEndPoint(fingers[i]);//We get the endpoint of one finger
            Transform endPoint_2 = FindEndPoint(fingers[i - 1]);//Get the endpoint of an adjacent finger
            float Side_InBetweenEndPoints = Vector3.Distance(endPoint_1.position, endPoint_2.position);//get distance between end points.
            float Side_F1toJoint = Vector3.Distance(endPoint_1.position, accuracyJoints[i - 1].position);//get distance from an accuracy joint to the each endpoint.
            float Side_F2toJoint = Vector3.Distance(endPoint_2.position, accuracyJoints[i - 1].position);// Now we have a Side-Side-Side triangle and we can find the angle inbetween using cosine law. Inorder to get the angle. 
            float theta = Mathf.Rad2Deg * Mathf.Acos((Mathf.Pow(Side_InBetweenEndPoints, 2) - Mathf.Pow(Side_F1toJoint, 2) - Mathf.Pow(Side_F2toJoint, 2)) / (-2 * Side_F2toJoint * Side_F1toJoint));//Uses cos inverse of (c^2 - a^2 - b^2)/-2ab to find inverse

            if (referenceValues[curFingersUp - 2][i - 1] != 0)//if current value of the angle is not 0 that means there is an angle between the two
            {
                float change = Mathf.Abs(referenceValues[curFingersUp - 2][i - 1] - theta) / referenceValues[curFingersUp - 2][i - 1];//gets percent change between the reference value and the current value
                DeviationFromStandard += change;//adds change to deviation
            }

        }
        float accuracy = (DeviationFromStandard / (curFingersUp - 1));//Deviation divided by the number of fingers that are up
        accuracy = Mathf.Max(0, accuracy);//accuracy cannot be less than 0
        accuracy = Mathf.Min(100, accuracy);//accuracy cannot be more than 100


        return accuracy;//return the accuracy
    }
    private Transform FindEndPoint(Transform startFinger)//Find end point to be referrenced for getting the angle
    {
        while (startFinger.childCount != 0)//while it has children it is not the end
        {
            startFinger = startFinger.GetChild(0); //set it to its child
        }
        return startFinger; //send end point
    }

    private void OnDestroy()
    {
        // when this is destroyed, they returned to the other menu. Check if this score is higher than their high score and set a goal accordingly
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text.Substring(6)); // the old goal
        Debug.Log(lastScore);
        Debug.Log(score);
        if (lastScore < score + 100) // checking if our score is a high score
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString(); // change the goal text to be a 100 above high score
        }

        StreamWriter writer = new StreamWriter("Assets/Finger_Test.txt", true); //Get output file to write results to
        writer.WriteLine(System.DateTime.Now + " - Score: " + score); //writes score along with the current date and time so it can be shown to doctors during check ups
        writer.Close(); //closes file

    }
}



//Anish Aggarwal, Noor Nasri, Zhehai Zhang
//December 11th 2019
//ICS4U-01
//Mr. McKenzie
//A balance script that measures how much the hand shakes. It gives an amount of points based on how steady their hands are.
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
    float score = 0;
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
            if (FrameCounter % FrameLength == 0)
            {//if its time to reset 

                Total_Deviation = Total_Deviation / FrameLength;//set total deviation to be averages of all total deviations
                score += Total_Deviation;//add to score
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

//Anish Aggarwal, Noor Nasri, Zhehai Zhang
//December 11th 2019
//ICS4U-01
//Mr. McKenzie
//A stretch script that gives the user points based on stretching their fingers and how frequently they stretch their fingers.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Stretch : MonoBehaviour
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
        foreach (Transform l_finger in lhand)//For each finger in the left hand we add that to the left fingers
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
        // when this is destroyed, they returned to the other menu. Check if this score is higher than their high score and set a goal accordingly
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("stretch - score").gameObject.GetComponent<TextMesh>().text.Substring(6));// the old goal
        if (lastScore < score + 100)// checking if our score is a high score
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("stretch - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString(); // change the goal text to be a 100 above high score
        }

        //When user exits the game write to the text file that stores user scores along with the current date and time
        StreamWriter writer = new StreamWriter("Assets/Stretch_Test.txt", true);
        writer.WriteLine(System.DateTime.Now + " - Score: " + score);
        writer.Close();

    }

}




