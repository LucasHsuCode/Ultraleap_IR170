using UnityEngine;
using System;
using LeapInternal;
using System.Runtime.InteropServices;

public class LeapCameraParameters : MonoBehaviour
{
    private eLeapRS result;
    private IntPtr hConnection;
    LEAP_DEVICE_EVENT deviceEvent;
    private IntPtr device;
    private uint timeout = 150;

    private LEAP_DEVICE_REF deviceRef = new LEAP_DEVICE_REF();
    LEAP_CONNECTION_MESSAGE message = new LEAP_CONNECTION_MESSAGE();
    private float[] leftCameraIntrinsic;
    private float[] leftCameraExtrinsic;



    [DllImport("LeapC", EntryPoint = "LeapCameraMatrix")]
    public static extern eLeapRS LeapCameraMatrix(IntPtr hConnection, eLeapPerspectiveType camera, float[] dest);

    [DllImport("LeapC", EntryPoint = "LeapExtrinsicCameraMatrix")]
    public static extern eLeapRS LeapExtrinsicCameraMatrix(IntPtr hConnection, eLeapPerspectiveType camera, float[] dest);


    LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG
    {
        size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_CONFIG)),    
        flags = 0,
        server_namespace = IntPtr.Zero
    };

    void Start()
    {
        // Create connection
        result = LeapC.CreateConnection(ref config, out hConnection);
        result = LeapC.OpenConnection(hConnection);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.Log("result: " + result +",  Connection Error!!");
        }
        leftCameraIntrinsic = new float[9];
        leftCameraExtrinsic = new float[16];

    }

    void Update()
    {
        // Poll connection
        result = LeapC.PollConnection(hConnection, timeout, ref message);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.LogWarning("Failed to poll connection: " + result);
            return;
        }
        eLeapRS matrixResult = LeapExtrinsicCameraMatrix(hConnection, eLeapPerspectiveType.eLeapPerspectiveType_stereo_left, leftCameraExtrinsic);
        Debug.Log("LeapCameraMatrix result: " + matrixResult);
        Debug.Log("Left Camera Intrinsic Parameters: " + string.Join(", ", leftCameraExtrinsic));
        switch (message.type)
            {
                case eLeapEventType.eLeapEventType_Device:
                    deviceEvent = (LEAP_DEVICE_EVENT)Marshal.PtrToStructure(message.eventStructPtr, typeof(LEAP_DEVICE_EVENT));
                    deviceRef = deviceEvent.device;
                    eLeapRS deviceresult = LeapC.OpenDevice(deviceRef, out device);         
                    break;                
                default:
                    break;
            }     
        //     // Get left camera extrinsic parameters
        //     float[] leftCameraExtrinsic = new float[16];
        //     IntPtr leftCameraExtrinsicPtr = Marshal.AllocHGlobal(sizeof(float) * 16);
        //     LeapExtrinsicCameraMatrix(hConnection, eLeapPerspectiveType.eLeapPerspectiveType_stereo_left, leftCameraExtrinsic);
        //     Marshal.Copy(leftCameraExtrinsicPtr, leftCameraExtrinsic, 0, 16);
        //     Marshal.FreeHGlobal(leftCameraExtrinsicPtr);
        //     Debug.Log("Left Camera Extrinsic Parameters: " + string.Join(", ", leftCameraExtrinsic));
        // }
        // else
        // {
        //     Debug.LogWarning("No Leap Motion device connected.");
        // }
    }
}
