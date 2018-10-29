using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	public float speed;
	public Text chordText;
	public Text embelText;
	public Text winText;

    public int nTarget;
	public int nBonus;
	
	//private Rigidbody rb;
	private int count;
	private int countBonus;
    private float moveHorizontal = 0;

    float yPos;
	float yPosPrev;

    ChuckSubInstance myChuckPitchTrack;
	ChuckFloatSyncer myAdvancerSyncer;
    ChuckEventListener myAdvancerListener;

	void Start () 
	{
		//rb = GetComponent<Rigidbody>();
		count = 0;
		countBonus = 0;
		SetCountText();
		winText.text = "";

        myChuckPitchTrack = GetComponent<ChuckSubInstance>();
		yPos = 0;
		yPosPrev = 0;

        
		myChuckPitchTrack.RunCode(@"

			global float midiPos;
			
			// analysis
			adc => PoleZero dcblock => FFT fft => dac;
			// synthesis
			//SinOsc s => JCRev r => dac;
			//0 => s.gain;

			// set reverb mix
			//.05 => r.mix;
			// set to block DC
			.99 => dcblock.blockZero;
			// set FFT params
			1024 => fft.size;
			// window
			Windowing.hamming( fft.size() ) => fft.window;

			// to hold result
			UAnaBlob blob;
			// find sample rate
			second / samp => float srate;

			// interpolate
			float target_freq, curr_freq, target_gain, curr_gain;
			spork ~ ramp_stuff();

			// go for it
			while( true )
			{
				// take fft
				fft.upchuck() @=> blob;
				// find peak
				0 => float max; int where;
				for( int i; i < blob.fvals().cap(); i++ )
				{
					// compare
					if( blob.fvals()[i] > max )
					{
						// save
						blob.fvals()[i] => max;
						i => where;      
					}    
				}  
				// set freq
				(where $ float) / fft.size() * srate => target_freq;
				
				// set gain
				(max / .8) => target_gain;
				
				// hop
				(fft.size()/2)::samp => now;
			}

			//convert freq to perceptually relevant scale
			fun float midi_scale(float f)
			{
				Math.round(69 + 12*Math.log2(f/440.0)) => float m;
				//<<< ""m: "", m >>>;
				return m;
			}
				// interpolation
			fun void ramp_stuff()
			{
				// mysterious 'slew'
				0.025 => float slew;
				float m_cf;
				float m_pf;

				// infinite time loop
				0 => int count;
				-1.0 => float prev_freq;
				while (true)
				{
					(target_freq - curr_freq) * 5 * slew + curr_freq => curr_freq;
					(target_gain - curr_gain) * slew + curr_gain => curr_gain;
					if (prev_freq > -1.0 && curr_freq >= 110.0 && curr_freq <= 1760)  //plausible sung pitch
					{
						midi_scale(curr_freq) => m_cf;
						midi_scale(prev_freq) => m_pf;
						if (m_cf == m_pf)
						{
							count++;
							if (count >= 10)
							{
								//curr_freq => s.freq;
								//0 => s.gain;
								m_cf => midiPos;
							//<<< ""Note:"", midiPos >>>; //test to check input       
							}
						}
						else
						{
							0 => count;
							//0 => s.freq;
							//0 => s.gain;
						}
					}
					curr_freq => prev_freq;
					0.0025::second => now;
				}
			}
			
		");
		

        myAdvancerSyncer = gameObject.AddComponent<ChuckFloatSyncer>();
        myAdvancerSyncer.SyncFloat(myChuckPitchTrack, "midiPos"); //current instance of chuck is determining pos value

	}
	void Update() 
	{
		yPos =  myAdvancerSyncer.GetCurrentValue();
		
		float offset = -60+14;
		float verticalPosition = yPos + offset; //sets middle c to height 14
        moveHorizontal += Time.deltaTime * speed;
        //Debug.Log("vert. pos. " + yPos);

		
		Vector3 movement = new Vector3 (moveHorizontal,verticalPosition,0.0f);
        transform.position = movement;


	}

	void OnTriggerEnter(Collider other) //play notes
	{
        if(other.gameObject.CompareTag("Target")) {
			//other.gameObject.SetActive(false);
			count++; 
			SetCountText();

			
			GetComponent<ChuckSubInstance>().RunCode(string.Format(@"
				SinOsc foo => dac;
				
				// function to get chords based on count
				
				//mtof({0}) => foo.freq;
				{0} => int nada;
				440 => foo.freq;
				100::ms => now;
			", yPos ) );
		
            
			Debug.Log("trigger count: " + count + ", @ MIDI " + yPos);
        }
		
        if (other.gameObject.CompareTag("Embel"))
        {
            //other.gameObject.SetActive(false);
            countBonus++;
            SetCountText();

			/* 
            GetComponent<ChuckSubInstance>().RunCode(string.Format(@"
				SinOsc foo => dac;
				
				// function to get chords based on count
				
				//mtof({0}) => foo.freq;
				{0} => int nada;
				880 => foo.freq;
				100::ms => now;
			", yPos));
			*/


            Debug.Log("trigger bonus: " + countBonus + ", @ MIDI " + yPos);
        }

    }

 
	void SetCountText () 
	{
		
		chordText.text = "Chords: " + count.ToString();
        embelText.text = "Bonuses: " + countBonus.ToString();

		if (count>=12) {
			winText.text = "You Win!";
		}
	}


}

