using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Gesture : MonoBehaviour
{
    private GameObject responsibleUI;//Changes UI based on movement of fingers, time, etc.

    private Transform left_hand_joints, right_hand_joints;//This controls the joints in each finger of the right and left hands respectively
    private Transform left_hand, right_hand, hand;//These are the parent object for left and right hand while the hand variable will be set to the active hand.

    private List<Transform> rfingers = new List<Transform>(), lfingers = new List<Transform>(), fingers = new List<Transform>();//rfingers holds the right fingers. while lfingers holds the left fingers. fingers is set to the active one.
    private List<List<float>> referenceValues = new List<List<float>> { new List<float> { 0, 15f, 0, 0 }, new List<float> { 0, 15.5f, 23f, 0 }, new List<float> { 0, 14.5f, 23.5f, 32f }, new List<float> { 37f, 14.5f, 26f, 33f } };//values that should happen for the joints depending on how many fingers are up.
    private List<Transform> raccuracyJoints = new List<Transform>(), laccuracyJoints = new List<Transform>(), accuracyJoints = new List<Transform>();//accuracy joints to test how accurate or proper the finger position is in. Same format as fingers

    private int wantedNumber = 1;//The number of fingers that the program asks use to put up
    private float startTime;//Start Time for Start Calculation

    private void next()
    {
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(false);
        wantedNumber = Random.Range(1, 6);
        responsibleUI.transform.Find("FingerDisplay").Find(wantedNumber.ToString()).gameObject.SetActive(true);
        startTime = Time.realtimeSinceStartup;
        Debug.Log("Lift up "+ wantedNumber+" fingers");
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
        else if(numFings == 1)
        {
            accuracy = AccuracyFirst();//If the number of fingers =  1 then we just find accuracy of 1 finger based off of horizontal and not other fingers.
        }

        int accuracyStabilizerConst = Mathf.Abs(wantedNumber + 1 - numFings);//if wanted number == numFings up then we want this to be one, otherwise our accuracy will be decreased base on how off it is
        accuracy = accuracy / accuracyStabilizerConst; //divide accuracy by stabilizer constant
        totAccuracy += accuracy;//Add the current accuracy to the total accuracy

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
        int lastScore = int.Parse(responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text.Substring(6));
        Debug.Log(lastScore);
        Debug.Log(score);
        if (lastScore  < score + 100)
        {
            responsibleUI.transform.parent.Find("Exercise Selection").Find("finger - score").gameObject.GetComponent<TextMesh>().text = "Goal: " + (score + 100).ToString();
        }
        
        StreamWriter writer = new StreamWriter("Assets/Finger_Test.txt", true);//Get output file to write results to
        writer.WriteLine(System.DateTime.Now + " - Score: "+ score);//writes score along with the current date and time so it can be shown to doctors during check ups
        writer.Close(); //closes file
        
    }
}
