using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerCounter : MonoBehaviour
{
    public Transform thumb, index, middle, ring, pinky, joint_ti, joint_im, joint_mr, joint_rp;
    private List<Transform> fingers = new List<Transform>();
    private List<Transform> accuracyJoints = new List<Transform>();
    //Thumb-Index, Index-Middle, Middle-Ring, Ring-Pinky
    private const float accuracyConst = 30f;
    private int previousNum = 0;
    void Start()
    {
        Debug.Log("Starting");
        fingers.Add(thumb);
        fingers.Add(index);
        fingers.Add(middle);
        fingers.Add(pinky);
        fingers.Add(ring);

        accuracyJoints.Add(joint_ti);
        accuracyJoints.Add(joint_im);
        accuracyJoints.Add(joint_mr);
        accuracyJoints.Add(joint_rp);

    }

    // Update is called once per frame
    void Update()
    {
       int numFings = 0;
       foreach (Transform curFing in fingers){
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
                else if (Mathf.Rad2Deg*(Mathf.Atan(Mathf.Abs((nextFing.GetChild(0).position.y - nextFing.position.y) / (nextFing.GetChild(0).position.x - nextFing.position.x))))<50) 
                {
                    break;
                }
                else
                {
                    nextFing = nextFing.GetChild(0);
                }
            }
        }
        if (numFings != previousNum)
        {
            Debug.Log(numFings);
            previousNum = numFings;
        }
        Debug.Log(Accuracy());
    }
    float Accuracy()
    {
        float curAccuracy = 100.0f;
        float DeviationFromStandard = 0;
        for (int i = 1; i<fingers.Count; i++)
        {
            Transform endPoint_1 = FindEndPoint(fingers[i]);
            Transform endPoint_2 = FindEndPoint(fingers[i - 1]);
            float Side_InBetweenEndPoints = Vector3.Distance(endPoint_1.position, endPoint_2.position);
            float Side_F1toJoint = Vector3.Distance(endPoint_1.position, accuracyJoints[i - 1].position);
            float Side_F2toJoint = Vector3.Distance(endPoint_2.position, accuracyJoints[i - 1].position);
            float theta = Mathf.Rad2Deg*Mathf.Acos((Mathf.Pow(Side_InBetweenEndPoints,2)-Mathf.Pow(Side_F1toJoint,2)-Mathf.Pow(Side_F2toJoint,2))/(-2*Side_F2toJoint*Side_F1toJoint));
            DeviationFromStandard += Mathf.Abs(theta - accuracyConst)/accuracyConst;
            Debug.Log(DeviationFromStandard);
            
        }
        return 100 - DeviationFromStandard;
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
    
}
