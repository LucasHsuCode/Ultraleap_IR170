using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

using TMPro;
using UnityEngine;
using LeapInternal;



public class LeapCMotionController : MonoBehaviour
{
    private Thread _leapThread;
    private IntPtr _leapConnection;
    public SocketServer socketServer;
    private LEAP_TRACKING_EVENT _leapEvent;
    LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();

    uint timeout = 150;
    private long testtp;
    private long inittp;
    private long currenttp;
    private int frame_id = 0;
    private bool _threading = false;
    private bool _connecting = false;
    private string dataPath = "D:/Unity/Leap/LeapUnity/Assets/joint_data";



    LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG
    {
        size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_CONFIG)),    
        flags = 0,
        server_namespace = IntPtr.Zero
    };

    /// <summary>
    /// Initializes the Leap Motion connection and opens it, then subscribes to the socket server message event.
    /// </summary>
    private void Start()
    {
        // Create a Leap Motion connection using the provided configuration
        eLeapRS result = LeapC.CreateConnection(ref config, out _leapConnection);
        if (result == eLeapRS.eLeapRS_Success)
        {
            Debug.Log("Connection created successfully.");
        }
        else
        {
            Debug.LogError("Failed to create connection. Error: " + result);
        }
        
        // Open the Leap Motion connection
        result = LeapC.OpenConnection(_leapConnection);
        if (result == eLeapRS.eLeapRS_Success)
        {
            Debug.Log("Connection opened successfully.");
            // Set the connecting flag to true
            _connecting = true;
        }
        else
        {
            Debug.LogError("Failed to open connection. Error: " + result);
        }
        
        // Subscribe to the socket server's message received event
        socketServer.OnMessageReceived += HandleMessageReceived;
    }

    void Update()
    {

    }

    /// <summary>
    /// Handles the message received event from the SocketServer and toggles the recording state of UltraLeap frames.
    /// </summary>
    /// <param name="message">The message received from the SocketServer.</param>
    private void HandleMessageReceived(string message)
    {
        Debug.Log($"Message received from SocketServer; {message}");

        // Toggle the recording state based on the current state
        if (_threading)
        {
            Debug.Log("Stop Record Ultraleap Frame!");
            _threading = false;
            frame_id = 0;

            // Stop the polling thread without closing the connection
            Stop();
        }
        else
        {
            Debug.Log("Start Record Ultraleap Frame!");
            _threading = true;

            // Start the polling thread without reopening the connection
            StartPollThread();
        }
    }

    /// <summary>
    /// Polls the Leap Motion connection for tracking events and processes them accordingly.
    /// </summary>
    /// <returns>Returns the result of the polling operation as an eLeapRS value.</returns>
    private eLeapRS PollConnectionWithResult()
    {
        // Poll the connection with the specified timeout
        eLeapRS result = LeapC.PollConnection(_leapConnection, timeout, ref _msg);

        // If polling was successful, process the received message
        if (result == eLeapRS.eLeapRS_Success)
        {
            switch (_msg.type)
            {
                case eLeapEventType.eLeapEventType_Tracking:
                    // Convert the event data to the appropriate structure type
                    _leapEvent = (LEAP_TRACKING_EVENT)Marshal.PtrToStructure(_msg.eventStructPtr, typeof(LEAP_TRACKING_EVENT));
                    break;
                
                // Add additional event types to handle here, if needed
                default:
                    break;
            }
        }
        // Return the result of the polling operation
        return result;
    }

    /// <summary>
    /// Polls the Leap Motion connection for tracking events in a separate thread, processing the frames.
    /// </summary>
    private void PollThread()
    {
        // Initialize the timestamp
        inittp = LeapC.GetNow();

        // Continuously poll the connection while the connection and thread are active
        while (_connecting && _threading)
        {
            Debug.Log("Start Thread !!");

            // Poll the connection and store the result
            eLeapRS result = PollConnectionWithResult();

            // If polling was successful, process the frame
            if (result == eLeapRS.eLeapRS_Success)
            {
                ProcessFrame(_leapEvent);
            }

            // Increment the frame counter
            frame_id += 1;
        }
    }

    /// <summary>
    /// Starts the polling thread if it's not already running.
    /// </summary>
    private void StartPollThread()
    {
        // If the thread is null or not alive, create and start a new thread for pollin
        if (_leapThread == null || !_leapThread.IsAlive)
        {
            _leapThread = new Thread(PollThread);
            _leapThread.Start();
        }
    }

    /// <summary>
    /// Stops the polling thread if it's currently running.
    /// </summary>
    public void Stop()
    {
        // If the thread is not null and is alive, abort the thread
        if (_leapThread != null && _leapThread.IsAlive)
        {
            _leapThread.Abort();
        }
    }

    /// <summary>
    /// Processes a single Leap Motion tracking event, extracting hand, finger, and bone data.
    /// Writes the extracted data to a file.
    /// </summary>
    /// <param name="leapEvent">The LEAP_TRACKING_EVENT object containing the tracking data.</param>
    private void ProcessFrame(LEAP_TRACKING_EVENT leapEvent)
    {

        // Get the current time difference between the initial time point and the current time
        currenttp = LeapC.GetNow() - inittp;

        // Initialize frame data string with frame ID and timestamp
        string frameData = "Frame ID: " + frame_id + "\n";
        frameData += "Timestamps: " + currenttp + "\n";

        // Iterate through hands in the tracking event
        for (int h = 0; h < leapEvent.nHands; h++)
        {
            // Get hand data and add it to the frame data string
            LEAP_HAND hand = (LEAP_HAND)Marshal.PtrToStructure(leapEvent.pHands + h * Marshal.SizeOf(typeof(LEAP_HAND)), typeof(LEAP_HAND));
            frameData += "Hand ID: " + hand.id + " | Hand Type: " + hand.type.ToString() + "\n";
        
            // Get finger data for each hand
            LEAP_DIGIT[] fingers = new LEAP_DIGIT[] { hand.thumb, hand.index, hand.middle, hand.ring, hand.pinky };

            // Iterate through fingers
            for (int f = 0; f < fingers.Length; f++){
                
                // Get finger data
                LEAP_DIGIT finger = fingers[f];
                
                // Get bone data for each finger
                LEAP_BONE[] bones = new LEAP_BONE[] { finger.metacarpal, finger.proximal, finger.intermediate, finger.distal};

                // Iterate through bones
                for (int b = 0; b < bones.Length; b++)
                {
                    // Get bone data and add it to the frame data string
                    LEAP_BONE bone = bones[b];
                    // Vector3 start = LeapToUnity(bone.prev_joint);
                    // Vector3 end = LeapToUnity(bone.next_joint);
                    LEAP_VECTOR start = bone.prev_joint;
                    LEAP_VECTOR end = bone.next_joint;
                    Vector3 prev = new Vector3(start.x, start.y, start.z);
                    Vector3 next = new Vector3(end.x, end.y, end.z); 
                    string boneName = GetBoneName(f, b);
                    frameData += "Bone Type: " + boneName + " | prev_joint: " + prev + " | next_joint: " + next + "\n";
                }
            }
        }
        // Write frame data to a file
        File.WriteAllText(dataPath + "/frame_" + frame_id.ToString("D8") + "_data.txt", frameData);
    }

    // private Vector3 LeapToUnity(LEAP_VECTOR leapVector)
    // {
    //     return new Vector3(leapVector.x * 0.001f, leapVector.y * 0.001f, -leapVector.z * 0.001f);
    // }

    private string GetBoneName(int fingerIndex, int boneIndex)
    {
        string[] fingerNames = { "Thumb", "Index", "Middle", "Ring", "Pinky" };
        string[] boneNames = { "Metacarpal", "Proximal", "Intermediate", "Distal" };

        return fingerNames[fingerIndex] + "_" + boneNames[boneIndex];
    }
}
