using UnityEngine;
using System.Collections;

using System;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

public class WifiCommunication : MonoBehaviour
{
    public delegate void NewDataHandler(byte[] data);
    public event NewDataHandler OnUpdate = null;

    public StreamSocket socket;
    bool connected = false;

    //string IP = "172.19.34.23";
    //string IP = "192.168.1.66";  

    // Connect to Cubic wifi "Post"  
    string IP = "18.111.21.182"; 
    int PORT = 12345;

    // Connect to local wifi "esp_ap", password
    //string IP = "192.168.4.1";
    //int PORT = 12345;


    int BUFFERLENGTHINBYTES = 37;
    public byte[] ReceivedByteBuffers { get; private set; }

    #region UNITY FUNCTIONS
    void Awake()
    {
        // Allocate memory for buffer
        ReceivedByteBuffers = new byte[BUFFERLENGTHINBYTES];
        socket = new StreamSocket();
        socket.Control.KeepAlive = true;
        socket.Control.NoDelay = true;
        StartCoroutine(ConnectWifi());
    }

    // Use this for initialization
    void Start()
    {
        InvokeRepeating("ReadFromWifi", 0, 0.001f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        connected = false;
        try
        {
            socket.Dispose();
        }
        catch (Exception e)
        {
            Debug.Log("WifiCommunication.OnDestroy(): " + e.Message);
        }
    }

    #endregion

    #region LOCAL FUNCTIONS


    private IEnumerator ConnectWifi()
    {
        // Wait 0.1 seconds
        yield return new WaitForSeconds(0.1f);
        Connect(IP, PORT);
    }

    DataReader reader;
    private async void Connect(string name, int portNumber)
    {
        try
        {
            // Connect to the server (in our case the listener we created in previous step).
            HostName hostName = new HostName(name);
            await socket.ConnectAsync(hostName, portNumber.ToString());
            reader = new DataReader(socket.InputStream);
            reader.InputStreamOptions = InputStreamOptions.Partial;
            connected = true;
            //Debug.Log(name + ":" + portNumber + "  CONNECTED!");
        }
        catch (Exception e)
        {
            // If this is an unknown status it means that the error is fatal and retry will likely fail.
            if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.Unknown)
            {
                throw;
            }
            Debug.Log("Connect failed with error: " + e.Message);
        }
    }


    private void ReadFromWifi()
    {
        OnRead();
    }


    /// <summary>
    /// Invoked once for each reading.
    /// </summary>
    private void OnRead()
    {
        if (connected)
        {
            try
            {
                // Following line has to switch to sync other causing error according to 
                //http://stackoverflow.com/questions/16110561/winrt-datareader-loadasync-exception-with-streamsocket-tcp
                //uint actualStringLength = await reader.LoadAsync((uint)BUFFERLENGTHINBYTES);
                IAsyncOperation<uint> taskLoad = reader.LoadAsync((uint)BUFFERLENGTHINBYTES);
                taskLoad.AsTask().Wait();
                uint actualStringLength = taskLoad.GetResults();
                reader.ReadBytes(ReceivedByteBuffers);
                //Debug.Log(System.BitConverter.ToUInt32(ReceivedByteBuffers, 4));
                if (OnUpdate != null)
                    OnUpdate(ReceivedByteBuffers);
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
                Debug.Log("Read stream failed with error: " + exception.Message);
            }
        }
    }
    #endregion
}
