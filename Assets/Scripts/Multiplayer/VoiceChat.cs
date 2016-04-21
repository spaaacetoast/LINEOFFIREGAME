using UnityEngine;
using System.Collections;
using TNet;
using AngryRain;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : TNBehaviour
{
    private ClientPlayer mPlayer;

    public bool canRecord;
    public bool isRecording;

    public int recordFrequency = 44100;

    int minimumFrequency;
    int maximumFrequency;

    AudioSource targetAudioSource;
    AudioClip voiceData;

    float updateTime;

    public void Initialize(ClientPlayer owner)
    {
        voiceData = AudioClip.Create("VoiceData", recordFrequency, 1, recordFrequency, false); 

        targetAudioSource = GetComponent<AudioSource>();
        targetAudioSource.clip = voiceData;

        /*Microphone.GetDeviceCaps(null, out minimumFrequency, out maximumFrequency);
        if (minimumFrequency != 0 && maximumFrequency != 0)*/
            canRecord = true;
    }

    void Update()
    {
        if(canRecord)
        {
            bool curInput = Input.GetKey(KeyCode.K);
            if (curInput != isRecording)
            {
                isRecording = curInput;

                if(isRecording)
                {
                    voiceData = Microphone.Start(null, true, 1, recordFrequency);
                    updateTime = Time.time;
                }
                /*else
                {
                    Microphone.End(null);
                }*/
            }

            if (!isRecording && Time.time > updateTime + 1 && Microphone.IsRecording(null))
            {
                Microphone.End(null);
            }

            if (Microphone.IsRecording(null) && Time.time >= updateTime)
            {
                updateTime = Time.time + 1;
                float[] samples = new float[voiceData.samples * voiceData.channels];
                voiceData.GetData(samples, 0);
                tno.SendQuickly(60, Target.Others, isRecording, samples);
            }
        }
    }

    [RFC(60)]
    public void GetMicData(bool isRecording, float[] samples)
    {
        this.isRecording = isRecording;

        voiceData.SetData(samples, 0);
        targetAudioSource.Play();
    }

    void OnGUI()
    {
        if(isRecording && canRecord)
        {
            GUI.Box(new Rect(25, 25, 200, 50), "RECORDING!!");
        }
    }

    /*public int pos = 0;
    public int lastSample = 0;
    int diff = 0;
    public int FREQUENCY = 30000;
    public bool recordPressed = false;
    int minFreq;
    int maxFreq;
    AudioSource ChatAudioSource;
    AudioClip tempClip; //this clip stores the realtime data from microphone
    public bool isChatting = false;
    float lastSendTime;

    void Start()
    {
        Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
        if (minFreq == 0 && maxFreq == 0)
        {
            maxFreq = FREQUENCY;
        }

        tempClip = new 
    }

    void OnGUI()
    {
        /*if (GUI.Button(new Rect(10, 10, 100, 20), "start chatting"))
        {
            isChatting = true;
            if (!recordPressed)
            {
                recordPressed = !recordPressed;
                if (TNManager.isHosting)
                {
                    tempClip = Microphone.Start(null, true, 50, maxFreq);
                }
                else
                {
                    tempClip = AudioClip.Create("ClientTest", FREQUENCY, 1, FREQUENCY, false, false);
                    audio.clip = tempClip;
                    audio.Play();
                }
            }
        }

        if (GUI.Button(new Rect(10, 40, 100, 20), "stop chatting"))
        {
            isChatting = false;
            if (recordPressed)
            {
                recordPressed = !recordPressed;
                Microphone.End(null);
            }
        }

        if (GUI.Button(new Rect(10, 70, 100, 20), "play"))
        {
            audio.PlayOneShot(tempClip, 1.0f);
        }*//*

 if(isChatting)
 {
     GUI.Box(new Rect(25, 25, 250, 50), "RECORDING");
 }
}

void Update()
{
 isChatting = Input.GetKey(KeyCode.K);

 if (isChatting && Time.time > lastSendTime)
 {
     lastSendTime = Time.time + 2.5f;

     pos = Microphone.GetPosition(null);
     if (pos < lastSample)
     {
         lastSample = 0;
     }
     diff = pos - lastSample;
     float[] samples = new float[diff * tempClip.channels];
     tempClip.GetData(samples, lastSample);
     //stream.Write<int>(samples.Length);
     //stream.Write<float[]>(samples);
     tno.SendQuickly(1, Target.Others, samples);
     lastSample = pos;
 }
}

[RFC(1)]
void GetMicData(float[] samples)
{
 tempClip.SetData(samples, 0);
 audio.PlayOneShot(tempClip, 1.0f);
}*/
}
