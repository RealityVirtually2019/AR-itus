using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchListener : MonoBehaviour {

    public SoundListen manager;
    public GameObject parent;

	// Use this for initialization
	void Start () {
		
	}
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("trigger");
        manager.select(parent, other);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
