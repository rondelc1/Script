using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System;
using System.Threading;
using System.Text;

public class ASyncSocket : MonoBehaviour
{
    Socket client;
    public string host = "localhost";
    public int port = 55000;

    bool socketReady = false;

    public bool verbose = false;
    public bool verboseSystemTime = false;
    System.DateTime sendTime;
    System.DateTime receiveTime;
    public float secondsBetweenTestSends = 1.0f;

    string testString;

    byte[] sendByteData;
    byte[] receivedByteData;
    public int bufferSize = 2048;
    StringBuilder stringBuilder;
    int numReceives = 0;

    //Thread socketThread;

    List<string> messagesReceived;
    bool messageReadyToProcess;

    //reference used to get the patients hand data
    private ObjectToTrack objToTrack;
    //reference to ghost hand script
    private ReplayPatientMovement replayPatientMovement;

    public ObjectToTrack dummyTrackerIfNoHeadset;
    public ReplayPatientMovement dummyReplayObject;


    // Start is called before the first frame update
    void Start()
    {
        objToTrack = GameObject.FindObjectOfType<ObjectToTrack>();

        if (objToTrack == null)
        {
            objToTrack = dummyTrackerIfNoHeadset;
        }

        replayPatientMovement = GameObject.FindObjectOfType<ReplayPatientMovement>();
        if (replayPatientMovement == null)
        {
            replayPatientMovement = dummyReplayObject;
        }

        socketReady = false;

        // do not need to do as a thread now
        //initCommsThread();

        messagesReceived = new List<string>();
        messageReadyToProcess = false;

        setUpSocket();

        receiveData();

        // change to call send cached data
        InvokeRepeating("SendCachedData", 0.0f, secondsBetweenTestSends);
    }

    // Update is called once per frame
    void Update()
    {

        processReceivedMessages();
    }

    // new function
    // sendCachedData

        // make sure socket is ready
        // rather than send test string, 
        // convert list of messages to json string
        // sendData with that
        // clear list

    void SendCachedData()
    {


        string data = objToTrack.GetAndClearMessageData();

        if (socketReady)
        {
            sendData(data);
        }

    }



    void sendTestData()
    {
        if (socketReady)
        {
            sendData(testString);
        }
    }

    void processReceivedMessages()
    {
        
        if (messageReadyToProcess)
        {
            foreach (string nextMesage in messagesReceived)
            {
                if (verbose)
                {
                    Debug.Log("Frame " + Time.frameCount + " delta " + Time.deltaTime + " realtime " + Time.realtimeSinceStartup + "|Processing message in update: " + nextMesage);

                }
                try
                {
                    replayPatientMovement.ReceiveMessageFromServer(nextMesage);

                }
                catch (System.Exception e)
                {
                    Debug.Log("messageFromJson Exception: " + e);
                }
            }
            messageReadyToProcess = false;
            messagesReceived.Clear();
        }
    }

    private void OnApplicationQuit()
    {
        // now received the close application, so close down the socket
        if (socketReady)
        {
            // checked by all async methods before trying to use the socket
            closeSocket();

        }
        // do not need to do as a thread now
        //socketThread.Abort();
    }

    /*
     * // do not need to do as a thread now
      
    void initCommsThread()
    {
        socketThread = new Thread(mainCommsMethod);
        Debug.Log("Starting comms thread");
        socketThread.Start();
    }

    void mainCommsMethod()
    {
        messagesReceived = new List<string>();
        messageReadyToProcess = false;

        setUpSocket();

        receiveData();
        while (true)
        {
            if (socketReady && !alreadySent)
            {
                alreadySent = true;
                sendData(testString);
            }
        }
       
    }
    */
    void setUpSocket()
    {
        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress address = IPAddress.Parse(host);
        IPEndPoint endPoint = new IPEndPoint(address, port);

        receivedByteData = new byte[bufferSize];
        sendByteData = new byte[bufferSize];
        stringBuilder = new StringBuilder();

        client.BeginConnect(endPoint, new AsyncCallback(connectedCallback), client);
        if (verbose)
        {
            Debug.Log("Asked to connect");
        }


    }

    void connectedCallback(IAsyncResult ar)
    {
        try
        {
            Socket returnedClient = (Socket)ar.AsyncState;
            returnedClient.EndConnect(ar);
            if (verbose)
            {
                Debug.Log("Connected to server");
            }

            socketReady = true;
        }
        catch (Exception e)
        {
            Debug.Log("Socket Error: " + e);
        }
    }

    void sendData (string data)
    {
        if (socketReady)
        {
            string dataWithEOL = data + (char)10;
            sendByteData = Encoding.ASCII.GetBytes(dataWithEOL);

            if (verboseSystemTime)
            {
                sendTime = System.DateTime.Now;
                Debug.Log("Sent: " + sendTime.Second + "-" + sendTime.Millisecond);
            }
            client.BeginSend(sendByteData, 0, sendByteData.Length, SocketFlags.None, new AsyncCallback(sendCallBack), client);
            if (verbose)
            {
                Debug.Log("Sending: " + data + ", original was " + data.Length + " long");
            }
        }
    }

    void sendCloseData()
    {
        char close = (char)27;
        string closeString = close.ToString();
        sendByteData = Encoding.ASCII.GetBytes(closeString);

        client.BeginSend(sendByteData, 0, sendByteData.Length, SocketFlags.None, new AsyncCallback(sendCallBack), client);
        if (verbose)
        {
            Debug.Log("Sending close message");
        }
    }

    void sendCallBack(IAsyncResult ar)
    {
        try
        {

            // may need to abort if socket not ready, but leaving in case we need to end send
            Socket returnedClient = (Socket)ar.AsyncState;
            int byteSent = returnedClient.EndSend(ar);
            if (verbose)
            {
                //Debug.Log("Sent worked without problem");
            }
        }
        catch (Exception e)
        {
            Debug.Log("Socket Error: " + e);
        }
    }

    void messageReceivedAsync(string message)
    {
        messagesReceived.Add(message);

        messageReadyToProcess = true;
    }

    void receiveData()
    {
        client.BeginReceive(receivedByteData, 0, bufferSize, 0, new AsyncCallback(receiveCallback), client);
        if (verbose)
        {
            Debug.Log("Listening");
        }
    }

    void receiveCallback(IAsyncResult ar)
    {
        try 
        {
            if (verboseSystemTime)
            {
                receiveTime = System.DateTime.Now;
                Debug.Log("Received: " + receiveTime.Second + "-" + receiveTime.Millisecond);
            }
            if (verbose)
            {
                Debug.Log("Got package " + numReceives + " from server");
            }
            numReceives++;
            Socket returnedClient = (Socket)ar.AsyncState;
            int bytesRead = returnedClient.EndReceive(ar);
            if (bytesRead > 0)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(receivedByteData, 0, bytesRead));
                int lastChar = (int)stringBuilder[stringBuilder.Length - 1];
                if (lastChar == 4)
                {
                    // this is a finished message
                    // get rid of first and last code
                    stringBuilder.Remove(0, 1);
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                    messageReceivedAsync(stringBuilder.ToString());                    

                    //sends string to replay patient movement class
                    //so it can access the transforms that the server sends back

                    stringBuilder.Clear();
                }
                /* otherwise we'll continue to build the message next callback
                 call again to pick up new messages
                 /*
                 
                if (socketReady)
                {
                    receiveData();
                }
            }
            else
            {// all data received

                if (socketReady)
                {
                    if (verbose)
                    {
                        Debug.Log("Received nothing, listening again as socket open");
                    }
                    receiveData();
                }
                else
                {
                    if (verbose)
                    {
                        Debug.Log("Received nothing, will not listen again as socket closed");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("Socket Error: " + e);
        }
    }

    void closeSocket()
    {
        // stop other messages being sent etc
        socketReady = false;
        // send close message
        sendCloseData();
        // this will disconnect the socket
        //client.BeginDisconnect(true, new AsyncCallback(closeCallBack), client);
    }


}
