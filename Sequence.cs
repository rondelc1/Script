using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class Sequence : MonoBehaviour
{
    private SphereManager sphereManager;

    //prefabs
    public GameObject spiralPrefab;
    public GameObject prefabToPlace;
    public Transform headset;
    public Transform patientController;
    public GameObject trailRenderer;
    public Transform findSequencePos;

    //transforms of sequence positioner, which determines where the sequence should take place
    public Transform sequencePositioner;
    public Transform left;
    public Transform right;
    public Transform up;
    public Transform down;
    public Transform towards;
    public Transform away;

    //vectors that the sequence will actully use, because the sequence positioner moves..
    private Vector3 seqLeft;
    private Vector3 seqRight;
    private Vector3 seqUp;
    private Vector3 seqDown;
    private Vector3 seqTowards;
    private Vector3 seqAway;

    private bool sequencePosFound = false;
    public bool toggleSequenceManager = false;
    
    private Vector3 heading;
    public float armLength = 1f;

    private bool placeSpiralBool = true;
    private GameObject spiral;


    private TargetData currentTarget;
    private bool currentAlreadyTouched;
    private SessionData currentSessionData;
    private bool needToSetupSession = true;

    public Dictionary<string, TargetData> targetsDict;
    public Dictionary<string, string> targetLocationsHumanReadable;



    //determines the sphere to place in each task
    public enum SpherePlace
    {
        FirstSphere,
        SecondSphere
    };
    public SpherePlace sphereplacer;

    private bool spherePlaced = false;
    private GameObject sphere;
    private GameObject sphereOne;
    private GameObject sphereTwo;
    private int taskCounter = 0;
    private int taskLimit = 6;
    private bool taskComplete;


    public enum TaskState
    {
        TaskOne,
        TaskTwo,
        TaskThree,
        TaskFour,
        TaskFive,
        TaskSix,
        TaskComplete
    };







    public TaskState taskSelecter;

    // Start is called before the first frame update
    void Start()
    {
        sphereManager = FindObjectOfType<SphereManager>();
        sphereplacer = SpherePlace.SecondSphere;

        armLength = 1f;
        taskSelecter = TaskState.TaskOne;

        targetsDict = new Dictionary<string, TargetData>();
        targetLocationsHumanReadable = new Dictionary<string, string>();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(headset.position, SequenceTargetPosition(), Color.blue);

        //measure arm length
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetArmLength();
            sequencePositioner.position = SequenceTargetPosition();
        }

        //start/stop the sequence
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            toggleSequenceManager = !toggleSequenceManager;
            sequencePosFound = true;

        
        }

        //place/destroy a spiral for user to trace 
        //also enable/disable tracer to be seen by user
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TogglePlaceSpiral();
        }

        if (toggleSequenceManager == true)
        {
            SetSequencePositions();
            SequenceManager();
        }
        else
        {
            ResetTasks(TaskState.TaskOne, false);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            TaskStepForward();
        }
    }

      

    private void TogglePlaceSpiral()
    {
        if (placeSpiralBool == true)
        {
            PlaceSpiral();
            ToggleTrailVisabilityAndLength(0, 5f);
            placeSpiralBool = false;
        }
        else
        {
            if (spiral != null)
            {
                Destroy(spiral);
                ToggleTrailVisabilityAndLength(13, 2f);
            }
            placeSpiralBool = true;
        }
    }

    private void ToggleTrailVisabilityAndLength(int layer, float time)
    {
        trailRenderer.layer = layer;
        trailRenderer.GetComponent<TrailRenderer>().time = time;
    }

    private void PlaceSpiral()
    {
        Vector3 spriralPos = sequencePositioner.transform.position;
        spiral = Instantiate(spiralPrefab, spriralPos, Quaternion.identity);
        spiral.transform.LookAt(headset);
    }

    private Vector3 SequenceTargetPosition()
    {
        Vector3 target = new Vector3(findSequencePos.position.x, headset.position.y, findSequencePos.position.z);
        float dist = Vector3.Distance(headset.position, target);

        heading = target - headset.position;
        heading.Normalize();
        heading *= armLength;

        return headset.position + heading;
    }

    private void SetSequencePositions()
    {
        if (sequencePosFound == true)
        {
            seqLeft = left.position;
            seqRight = right.position;
            seqUp = up.position;
            seqDown = down.position;
            seqTowards = towards.position;
            seqAway = away.position;
            sequencePosFound = false;
        }
    }

    //aslo delete all spheres still around...
    private void ResetTasks(TaskState taskState, bool toggleStart)
    {
        sphereplacer = SpherePlace.FirstSphere;
        //toggleSphereToPlace = true;
        spherePlaced = false;
        taskSelecter = taskState;
        toggleSequenceManager = toggleStart;

        sphereManager.DestroyAllSpheres();

    }

    private void SetArmLength()
    {
        armLength = Vector3.Distance(headset.position, patientController.position);
    }

    private void taskSequenceStarted()
    {
        Debug.Log("Start of sequence");
        currentSessionData = new SessionData();
        currentSessionData.id = taskSelecter.ToString();
        currentSessionData.targets = new List<SessionDataTarget>();
        currentSessionData.tStart = Time.realtimeSinceStartup;
        currentSessionData.type = "Spheres";
    }

    private void taskSequenceCompleted()
    {
        Debug.Log("End of sequence - tell server now");
        currentSessionData.tEnd = Time.realtimeSinceStartup;

        ObjectToTrack objToTrack = GameObject.FindObjectOfType<ObjectToTrack>();
        objToTrack.RecordSessionMessage(currentSessionData);
    }

    // Add in method on task completed, which goes in StartSequence and Fast Forward
    // takes in TaskState, then creates a message type of TargetData
    // this needs to be set up when first created, then updated on first touch, then closed on complete time
    // when closed, send off to the server


 

    // add in method on final sequence, which runs through all the targets completed and indicates complete with final start and end time
    private void SequenceManager()
    {


        switch (taskSelecter)
        {
            case TaskState.TaskOne:
                Task(0.4f, seqLeft, seqRight);
                if (taskComplete == true)
                {
                    Debug.Log(taskSelecter + " is complete");
                    taskComplete = false;
                    taskSelecter = TaskState.TaskTwo;
                    // this is a full sequence, send off to server a session complete message
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskTwo:
                Task(0.4f, seqAway, seqTowards);
                if (taskComplete == true)
                {
                    Debug.Log(taskSelecter + " is complete");

                    taskComplete = false;
                    taskSelecter = TaskState.TaskThree;
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskThree:
                Task(0.08f, seqLeft, seqRight);
                if (taskComplete == true)
                {
                    taskComplete = false;
                    taskSelecter = TaskState.TaskFour;
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskFour:
                Task(0.08f, seqAway, seqTowards);
                if (taskComplete == true)
                {
                    taskComplete = false;
                    taskSelecter = TaskState.TaskFive;
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskFive:
                Task(0.02f, seqLeft, seqRight);
                if (taskComplete == true)
                {
                    taskComplete = false;
                    taskSelecter = TaskState.TaskSix;
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskSix:
                Task(0.02f, seqAway, seqTowards);
                if (taskComplete == true)
                {
                    taskComplete = false;
                    taskSelecter = TaskState.TaskComplete;
                    taskSequenceCompleted();
                }
                break;
            case TaskState.TaskComplete:
                ResetTasks(TaskState.TaskOne, false);

                break;
            default:
                break;
        }


    }
      public void TaskStepForward()
            {
                switch (taskSelecter)
                {
                    case TaskState.TaskOne:
                        ResetTasks(TaskState.TaskTwo, true);
                        break;
                    case TaskState.TaskTwo:
                        ResetTasks(TaskState.TaskThree, true);
                        break;
                    case TaskState.TaskThree:
                        ResetTasks(TaskState.TaskFour, true);
                        break;
                    case TaskState.TaskFour:
                        ResetTasks(TaskState.TaskFive, true);
                        break;
                    case TaskState.TaskFive:
                        ResetTasks(TaskState.TaskSix, true);
                        break;
                    case TaskState.TaskSix:
                        ResetTasks(TaskState.TaskComplete, false);
                        taskSequenceCompleted();
                        break;
                    case TaskState.TaskComplete:
                        taskSequenceCompleted();
                        break;
                    default:
                        break;
                }
            }

            public void TaskStepBackwards()
            {
                switch (taskSelecter)
                {
                    case TaskState.TaskOne:
                        ResetTasks(TaskState.TaskComplete, false);
                        break;
                    case TaskState.TaskTwo:
                        ResetTasks(TaskState.TaskOne, true);
                        break;
                    case TaskState.TaskThree:
                        ResetTasks(TaskState.TaskTwo, true);
                        break;
                    case TaskState.TaskFour:
                        ResetTasks(TaskState.TaskThree, true);
                        break;
                    case TaskState.TaskFive:
                        ResetTasks(TaskState.TaskFour, true);
                        break;
                    case TaskState.TaskSix:
                        ResetTasks(TaskState.TaskFive, true);
                        break;
                    case TaskState.TaskComplete:
                        break;
                    default:
                        break;
                }
            }

    


            public void Task(float size, Vector3 posOne, Vector3 posTwo)
            {
                if (needToSetupSession == true)
                {
                    taskSequenceStarted();
                    needToSetupSession = false;
                }
                switch (sphereplacer)
                {
                    case SpherePlace.FirstSphere:
                        if (spherePlaced == false)
                        {
                            PlaceSphere(size, posOne);

                            sphereOne = sphere;
                            spherePlaced = true;
                        }

                        if (sphereOne == null)
                        {
                            sphereplacer = SpherePlace.SecondSphere;
                            spherePlaced = false;
                            taskCounter++;
                        }
                        break;
                    case SpherePlace.SecondSphere:
                        if (spherePlaced == false)
                        {
                            PlaceSphere(size, posTwo);

                            sphereTwo = sphere;
                            spherePlaced = true;
                        }
                        if (sphereTwo == null)
                        {
                            sphereplacer = SpherePlace.FirstSphere;
                            spherePlaced = false;
                            taskCounter++;
                        }
                        break;
                    default:
                        break;
                }
                if (taskCounter >= taskLimit)
                {
                    taskComplete = true;
                    taskCounter = 0;
                    taskSequenceCompleted();
                    needToSetupSession = true;
                }
            }

            public void PlaceSphere(float size, Vector3 _pos)
            {
                StartCoroutine(DoPlace(size, _pos));
                sphereManager.sphereList.Add(sphere);


            }

            public void EndSequenceButton()
            {
                ResetTasks(TaskState.TaskOne, false);
            }

            private string randomID()
            {
                string st = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                string a = st[UnityEngine.Random.Range(0, st.Length)].ToString();
                string b = st[UnityEngine.Random.Range(0, st.Length)].ToString();
                string c = st[UnityEngine.Random.Range(0, st.Length)].ToString();
                string d = st[UnityEngine.Random.Range(0, st.Length)].ToString();
                return (a + b + c + d);
            }

            private void initTargetData(float size, Vector3 position, Quaternion rotation, Vector3 relativePosition)
            {
                string id = randomID();
                Debug.Log(id);
                currentTarget = new TargetData();
                currentTarget.id = id;
                currentTarget.type = "Sphere";
                currentTarget.size = size;
                currentTarget.pos = position;
                currentTarget.quat = rotation;
                currentTarget.tCreated = Time.realtimeSinceStartup;
                currentTarget.tFirstTouch = Time.realtimeSinceStartup + 0.5f;
                currentTarget.tCompleted = Time.realtimeSinceStartup + 4.5f;

                // also add ID into current session
                SessionDataTarget currentSessionDataTarget = new SessionDataTarget();
                currentSessionDataTarget.id = id;
                currentSessionData.targets.Add(currentSessionDataTarget);

                targetsDict.Add(id, currentTarget);
                targetLocationsHumanReadable.Add(id, getHumanReadableForVector(relativePosition));

                //set current already touched to false
                currentAlreadyTouched = false;
            }

            public void firstTouchedData()
            {
                if (currentAlreadyTouched == false)
                {
                    currentTarget.tFirstTouch = Time.realtimeSinceStartup;
                    currentAlreadyTouched = true;
                }

            }

            public void endTargetDataAndSend()
            {
                currentTarget.tCompleted = Time.realtimeSinceStartup;

                Debug.Log("Sending target data:" + JsonUtility.ToJson(currentTarget));
                ObjectToTrack objToTrack = GameObject.FindObjectOfType<ObjectToTrack>();
                objToTrack.RecordTargetMessage(currentTarget);
            }

            private string getHumanReadableForVector(Vector3 pos)
            {
                if (pos == seqAway)
                {
                    return "Far";
                }
                if (pos == seqTowards)
                {
                    return "Near";
                }
                if (pos == seqLeft)
                {
                    return "Left";
                }
                if (pos == seqRight)
                {
                    return "Right";
                }
                return "Unknown";
            }


            private IEnumerator DoPlace(float size, Vector3 spherePos)
            {

                sphere = GameObject.Instantiate<GameObject>(prefabToPlace);
                sphere.transform.position = spherePos;

                Vector3 initialScale = sphere.transform.localScale * size * 0.01f;
                Vector3 targetScale = sphere.transform.localScale * size;

                float startTime = Time.time;
                float overTime = 0.5f;
                float endTime = startTime + overTime;

                // this is absolutely not the place to do this, but the only time when all the data is available
                initTargetData(size, sphere.transform.position, sphere.transform.rotation, spherePos);



                while (Time.time < endTime)
                {
                    if (sphere != null)
                    {
                        sphere.transform.localScale = Vector3.Slerp(initialScale, targetScale, (Time.time - startTime) / overTime);
                    }
                    yield return null;
                }
            }
        }
