using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupAudio : MonoBehaviour {

	// Use this for initialization
	void Start () {

		GetComponent<ChuckSubInstance>().RunCode(@"
			TriOsc spatialOsc => dac;
			while(true) {
				Math.random2f(300,1000) => spatialOsc.freq;
				50::ms => now;
			}
		");
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
