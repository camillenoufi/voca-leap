using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioAnalyzer : MonoBehaviour {
	ChuckSubInstance myChuck;
	ChuckFloatSyncer myGetLoudnessSyncer;
	
	// Use this for initialization
	void Start () 
	{
		myChuck = GetComponent<ChuckSubInstance>();
		myChuck.RunCode(@"
			global float dacLoudness;

			dac=> FFT fft =^ RMS rms => blackhole;
			1024 => fft.size;
			Windowing.hann(fft.size() ) => fft.window;

			while(true)
			{
				//upchuck: take fft then rms
				rms.upchuck() @=> UAnaBlob data;

				//store value in global
				data.fval(0) => dacLoudness; //where is this data var coming from?

				//advance time
				fft.size()::samp=>now;
			}
		");

		myGetLoudnessSyncer = gameObject.AddComponent<ChuckFloatSyncer>();
		myGetLoudnessSyncer.SyncFloat(myChuck,"dacLoudness");

	}
	
	// Update is called once per frame
	void Update () 
	{
		Debug.Log("most recent dac loudness: " +
			myGetLoudnessSyncer.GetCurrentValue().ToString("0.000")
		);	
	}
}
