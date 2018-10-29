using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetController : MonoBehaviour {

	Behaviour halo;

	// Use this for initialization
	void Start () {
        halo = (Behaviour)GetComponent("Halo");
        halo.enabled = false; // false
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTriggerEnter(Collider other) //turn on halo
    {
        if (other.gameObject.CompareTag("Player"))
        {
            halo.enabled = true; // false
			//Debug.Log("target triggered");
        }
    }
}
