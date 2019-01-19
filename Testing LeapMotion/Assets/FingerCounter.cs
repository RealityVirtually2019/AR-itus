using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FingerCounter : MonoBehaviour
{
    public Transform thumb, index, middle, ring, pinky;
    private List<Transform> fingers = new List<Transform>();
    // Start is called before the first frame update
    private int previousNum = 0;
    void Start()
    {
        Debug.Log("Starting");
        fingers.Add(thumb);
        fingers.Add(index);
        fingers.Add(pinky);
        fingers.Add(ring);
        fingers.Add(middle);
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
                else if (nextFing.position.y>=nextFing.GetChild(0).position.y)
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
    }
}
