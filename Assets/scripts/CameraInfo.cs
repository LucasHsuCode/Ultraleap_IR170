using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Leap;
using Leap.Unity;
using LeapInternal;

public class CameraInfo : MonoBehaviour
{
    private LEAP_DEVICE_INFO _info;

    private IntPtr _leapConnection;
    private bool _connecting = false;


    LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG
    {
        size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_CONFIG)),    
        flags = 0,
        server_namespace = IntPtr.Zero
    };

    private void Awake()
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


        LeapServiceProvider provider = FindObjectOfType<LeapServiceProvider>();
        if (provider != null)
        {
            Controller controller = provider.GetLeapController();
            DeviceList deviceList = controller.Devices;

            if (deviceList != null && !deviceList.IsEmpty)
            {
                foreach (Device device in deviceList.ActiveDevices)
                {
                    _info = new LEAP_DEVICE_INFO();
                    LeapC.GetDeviceInfo(device.Handle, ref _info);
                }
            }
            else
            {
                Debug.LogError("Failed to get Leap Motion device.");
            }
        }
        else
        {
            Debug.LogError("LeapServiceProvider not found in the scene.");
        }
    }

    private void Start()
    {
        if (_info.size != 0)
        {
            Debug.Log("Device size: " + _info.size);
            Debug.Log("Device status: " + _info.status);
            Debug.Log("Device capabilities: " + _info.caps);
            Debug.Log("Device type: " + _info.type);
            Debug.Log("Device baseline: " + _info.baseline);
            Debug.Log("Device serial_length: " + _info.serial_length);
            Debug.Log("Device serial: " + _info.serial);
            Debug.Log("Device horizontal FOV: " + _info.h_fov);
            Debug.Log("Device vertical FOV: " + _info.v_fov);
            Debug.Log("Device range: " + _info.range);
        }
        else
        {
            Debug.LogError("Failed to get device info.");
        }
    }
}
