using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	public float speed;
	public Text countText;
	public Text winText;
	
	private Rigidbody rb;
	private int count;

	void Start () 
	{
		rb = GetComponent<Rigidbody>();
		count = 0;
		SetCountText();
		winText.text = "";

		GetComponent<ChuckSubInstance>().RunCode(@"

			global float impactIntensity;
			global Event impactHappened;
			
			fun void playImpact(float intensity) 
			{
				SndBuf impactBuf => dac;
				me.dir() + ""fire.wav"" => impactBuf.read;

				//start at the beginning of the clip
				0 => impactBuf.pos;

				//set rate: least intense is fast, most intense is slow, range 0.4 to 1.6
				1.5- intensity + Math.random2f(-0.1,0.1) => impactBuf.rate;

				chout <= ""Rate is "" <= impactBuf.rate() <= IO.newline();

				//set gain: least intense is quiet, most intense is loud, range from 0.05 to 1
				0.05 + 0.95 * intensity => impactBuf.gain;

				//pass time so that file plays
				impactBuf.length() / impactBuf.rate() => now;
			}

			while(true)
			{
				impactHappened => now;
				spork ~ playImpact(impactIntensity);
			}
			
		");

	}
	void Update() 
	{
		float moveHorizontal = Input.GetAxis("Horizontal");
		float moveVertical = Input.GetAxis("Vertical");

		Vector3 movement = new Vector3 (moveHorizontal,moveVertical,0.0f);

		//rb.AddForce (movement * speed); //if you ever want to physically roll!
		transform.position += 2*Time.deltaTime*movement;


	}

	void OnTriggerEnter(Collider other) 
	{
        if(other.gameObject.CompareTag("Pick Up")) {
			other.gameObject.SetActive(false);
			count++; 
			SetCountText();

			GetComponent<ChuckSubInstance>().RunCode(string.Format(@"
				SinOsc foo => dac;
				repeat({0}) 
				{{
					Math.random2f(300,1000) => foo.freq;
					100::ms => now;
				}}
			", count ) );
		}
    }

	void OnCollisionEnter(Collision collision)
	{
		//map and clamp from [0,16] to [0,1]
		float intensity = Mathf.Clamp01(collision.relativeVelocity.magnitude / 16);
		
		//square it to make the ramp upward more dramatic
		intensity = intensity*intensity;

		GetComponent<ChuckSubInstance>().SetFloat("impactIntensity", intensity);
		GetComponent<ChuckSubInstance>().BroadcastEvent("impactHappened");

	}

	void SetCountText () 
	{
		countText.text = "Count: " + count.ToString();
		if (count>=12) {
			winText.text = "You Win!";
		}
	}

}

