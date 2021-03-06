﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CSharpSynth.Effects;
using CSharpSynth.Sequencer;
using CSharpSynth.Synthesis;
using CSharpSynth.Midi;

[RequireComponent(typeof(AudioSource))]
public class MIDIPlayer : MonoBehaviour
{
    //Public
    //Check the Midi's file folder for different songs
    public string midiFilePath = "Midis/Groove.mid";
    public bool ShouldPlayFile = true;

    //Try also: "FM Bank/fm" or "Analog Bank/analog" for some different sounds
    public string bankFilePath = "GM Bank/gm";
    public int bufferSize = 1024;
    public int midiNote = 60;
    public int midiNoteVolume = 100;
    [Range(0, 127)] //From Piano to Gunshot
    public int midiInstrument = 0;

    public Actor[] actors;

    //Private 
    private float[] sampleBuffer;
    private float gain = 1f;
    private MidiSequencer midiSequencer;
    private MidiSequencer midiSequencerDelayed;
    private StreamSynthesizer midiStreamSynthesizer;
    private StreamSynthesizer midiStreamSynthesizerDelayed;

    private float synchTimer = 1f;
    private bool hasStartedTrack;

    //private float sliderValue = 1.0f;
    //private float maxSliderValue = 127.0f;

    // Awake is called when the script instance
    // is being loaded.
    void Awake()
    {
        midiStreamSynthesizer = new StreamSynthesizer(44100, 2, bufferSize, 40);
        midiStreamSynthesizerDelayed = new StreamSynthesizer(44100, 2, bufferSize, 40);
        midiStreamSynthesizerDelayed.MasterVolume = 0.0f;
        //midiStreamSynthesizerDelayed.MasterVolume = 0.0f;

        sampleBuffer = new float[midiStreamSynthesizer.BufferSize];
        
        midiStreamSynthesizer.LoadBank(bankFilePath);
        midiStreamSynthesizerDelayed.LoadBank(bankFilePath);

        midiSequencer = new MidiSequencer(midiStreamSynthesizer);
        midiSequencerDelayed = new MidiSequencer(midiStreamSynthesizerDelayed);

        //These will be fired by the midiSequencer when a song plays. Check the console for messages if you uncomment these
        //midiSequencer.NoteOnEvent += new MidiSequencer.NoteOnEventHandler (MidiNoteOnHandler);
        //midiSequencer.NoteOffEvent += new MidiSequencer.NoteOffEventHandler (MidiNoteOffHandler);			
        midiSequencer.NoteOnEvent += midiSequencer_NoteOnEvent;
        midiSequencer.NoteOffEvent += midiSequencer_NoteOffEvent;
        midiSequencerDelayed.NoteOnEvent += midiSequencer_NoteOnEventDelayed;
        midiSequencerDelayed.NoteOffEvent += midiSequencer_NoteOffEventDelayed;
    }

    void LoadSong(string midiPath)
    {
        midiSequencer.LoadMidi(midiPath, false);
        midiSequencerDelayed.LoadMidi(midiPath, false);
        midiSequencerDelayed.Play();
    }

    void midiSequencer_NoteOnEvent(int channel, int note, int velocity)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(NoteOn(channel, note, velocity));
    }

    void midiSequencer_NoteOffEvent(int channel, int note)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(NoteOff(channel, note));
    }

    void midiSequencer_NoteOnEventDelayed(int channel, int note, int velocity)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(NoteOnDelayed(channel, note, velocity));
    }

    void midiSequencer_NoteOffEventDelayed(int channel, int note)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(NoteOffDelayed(channel, note));
    }


    IEnumerator NoteOn(int channel, int note, int velocity)
    {
        foreach (Actor actor in actors)
        {
            actor.NoteOnEvent(channel, note, velocity);
        }

        yield return null;
    }

    IEnumerator NoteOff(int channel, int note)
    {
        foreach (Actor actor in actors)
        {
            actor.NoteOffEvent(channel, note);
        }

        yield return null;
    }

    IEnumerator NoteOnDelayed(int channel, int note, int velocity)
    {
        foreach (Actor actor in actors)
        {
            actor.NoteOnEventDelayed(channel, note, velocity);
        }

        yield return null;
    }

    IEnumerator NoteOffDelayed(int channel, int note)
    {
        foreach (Actor actor in actors)
        {
            actor.NoteOffEventDelayed(channel, note);
        }

        yield return null;
    }

    // Start is called just before any of the
    // Update methods is called the first time.
    void Start()
    {
    }

    // Update is called every frame, if the
    // MonoBehaviour is enabled.
    void Update()
    {
        if (!midiSequencer.isPlaying)
        {
            //if (!GetComponent<AudioSource>().isPlaying)
            if (ShouldPlayFile)
            {
                LoadSong(midiFilePath);
                
                if (synchTimer > 0 && !hasStartedTrack) 
                    synchTimer -= Time.deltaTime;
                else if (!hasStartedTrack)
                {
                    midiSequencer.Play();
                    hasStartedTrack = true;
                }
            }
        }
        else if (!ShouldPlayFile)
        {
            midiSequencer.Stop(true);
            midiSequencerDelayed.Stop(true);
        }

        if (Input.GetButtonDown("Fire1"))
        {
            midiStreamSynthesizer.NoteOn(0, midiNote, midiNoteVolume, midiInstrument);
        }

        if (Input.GetButtonUp("Fire1"))
        {
            midiStreamSynthesizer.NoteOff(0, midiNote);
        }


        }

        // See http://unity3d.com/support/documentation/ScriptReference/MonoBehaviour.OnAudioFilterRead.html for reference code
        //	If OnAudioFilterRead is implemented, Unity will insert a custom filter into the audio DSP chain.
        //
        //	The filter is inserted in the same order as the MonoBehaviour script is shown in the inspector. 	
        //	OnAudioFilterRead is called everytime a chunk of audio is routed thru the filter (this happens frequently, every ~20ms depending on the samplerate and platform). 
        //	The audio data is an array of floats ranging from [-1.0f;1.0f] and contains audio from the previous filter in the chain or the AudioClip on the AudioSource. 
        //	If this is the first filter in the chain and a clip isn't attached to the audio source this filter will be 'played'. 
        //	That way you can use the filter as the audio clip, procedurally generating audio.
        //
        //	If OnAudioFilterRead is implemented a VU meter will show up in the inspector showing the outgoing samples level. 
        //	The process time of the filter is also measured and the spent milliseconds will show up next to the VU Meter 
        //	(it turns red if the filter is taking up too much time, so the mixer will starv audio data). 
        //	Also note, that OnAudioFilterRead is called on a different thread from the main thread (namely the audio thread) 
        //	so calling into many Unity functions from this function is not allowed ( a warning will show up ). 	
        private void OnAudioFilterRead(float[] data, int channels)
    {
        //This uses the Unity specific float method we added to get the buffer
        midiStreamSynthesizerDelayed.GetNext(sampleBuffer);
        midiStreamSynthesizer.GetNext(sampleBuffer);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = sampleBuffer[i] * gain;
        }
    }

    public void MidiNoteOnHandler(int channel, int note, int velocity)
    {
        Debug.Log("NoteOn: " + note.ToString() + " Velocity: " + velocity.ToString());
    }

    public void MidiNoteOffHandler(int channel, int note)
    {
        Debug.Log("NoteOff: " + note.ToString());
    }
}
