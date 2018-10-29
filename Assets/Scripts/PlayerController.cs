using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

	// Public variables
	public float speed;
	public float goalPosition;
	public Text chordText;
	public Text embelText;
    public Text midiText;
	public Text winText;
	public float midiStartNote;
	public float zPos;

    public int nTarget;
	public int nBonus;
	
	//Private Variables
	private int count = -1;
	private int countBonus =-1;

    private float xPos = -1;
    private float yPosPrev =-1;

	// Chuck stuff
    ChuckSubInstance myChuckPitchTrack;
	ChuckFloatSyncer myPitchSyncer;
    ChuckFloatSyncer myTimeSyncer;
    float yPos; //syncer variable

    void Start () 
	{
		// set display variables
		count = 0;
		countBonus = 0;
		SetCountText();
		winText.text = "";

		// set chuck position tracking variables
		yPos = midiStartNote;
		yPosPrev = yPos;

		// set up chuck
        myChuckPitchTrack = GetComponent<ChuckSubInstance>();
        StartChuckPitchTrack(myChuckPitchTrack);
        myPitchSyncer = gameObject.AddComponent<ChuckFloatSyncer>();
        myPitchSyncer.SyncFloat(myChuckPitchTrack, "midiPos"); //current instance of chuck is determining pos value
        myTimeSyncer = gameObject.AddComponent<ChuckFloatSyncer>();
        myTimeSyncer.SyncFloat(myChuckPitchTrack, "timePos"); //current instance of chuck is determining pos value

    }
	void Update() 
	{
        xPos += Time.deltaTime * speed;
		//xPos = speed * myTimeSyncer.GetCurrentValue();
        //Debug.Log(xPos);

        yPos =  myPitchSyncer.GetCurrentValue();
        //octave offshoot error correction for intervals +/- a 9th (13 semitones)
        if ((yPos < yPosPrev - 13.0f) | (yPos > yPosPrev + 13.0f)) { yPos = yPosPrev; }
        //Debug.Log(yPos);

		Vector3 movement = new Vector3 (xPos,yPos,zPos);
        transform.position = movement;

		if(xPos>goalPosition) 
			DetermineGameState();
		else
            transform.position = movement;

	}

	void OnTriggerEnter(Collider other) //play back incoming midi note
	{
		// Target trigger
		if(other.gameObject.CompareTag("Target")) {
			//other.gameObject.SetActive(false);
			count++; 
			SetCountText();

            //Debug.Log("trigger count: " + count + ", @ MIDI " + yPos);
            GetComponent<ChuckSubInstance>().RunCode(string.Format(@"
				//TriOsc foo => dac;
				{0} => float midi;

				Mandolin foo => JCRev r => dac;
				Mandolin foo2 => r => dac;
				.5 => r.gain;
				.025 => r.mix;
				//e.set( 5::ms, 5::ms, .5, 20::ms );
				//<<< midi >>>;
				spork ~ play( midi ); play2( (midi+4.0) );
				1::second => now;

				fun void play( float note )
				{{
						// start the note
					Std.mtof(midi) => foo.freq;
					Math.random2f( .6, .9 ) => foo.pluck;
				}}
				fun void play2( float note )
				{{
						// start the note
					Std.mtof(midi) => foo2.freq;
					Math.random2f( .6, .9 ) => foo2.pluck;
				}}

			", yPos));
        }
		
		// Embellishment trigger
	    if (other.gameObject.CompareTag("Embel"))
        {
            //other.gameObject.SetActive(false);
            countBonus++;
            SetCountText();
			
            GetComponent<ChuckSubInstance>().RunCode(string.Format(@"
				//TriOsc foo => dac;
				{0} => float midi;

				Mandolin foo => JCRev r => dac;
				.75 => r.gain;
				.025 => r.mix;
				//e.set( 5::ms, 5::ms, .5, 10::ms );

				//<<< midi >>>;
				Std.mtof(midi) => foo.freq;
				Math.random2f( .6, .9 ) => foo.pluck;
				250::ms => now;
			", yPos));
            //Debug.Log("trigger bonus: " + countBonus + ", @ MIDI " + yPos);
        }
    }

 
	void SetCountText () 
	{	
		chordText.text = "Chords: " + count.ToString();
        embelText.text = "Fun n' Fancy: " + countBonus.ToString();
        midiText.text = "MIDI note: " + yPos.ToString();
	}

	void DetermineGameState () 
	{
        if (count >= 0.9 * nTarget)
        {
            winText.text = "Great Job! Singin' like a champ!";
            RenderSettings.haloStrength = 5;
        }
        else 
		{
			if (count >= 0.6 * nTarget) 
                winText.text = "Almost There! Keep at it!";
			else 
                winText.text = "Keep going!";
            xPos = -5;
            yPos = midiStartNote;
			count = 0;
			countBonus = 0;
			SetCountText();
		}
	}


	
	// ChucK pitch tracking script contained here
	void StartChuckPitchTrack(ChuckSubInstance myChuckPitchTrack) 
	{
        // instantiate Chuck Pitch Tracking code
        myChuckPitchTrack.RunCode(@"

			60.0 => global float midiPos;
			0.0 => global float timePos;
			
			// analysis
			adc => PoleZero dcblock => FFT fft => dac;

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

			Mandolin foo => JCRev r => dac;
				.75 => r.gain;
				.025 => r.mix;
				Std.mtof(60) => foo.freq;
				Math.random2f( .6, .9 ) => foo.pluck;
				500::ms => now;

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
						Math.round(Std.ftom(curr_freq)) => m_cf;
						Math.round(Std.ftom(prev_freq)) => m_pf;
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
					0.0025 + timePos => timePos;
				}
			}
			
		");
	}

}

