using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using LeapInternal;
using UnityEngine.UI;

public class LeapImageCaptureUT : MonoBehaviour{
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
    public SocketServer socketServer;


    LEAP_CONNECTION_CONFIG config = new LEAP_CONNECTION_CONFIG
    {
        size = (uint)Marshal.SizeOf(typeof(LEAP_CONNECTION_CONFIG)),
        flags = 0,
        server_namespace = IntPtr.Zero
    };

    private void Start() 
    {   
        textureLeftImage = new Texture2D(1, 1, TextureFormat.R8, false);
        textureRightImage = new Texture2D(1, 1, TextureFormat.R8, false);
        result = LeapC.CreateConnection(ref config, out hConnection);
        result = LeapC.OpenConnection(hConnection);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.Log("result: " + result +",  Connection Error!!");
        }
        // Enable image policy flag
        policyFlags |= (ulong)eLeapPolicyFlag.eLeapPolicyFlag_Images;
        result = LeapC.SetPolicyFlags(hConnection, policyFlags, 0);
        if (result != eLeapRS.eLeapRS_Success)
        {
            Debug.LogError("Failed to set policy flag for images: " + result);
        }
        // Subscribe to the socket server's message received event
        socketServer.OnMessageReceived += HandleMessageReceived;
    }

    private void Update() 
    {
        // 初始化 _leapEvent
        _leapEvent = new LEAP_IMAGE_EVENT();

        // 確保 LeapC.PollConnection 有回傳新的影像事件
        do {
            result = LeapC.PollConnection(hConnection, timeout, ref _msg);
            if (result != eLeapRS.eLeapRS_Success) {
                // 沒有新的事件，所以跳過這次的 Update() 循環
                return;
            }
        } while (_msg.type != eLeapEventType.eLeapEventType_Image);

        switch (_msg.type)
            {
                case eLeapEventType.eLeapEventType_Image:
                    _leapEvent = (LEAP_IMAGE_EVENT)Marshal.PtrToStructure(_msg.eventStructPtr, typeof(LEAP_IMAGE_EVENT));
                    break;                
                default:
                    break;
            }        

        // Retrieve left and right images from the LEAP_IMAGE_EVENT
        leftImage = _leapEvent.leftImage;
        rightImage = _leapEvent.rightImage;

        Debug.Log($"Left Image Width: {leftImage.properties.width}, Height: {leftImage.properties.height}, BPP: {leftImage.properties.bpp}");
        Debug.Log($"Right Image Width: {rightImage.properties.width}, Height: {rightImage.properties.height}, BPP: {rightImage.properties.bpp}");

        // Convert the images to Texture2D
        textureLeftImage = GetTextureImage(leftImage, textureLeftImage);
        textureRightImage = GetTextureImage(rightImage, textureRightImage);

        // Save the left and right images as PNG files
        if (textureLeftImage != null)
        {
            // SaveTextureAsPNG(textureLeftImage, leftImagePath);
            rawImageLeft.texture = textureLeftImage;
            
        }

        if (textureRightImage != null)
        {
            // SaveTextureAsPNG(textureRightImage, rightImagePath);
            rawImageRight.texture = textureRightImage;
        }
    }

    private byte[] GetImageData(LEAP_IMAGE Image)
    {
        int width = (int)Image.properties.width;
        int height = (int)Image.properties.height;
        int offset = (int)Image.offset;
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

        return imageData;

    }

    /// <summary>
    /// Gets the texture image from a LEAP_IMAGE object.
    /// </summary>
    /// <param name="Image">The LEAP_IMAGE object containing the image data.</param>
    /// <returns>A Texture2D object representing the image, or null if the image data is not available.</returns>
    private Texture2D GetTextureImage(LEAP_IMAGE Image, Texture2D textureImage)
    {
        // 確保 Image.data 不為 IntPtr.Zero
        if (Image.data == IntPtr.Zero)
        {
            Debug.LogWarning("Image.data is IntPtr.Zero.");
            return null;
        }

        // Get image data
        byte[] imageData = GetImageData(Image);

        // If the image data is not empty, convert it to a Texture2D object
        if (imageData != null && imageData.Length > 0)
        {
            int width = (int)Image.properties.width;
            int height = (int)Image.properties.height;
            Texture2D textureimage = ConvertToTexture2D(imageData, width, height, textureImage);
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
    private Texture2D ConvertToTexture2D(byte[] imageData, int width, int height, Texture2D textureImage)
    {
        if (width != textureImage.width || height != textureImage.height)
        {
            textureImage.Resize(width, height, TextureFormat.R8, false);
            textureImage.Apply();
        }

        // Create a new Texture2D object with the specified dimensions and format
        // Texture2D texture = new Texture2D(width, height, TextureFormat.R8, false);
        // Load the raw image data into the Texture2D object
        textureImage.LoadRawTextureData(imageData);
        // Apply the texture to the Texture2D object to finalize its creation
        textureImage.Apply();
        return textureImage;
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
        }
    }

    /// <summary>
    /// Handles the message received event from the SocketServer and toggles the recording state of UltraLeap frames.
    /// </summary>
    /// <param name="message">The message received from the SocketServer.</param>
    private void HandleMessageReceived(string message)
    {
        Debug.Log($"Message received from SocketServer: {message}");

        // Check if leftImage and rightImage are valid
        if (leftImage.data != IntPtr.Zero && rightImage.data != IntPtr.Zero)
        {
            
            // Construct the file paths for the saved binary image data
            leftImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "image_data/left/left_image" + image_id + ".bin");
            rightImagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "image_data/right/right_image" + image_id + ".bin");

            // Save the left and right images as binary files
            SaveImageDataAsBinary(leftImage, leftImagePath);
            SaveImageDataAsBinary(rightImage, rightImagePath);

            // Increment image_id
            image_id++;

        }
    }

    /// <summary>
    /// Saves the raw image data from a LEAP_IMAGE as a binary file.
    /// </summary>
    /// <param name="Image">The LEAP_IMAGE containing the image data.</param>
    /// <param name="fileName">The filename for the saved binary file.</param>
    private void SaveImageDataAsBinary(LEAP_IMAGE Image, string fileName)
    {
        byte[] imageData = GetImageData(Image);

        // If the image data is not empty, save it to a binary file
        if (imageData != null && imageData.Length > 0)
        {
            try
            {
                System.IO.File.WriteAllBytes(fileName, imageData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing image data to file: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("No image data available.");
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
    }
}
