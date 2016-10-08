using UnityEngine;
using System;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(WifiCommunication))]
public class ArduinoWifiCommunication : MonoBehaviour
{
    WifiCommunication wifiCommunication;
    //public IMUMessageFormat iMUMessage { get; private set; }
    IMUMessageFormat iMUMessage;
    byte[] emptyHeader;

    // Use this for initialization
    void Start()
    {
        wifiCommunication = GetComponent<WifiCommunication>();
        iMUMessage = new IMUMessageFormat();
        iMUMessage.Initialise();
        emptyHeader = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        wifiCommunication.OnUpdate += DataUpdated;
    }


    public void DataUpdated(byte[] data)
    {
        if (HasHeader(data, this.iMUMessage))
        {
            //Debug.Log("Correctly formatted message received: " + System.BitConverter.ToString(data));
            ParseReceivedValues(iMUMessage, data);
        }
    }

    private void ParseReceivedValues(IMUMessageFormat iMUMessage, byte[] circularBufferBytes)
    {
        ParseTimestamp(circularBufferBytes);
        if (iMUMessage.quaternion != null)
        {
            ParseQuaternion(circularBufferBytes);
        }
        if (iMUMessage.linearAcceleration != null)
        {
            ParseLinearAcceleration(circularBufferBytes);
        }

        ParseOn(circularBufferBytes);
        Debug.Log(iMUMessage.timestamp + ": " + iMUMessage.quaternion + " ; " + iMUMessage.linearAcceleration + " ; on: " + ((iMUMessage.on) ? "yes" : "no"));
        this.transform.rotation = iMUMessage.quaternion;
    }

    private void ParseOn(byte[] circularBufferBytes)
    {
        bool on = System.BitConverter.ToBoolean(circularBufferBytes, 34);
        if (on)
            iMUMessage.on = true;
        else
            iMUMessage.on = false;
    }

    private void ParseTimestamp(byte[] circularBufferBytes)
    {
        Debug.Log("circularBufferBytes: " + System.BitConverter.ToString(circularBufferBytes));
        UInt32 timestamp = System.BitConverter.ToUInt32(circularBufferBytes, 2);
        if (timestamp == 0)
            iMUMessage.timestamp = timestamp;
        else
        {
            if (timestamp > iMUMessage.timestamp && timestamp < UInt32.MaxValue)
                iMUMessage.timestamp = timestamp;
        }
    }

    int lastTimeStamp = 0;
    private void ParseQuaternion(byte[] circularBufferBytes)
    {
        float x = System.BitConverter.ToSingle(circularBufferBytes, 6);
        float y = System.BitConverter.ToSingle(circularBufferBytes, 10);
        float z = System.BitConverter.ToSingle(circularBufferBytes, 14);
        float w = System.BitConverter.ToSingle(circularBufferBytes, 18);
        //Debug.Log(DateTime.Now.Millisecond + " Quaternion "+x+","+y+","+z+","+w);

        int current = DateTime.Now.Millisecond;
        if (current > lastTimeStamp)
            Debug.Log("Elapsed " + (current - lastTimeStamp) + " ms");
        else
            Debug.Log("Elapsed " + (current - lastTimeStamp + 1000) + " ms");
        lastTimeStamp = current;

        Quaternion quaternion = new Quaternion(x, y, z, w);
        if (IsValidQuaternionValue(x) && IsValidQuaternionValue(y) && IsValidQuaternionValue(z) && IsValidQuaternionValue(w))
        {
            iMUMessage.quaternion.x = x;
            iMUMessage.quaternion.y = y;
            iMUMessage.quaternion.z = z;
            iMUMessage.quaternion.w = w;
        }
    }

    private void ParseLinearAcceleration(byte[] circularBufferBytes)
    {
        float x = System.BitConverter.ToSingle(circularBufferBytes, 22);
        float y = System.BitConverter.ToSingle(circularBufferBytes, 26);
        float z = System.BitConverter.ToSingle(circularBufferBytes, 30);
        if (IsValidLinearAccelerationValue(x))
            iMUMessage.linearAcceleration.x = x;
        if (IsValidLinearAccelerationValue(y))
            iMUMessage.linearAcceleration.y = y;
        if (IsValidLinearAccelerationValue(z))
            iMUMessage.linearAcceleration.z = z;
    }

    bool IsValidQuaternionValue(float value)
    {
        if (float.IsNaN(value))
            return false;
        if (Mathf.Abs(value) >= 1.0f)
            return false;

        return true;
    }

    bool IsValidLinearAccelerationValue(float value)
    {
        if (float.IsNaN(value))
            return false;
        if (Mathf.Abs(value) > 100f)
            return false;

        return true;
    }

    private bool HasHeader(byte[] circularBuffer, IMUMessageFormat iMUMessage)
    {
        if (circularBuffer != null && iMUMessage.header != null && circularBuffer.Length > iMUMessage.header.Length && iMUMessage.header.Length > 0)
        {
            byte[] circularBufferHeader = new byte[iMUMessage.header.Length];
            Array.Copy(circularBuffer, 0, circularBufferHeader, 0, iMUMessage.header.Length);
            if (iMUMessage.header.SequenceEqual(this.emptyHeader))
                return true;
            else
            {
                //Debug.Log("iMUMessage.header: " + System.BitConverter.ToString(iMUMessage.header));
                //Debug.Log("circularBufferHeader: " + System.BitConverter.ToString(circularBufferHeader));
                if (circularBufferHeader.SequenceEqual(iMUMessage.header))
                    return true;
                else
                    return false;
            }
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {

    }
}

/// <summary>
/// The format of the 37 byte packet received from the ESP8266 (& IMU) is, in order, as follows:
/// (0xFFFFFFFF) 4 bytes - header
/// 4 bytes (UInt32) - timestamp (microseconds) time elapsed since ESP8266 TCP socket was connected
/// 4 bytes (float) - normalised (i.e unit quaterion, magnitude == 1.0) quaternion x value
/// 4 bytes (float) - normalised (unit) quaternion y value
/// 4 bytes (float) - normalised (unit) quaternion z value
/// 4 bytes (float) - normalised (unit) quaternion w value
/// 4 bytes (float) - linear acceleration value x value with component due to gravity removed (e.g 0 at rest)
/// 4 bytes (float) - linear acceleration value y value with component due to gravity removed (e.g 0 at rest)
/// 4 bytes (float) - linear acceleration value z value with component due to gravity removed (e.g 0 at rest)
/// 1 byte (0x01 on 0x00 off) - whether gun trigger is pressed or not. No hardware or software debouncing included
/// </summary>
public class IMUMessageFormat
{
    public byte[] header; // If no header, header = 0x00000000
    public byte[] footer; // If no footer, footer = 0x00000000
    public Quaternion quaternion;
    public Vector3 linearAcceleration;
    public UInt32 timestamp;
    public bool on;

    public void Initialise()
    {
        header = new byte[] { 0xFF, 0xFF };
        footer = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        quaternion = Quaternion.identity;
        linearAcceleration = Vector3.zero;
        timestamp = 0;
        on = false;
    }
}