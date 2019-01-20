using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stretch : MonoBehaviour
{
    public Transform index_end, thumb_end, middle_end, pinky_end, ring_end, palm;
    private List<Transform> fingers = new List<Transform>();
    float HandPos1 = 0, HandPos2 = 0;

    // Use this for initialization
    void Start()
    {
        fingers.Add(index_end);
        fingers.Add(thumb_end);
        fingers.Add(middle_end);
        fingers.Add(ring_end);
        fingers.Add(pinky_end);
    }

    // Update is called once per frame
    void Update()
    {

        //Debug.Log(Vector3.Distance(palm.position, pinky_end.position));
        
       // Debug.Log(GetHandPos());
        if (HandPos1 == 0)
        {
            HandPos1 = HandPos2 = GetHandPos();
        }
        else
        {
            HandPos1 = Mathf.Max(GetHandPos(), HandPos1);
            HandPos2 = Mathf.Min(GetHandPos(), HandPos2);
        }
        Debug.Log(100*(HandPos1 - HandPos2)/0.31f);

	}
    float GetHandPos()
    {
        float sumVecs = 0;
        foreach(Transform fing in fingers)
        {
            sumVecs += Vector3.Distance(fing.position,palm.position);
        }
        return sumVecs;

    }
    
    
}
