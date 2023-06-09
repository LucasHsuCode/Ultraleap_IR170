using UnityEngine;
using System.IO;
using System;
using Leap;
using Leap.Unity;
using System.Threading;

public class handpose : MonoBehaviour
{
    public LeapServiceProvider leapServiceProvider;
    public SocketServer socketServer;

    private int frameCounter = 0;
    private string pngPath = "D:/Unity/Leap/LeapUnity/Assets/output";
    private string dataPath = "D:/Unity/Leap/LeapUnity/Assets/joint_data";

    private bool startProcessing = false;
    private int image_id = 0;

    private long adbReceivedTimestamp;
    private Thread dataThread;


    private void Start()
    {
        socketServer.OnMessageReceived += HandleMessageReceived;
    }

    void Update()
    {
        // Frame frame = leapServiceProvider.CurrentFrame;
        Debug.Log("AAAAAA");
        if (startProcessing){
            ProcessUltraleapData();
        }
    }

    private void OnDestroy()
    {
        socketServer.OnMessageReceived -= HandleMessageReceived;
        if (dataThread != null && dataThread.IsAlive)
        {
            dataThread.Abort();
        }
    }

    private void HandleMessageReceived(string message)
    {
        Debug.Log($"Message received from SocketServer; {message}");
        if (startProcessing)
        {
            Debug.Log("Stop Record Ultraleap Frame!");
            startProcessing = false;
        }
        else
        {
            Debug.Log("Start Record Ultraleap Frame!");
            startProcessing = true;
            Frame frame = leapServiceProvider.CurrentFrame;
            Debug.Log("tttttt");
            adbReceivedTimestamp = frame.Timestamp;
            // if (dataThread == null || !dataThread.IsAlive)
            // {
            //     dataThread = new Thread(ProcessUltraleapData);
            //     dataThread.Start();
            // }
            
        }
    }

    private void ProcessUltraleapData()
    {
        Debug.Log("Start Record Ultraleap Frame!");
        Frame frame = leapServiceProvider.CurrentFrame;
        Debug.Log("testttt");
        while (startProcessing)
        {
            string frameData = "Frame ID: " + image_id + "\n";
            image_id ++ ;

            foreach (Hand hand in frame.Hands)
            {
                frameData += "Hand ID: " + hand.Id + " | Is Left: " + hand.IsLeft + " | Palm Position: " + hand.PalmPosition + "\n";

                // Iterate through all fingers of the hand
                for (int i = 0; i < 5; i++)
                {
                    Finger finger = hand.Fingers[i];
                    frameData += "Finger Type: " + finger.Type + " | Length: " + finger.Length.ToString("F2") + "m | Width: " + finger.Width.ToString("F2") + "m\n";

                    // Iterate through all bones of the finger
                    for (int j = 0; j < 4; j++)
                    {
                        Bone bone = finger.Bone((Bone.BoneType)j);
                        Debug.Log("bone next!" +bone.PrevJoint);
                        frameData += "Bone Type: " + bone.Type + " | Start Poasition: " + bone.PrevJoint + " | End Position: " + bone.NextJoint + " | Direction: " + bone.Direction + "\n";
                    }
                }
            }

            long now = frame.Timestamp;
            long millisecondsSinceEpoch = now - adbReceivedTimestamp;
            if (frameCounter % 1 == 0)
            {
                frameData += "TimeStamp: " + millisecondsSinceEpoch + "\n";
                // StartCoroutine(CaptureFrameWithHands());
                File.WriteAllText(dataPath + "/frame_" + frameCounter.ToString("D8") + "_data.txt", frameData);
            }
            frameCounter++;
        
        }
    
    }
}
