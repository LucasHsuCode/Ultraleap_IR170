using System;
using System.Net;
using System.Net.Sockets;

using System.Text;
using UnityEngine;
using System.Threading.Tasks;


/// <summary>
/// A simple TCP socket server for Unity, listening for incoming messages from clients.
/// </summary>
public class SocketServer : MonoBehaviour
{
    private bool _isRunning;
    private TcpListener _server;
    [SerializeField] private int port = 8888;

    // Add MessageReceived Event
    public event Action<string> OnMessageReceived;

    /// <summary>
    /// Starts the socket server and begins listening for client connections.
    /// </summary>
    private async void Start()
    {
        IPAddress ipAddress = IPAddress.Any;
        _server = new TcpListener(ipAddress, port);
        _server.Start();
        Debug.Log($"Server is listening on {ipAddress}:{port}");
        _isRunning = true;
        await AcceptClientsAsync();
    }

    /// <summary>
    /// Asynchronously accepts incoming client connections and handles each client in a separate task.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task AcceptClientsAsync()
    {
        while (_isRunning)
        {
            try
            {
                TcpClient client = await _server.AcceptTcpClientAsync();
                Debug.Log("Client connected.");
                Task.Run(async () => await HandleClientAsync(client));
            }
            catch (ObjectDisposedException)
            {
                // The TcpListener has been disposed, which is expected when the application is quitting.
                // Ignore the exception and exit the loop.
                break;
            }

        }
    }

    /// <summary>
    /// Asynchronously handles a connected client by reading and processing incoming messages.
    /// </summary>
    /// <param name="client">The connected TcpClient.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    private async Task HandleClientAsync(TcpClient client)
    {
        // Using statement ensures the disposal of the NetworkStream when the method completes
        using (NetworkStream stream = client.GetStream())
        {
            // Create a buffer to store incoming data
            byte[] buffer = new byte[1024];

            // Read data from the client asynchronously
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            // Convert the received data to a string
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Debug.Log($"Received: {receivedMessage}");

            // Trigger the OnMessageReceived event
            OnMessageReceived?.Invoke(receivedMessage);

            // Create a response message and send it back to the client
            byte[] responseMessage = Encoding.ASCII.GetBytes($"You sent: {receivedMessage}");
            await stream.WriteAsync(responseMessage, 0, responseMessage.Length);
        }

        // Close the client connection
        client.Close();
    }

    /// <summary>
    /// Handles the Unity OnApplicationQuit event, stopping the server and setting _isRunning to false.
    /// </summary>
    private void OnApplicationQuit()
    {
        _isRunning = false;
        _server.Stop();
    }
}