using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using LeapInternal;
using UnityEngine.UI;

public class LeapImageCapture : MonoBehaviour{
    private eLeapRS result;
    private IntPtr hConnection;
    private LEAP_CONNECTION_MESSAGE _msg = new LEAP_CONNECTION_MESSAGE();
    private uint timeout = 150;
    private ulong policyFlags = 0;
    private LEAP_IMAGE_EVENT _leapEvent;
    private int image_id = 0;
    private string leftImagePath;
    private string rightImagePath;
    private LEAP_IMAGE leftImage;
    private LEAP_IMAGE rightImage;
    private Texture2D textureLeftImage;
    private Texture2D textureRightImage;
    public RawImage rawImageLeft;
    public RawImage rawImageRight;

    LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG
    {
        size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_CONFIG)),    
        flags = 0,
        server_namespace = IntPtr.Zero
    };

    private void Start() 
    {   
        result = LeapC.CreateConnection(ref config, out hConnection);
        result = LeapC.OpenConnection(hConnection);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.Log("result: " + result +",  Connection Error!!");
        }
    }

    private void Update() 
    {
        // RetrieveDeviceList(hConnection);
        StartCoroutine(ConnectImageEvent());
        if (textureLeftImage != null) rawImageLeft.texture = textureLeftImage;
        if (textureRightImage != null) rawImageRight.texture = textureRightImage;
    }
    
    /// <summary>
    /// ConnectImageEvent is a coroutine that attempts to retrieve images from the Leap Motion device.
    /// It sets the policy flags to allow images, polls the connection for events, and saves the images as PNG files when available.
    /// </summary>
    private IEnumerator ConnectImageEvent()
    {
        // Enable image policy flag
        policyFlags |= (ulong)eLeapPolicyFlag.eLeapPolicyFlag_Images;
        result = LeapC.SetPolicyFlags(hConnection, policyFlags, 0);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.LogError("Failed to set policy flag for images: " + result);
        }

        // Poll the connection for image events
        result = LeapC.PollConnection(hConnection, timeout, ref _msg);
        switch (_msg.type)
            {
                case eLeapEventType.eLeapEventType_Image:
                    _leapEvent = (LEAP_IMAGE_EVENT)Marshal.PtrToStructure(_msg.eventStructPtr, typeof(LEAP_IMAGE_EVENT));
                    break;                
                default:
                    break;
            }        
        
        // Wait for a short period to avoid overwhelming the system
        float waitTime = 0.1f; // 設定延遲時間（以秒為單位）
        yield return new WaitForSeconds(waitTime);

        // Retrieve left and right images from the LEAP_IMAGE_EVENT
        leftImage = _leapEvent.leftImage;
        rightImage = _leapEvent.rightImage;

        Debug.Log($"Left Image Width: {leftImage.properties.width}, Height: {leftImage.properties.height}, BPP: {leftImage.properties.bpp}");
        Debug.Log($"Right Image Width: {rightImage.properties.width}, Height: {rightImage.properties.height}, BPP: {rightImage.properties.bpp}");

        // Convert the images to Texture2D
        textureLeftImage = GetTextureImage(leftImage);
        textureRightImage = GetTextureImage(rightImage);

        // Construct the file paths for the saved PNG images
        leftImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "image_data/left/left_image"+image_id+".png");
        rightImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "image_data/right/right_image"+image_id+".png");

        // Save the left and right images as PNG files
        if (textureLeftImage != null)
        {
            SaveTextureAsPNG(textureLeftImage, leftImagePath);
            image_id++;
        }

        if (textureRightImage != null)
        {
            SaveTextureAsPNG(textureLeftImage, rightImagePath);
        }
    }

    /// <summary>
    /// Gets the texture image from a LEAP_IMAGE object.
    /// </summary>
    /// <param name="Image">The LEAP_IMAGE object containing the image data.</param>
    /// <returns>A Texture2D object representing the image, or null if the image data is not available.</returns>
    private Texture2D GetTextureImage(LEAP_IMAGE Image)
    {
        int width = (int)Image.properties.width;
        int height = (int)Image.properties.height;
        int offset = (int)Image.offset;
        Debug.Log("offset:" + offset);
        int bpp = (int)Image.properties.bpp; // Bytes per pixel
        byte[] imageData = new byte[width * height * bpp];

        // Copy image data from unmanaged memory to managed array
        try
        {
            Marshal.Copy(Image.data + offset, imageData, 0, imageData.Length);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error copying image data: {ex.Message}");
            return null;
        }
        
        // If the image data is not empty, convert it to a Texture2D object
        if (imageData.Length > 0)
        {
            Texture2D textureimage = ConvertToTexture2D(imageData, width, height);
            return textureimage;
        }
        else
        {
            Debug.LogWarning("No image data available.");
            return null;
        }
    }

    /// <summary>
    /// Converts raw image data to a Unity Texture2D object.
    /// </summary>
    /// <param name="imageData">The raw image data, as a one-dimensional byte array.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <returns>The converted Texture2D object.</returns>
    private Texture2D ConvertToTexture2D(byte[] imageData, int width, int height)
    {
        // Create a new Texture2D object with the specified dimensions and format
        Texture2D texture = new Texture2D(width, height, TextureFormat.R8, false);
        // Load the raw image data into the Texture2D object
        texture.LoadRawTextureData(imageData);
        // Apply the texture to the Texture2D object to finalize its creation
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Save the given Texture2D as a PNG file with the given filename.
    /// </summary>
    /// <param name="texture">The Texture2D to be saved as a PNG file.</param>
    /// <param name="fileName">The filename for the saved PNG file.</param>
    private void SaveTextureAsPNG(Texture2D texture, string fileName)
    {   
        // Encode the Texture2D as a PNG image
        byte[] pngData = texture.EncodeToPNG();

        // If the encoded PNG data is not null, write it to the specified file
        if (pngData != null)
        {
            System.IO.File.WriteAllBytes(fileName, pngData);
            // Debug.Log("Image_id: " + image_id);
        }
    }

    /// <summary>
    /// Retrieve a list of connected Leap Motion devices and print out their information.
    /// </summary>
    /// <param name="hConnection">The handle to the Leap Motion connection.</param>
    private void RetrieveDeviceList(IntPtr hConnection)
    {
        // Initialize variables to retrieve device list
        UInt32 deviceCount;
        LEAP_DEVICE_REF[] deviceArray = new LEAP_DEVICE_REF[1];

        // Poll the connection and get the device list
        eLeapRS result = LeapC.PollConnection(hConnection, timeout, ref _msg);
        result = LeapC.GetDeviceList(hConnection, deviceArray, out deviceCount);

        // Check if any devices are connected
        if (deviceCount > 0)
        {
            // Resize the device array to match the number of connected devices
            deviceArray = new LEAP_DEVICE_REF[deviceCount];
            result = LeapC.GetDeviceList(hConnection, deviceArray, out deviceCount);
            
            // Loop through each device in the device array
            for (int i = 0; i < deviceCount; i++)
            {
                // Get the device reference and print its handle and ID
                LEAP_DEVICE_REF deviceRef = deviceArray[i];
                Debug.Log("Device " + i + " Handle: " + deviceRef.handle + " ID: " + deviceRef.id);
                
                // Initialize a device info struct and retrieve the device's info
                LEAP_DEVICE_INFO deviceInfo = new LEAP_DEVICE_INFO();
                deviceInfo.size = (uint)Marshal.SizeOf(typeof(LEAP_DEVICE_INFO));
                eLeapRS deviceInfoResult = LeapC.GetDeviceInfo(deviceRef.handle, ref deviceInfo);

                // Print the device's baseline if successful, or an error message if not
                if (deviceInfoResult == eLeapRS.eLeapRS_Success)
                {
                    Debug.Log("Device " + i + " Baseline: " + deviceInfo.baseline);
                }
                else
                {
                    Debug.Log("Failed to get device info for Device " + i);
                }
            }
        }
    }
    private void OnDestroy() {
        // Release resources related to the Leap Motion connection
        LeapC.CloseConnection(hConnection);
        LeapC.CloseConnection(hConnection);
    }
}
