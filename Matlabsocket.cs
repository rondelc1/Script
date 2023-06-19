using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
using System.Threading;



public class MatLabSocket : MonoBehaviour
{
    // Use this for initialization
    internal Boolean socketReady = false;
    TcpClient mySocket;
    NetworkStream theStream;
    StreamWriter theWriter;
    StreamReader theReader;

    ManualResetEvent connectDone;

    public String Host = "localhost";
    //String Host = "10.211.55.3"; //BT Parralels PC
    Int32 Port = 55000;

    Thread socketThread;

    bool messageToSendFlag;
    string messageToSend;
    bool closeSocketFlag;
    bool messageReadFromSocketFlag;

    void Start()
    {
        closeSocketFlag = false;
        messageToSendFlag = true;
        messageReadFromSocketFlag = false;
        messageToSend = TestData.createTestData();

        socketThread = new Thread(messageThread);
        socketThread.Start();

        InvokeRepeating("sendTestMessage", 1f, 1f);

    }

    private void Update()
    {
        if (messageReadFromSocketFlag)
        {
            // send up alert to process signal
            Debug.Log("Process message from server here");
            messageReadFromSocketFlag = false;
        }
    }

    private void messageThread()
    {
        openSocket();


        while (closeSocketFlag == false)
        {
            // send any messages
            if (messageToSendFlag)
            {
                sendMessage(messageToSend);
            }
            // read any messages
            readAvailableMessages();
            Thread.Sleep(1500);

        }
             
        if (socketReady)
        {
            closeSocket();
        }


    }

    private void OnApplicationQuit()
    {
        if (socketReady)
        {
            closeSocket();
        }
        socketThread.Abort();
    }

   
    private string readNextMessage ()
    {
        string message = "";
        int p = theReader.Peek();
        while (p != 4) // End of transmission
        {
            char c = (char)theReader.Read();
            message += c;
            p = theReader.Peek();
        }
        int dispose4 = theReader.Read();
        // will have ended at 4
        // not this does not allow for multiple messages / multiple frames worth of messages or broken messages
        Debug.Log("Received: " + message + " (ended with code " + p + ")");
        return message;
    }

    public void readAvailableMessages ()
    {

        if (theStream.CanRead)
        {
            try
            {
                
                int p = theReader.Peek();
                if (p == -1)
                {
                    Debug.Log("Tried to read, got a -1)");
                    theReader.DiscardBufferedData();
                }

                bool startedReadingMessage = false;

                while (p == 2)
                {
                    startedReadingMessage = true;
                    // get rid of the start of transmission message
                    int dispose2 = theReader.Read();
                    readNextMessage();
                    p = theReader.Peek();

                }
                if (startedReadingMessage)
                {
                    messageReadFromSocketFlag = true;
                }
                // will have ended with next char == -1 (no more to read) or ?

            }
            catch (Exception e)
            {
                Debug.Log("Socket error: " + e);
            }
        }
        else
        {
            Debug.Log("Cannot read");
        }
    }

    public void sendTestMessage ()
    {
        messageToSend = TestData.createTestData();
        messageToSendFlag = true;

    }

    private void checkAvailableMessages ()
    {
        if (messageReadFromSocketFlag)
        {
            Debug.Log("Message ready to process");
            messageReadFromSocketFlag = false;
        }
    }

    public void sendMessage(string messageString)
    {
        if (theStream.CanWrite)
        {
            try
            {
                theWriter.WriteLine(messageString);
                messageToSendFlag = false;

                Debug.Log("Sent: " + messageString);

                theWriter.Flush();
            }
            catch (Exception e)
            {
                Debug.Log("Socket error: " + e);
            }
        }
        else
        {
            Debug.Log("Cannot write to socket");
        }

    }

    public void openSocket()
    {
        try
        {
            mySocket = new TcpClient(Host, Port);

            theStream = mySocket.GetStream();
            theWriter = new StreamWriter(theStream);
            theReader = new StreamReader(theStream);

            socketReady = true;
            Debug.Log("Opened Socket");

        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e);
        }
    }

    public void closeSocket ()
    {
        socketReady = false;
        char escape = (char)27;
        try
        {
            theWriter.Write(escape);
            theWriter.Close();
            mySocket.Close();

            Debug.Log("Closed socket");
        }
        catch (Exception e)
        {
            Debug.Log("Socket error: " + e);
        }
    }



}
