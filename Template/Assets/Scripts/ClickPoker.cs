using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickPoker : MonoBehaviour
{
    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
           
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Clicked");
            //Raycasting for detecting if mouse is on the cube
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit!");

                var collider = hit.collider;
                Debug.Log(collider.name);
            }
        }
    }
}
