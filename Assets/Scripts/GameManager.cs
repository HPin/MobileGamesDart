using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/**
 * Contains all the game logic
 */
public class GameManager : MonoBehaviour {

    // used for callback for arrow:
    public static GameManager Instance
    {
        get;
        private set;
    }

    void Awake()
    {
        Instance = this;
    }
    
    // swipe gesture:
    Vector2 touchStart;
    float startTime;
    Vector2 touchEnd;
    float diffTime;
    float flickTime = 5;
    float flickLength = 0;
    float dartVelocity;
    float dartSpeed = 0.0f;
    Vector3 worldAngle;
    public float comfortZone;
    bool couldBeSwipe;
    public float shootForce = 50;

    // UI elements:
    public Text player1ScoreLabel;     // holds the score of player 1
    public Text player2ScoreLabel;     // holds the score of player 2
    public Text player1LegsLabel;     // holds the legs of player 1
    public Text player2LegsLabel;     // holds the legs of player 2
    public Text player1SetsLabel;     // holds the sets of player 1
    public Text player2SetsLabel;     // holds the sets of player 2
    public Text infoLabel;             // info bar on top (for score of current throw)
    public GameObject TurnPanelPlayer1;
    public GameObject TurnPanelPlayer2;
    public TextMeshProUGUI scoreIndicatorLabel;
    public TextMeshProUGUI playerChangeIndicatorLabel;

    // Score:
    private int pointsPerLeg;    // the start amount of points (e.g. 501)
    int player1Score;    // score of player 1
    int player1ScoreBeforeRound;    // score of player 1
    int player2Score;    // score of player 2
    int player2ScoreBeforeRound;    // score of player 1

    // sets and legs:
    int player1Legs = 0;    // legs of player 1
    int player1Sets = 0;    // sets of player 1
    int player2Legs = 0;    // legs of player 2
    int player2Sets = 0;    // sets of player 2

    State tempState;    // only used for testing!

    // bool flags:
    private bool useDoubleOut;   // if true players can only finish legs on a double field
    bool isPlayer1 = true;   // true if it is player1's turn, false if player2 throws
    private bool showScoreIndicator = true;    // true if current score should get displayed
    private bool showPlayerChangeScreen = false;  
    private bool arePlayersChanging = false;    // true if players are currently changing

    // indicates which throw of the three is currently in action
    enum ThrowNumber
    {
        THROW1 = 1,
        THROW2,
        THROW3
    };
    ThrowNumber throwNumber = ThrowNumber.THROW1;   // set throw 1 as the start

    public Vector3 defaultCameraPosition;       // the default position of the camera
    public Vector3 playerChangeCameraPosition;
    public Vector3 playerChangeCameraAngle;
    public Vector3 closeCameraDistance;  // sets the distance of the camera when zoomed in on a dart
    public Vector3 closeCameraAngle;     // sets the angle of the camera when zoomed in on a dart

    public GameObject dartPrefabOne;  // prefab of the dart for player one
    public GameObject dartPrefabTwo; // player 2 arrow
    GameObject currentDart;     // the currently thrown dart
    private Vector3 positionOfPrevDart;
    Queue<GameObject> dartQueue = new Queue<GameObject>();

    public ScoringValue scoringValue;   // holds the score

    // shows in which state the game currently is
    public enum State 
    { 
        THROWINGSTATE,      // standard camera: player can throw
        CLOSECAMERASTATE,    // close up camera of the landed dart
        PLAYERCHANGESTATE
    };    
    public float transitionTime;     // time between state changes
    public State state;

    private State currentState;               // current mode
    private State previousState;              // previous mode
    private float progressUnsmoothed;
    private float progress;

    // Use this for initialization
    void Start()
    {
        currentState = state;
        previousState = state;
        progress = 0;

        // fetch starting point amount from player prefs
        this.pointsPerLeg = PlayerPrefs.GetInt("StartPoints", 501);

        int useDoubleOutIntValue = PlayerPrefs.GetInt("UseDoubleOut", 1);
        Debug.Log(useDoubleOutIntValue);
        if (useDoubleOutIntValue == 1)
        {
            this.useDoubleOut = true;
        } else
        {
            this.useDoubleOut = false;
        }

        this.player1Score = this.pointsPerLeg;
        this.player1ScoreBeforeRound = this.pointsPerLeg;
        this.player2Score = this.pointsPerLeg;    
        this.player2ScoreBeforeRound = this.pointsPerLeg;

        this.scoreIndicatorLabel.enabled = false;
        this.playerChangeIndicatorLabel.enabled = false;

        tempState = state;
    }

    // Update is called once per frame
    void Update()
    {
        // display scores
        player1ScoreLabel.text = player1Score.ToString();
        player2ScoreLabel.text = player2Score.ToString();
        player1LegsLabel.text = player1Legs.ToString();
        player2LegsLabel.text = player2Legs.ToString();
        player1SetsLabel.text = player1Sets.ToString();
        player2SetsLabel.text = player2Sets.ToString();

        // check if state has changed
        if (currentState != previousState)
        {
            progressUnsmoothed += Time.deltaTime / transitionTime;
            if (progressUnsmoothed >= 1)
            {
                progressUnsmoothed = 0;
                previousState = currentState;
            }
            progress = smoothen(progressUnsmoothed);
        }
        else if (currentState != state)
        {
            currentState = state;
        }

        // update positions
        updateCameraAndDartPosition();

        if (tempState != state)
        {
            Debug.Log("New State: " + state);
            tempState = state;
        }

        // check user input and throw darts
        if (Input.touchCount > 0)
        {
            if (state == State.THROWINGSTATE && progress == 0)
            {
                // Debug.Log("throwingstate entered");

                this.infoLabel.text = "Swipe to throw.";

                this.playerChangeIndicatorLabel.enabled = false;    // make sure to dismiss player info label
                
                ThrowDart();
            }
            else if (state == State.CLOSECAMERASTATE && progress == 0)
            {
                // Debug.Log("closecamstate entered");

                if (this.arePlayersChanging)
                {
                    state = State.PLAYERCHANGESTATE;
                    this.showPlayerChangeScreen = true;

                    if (isPlayer1)
                    {
                        StartCoroutine(ShowPlayerChangeIndicator("Player 1"));
                    }
                    else
                    {
                        StartCoroutine(ShowPlayerChangeIndicator("Player 2"));
                    }

                    this.infoLabel.text = "Tap to continue.";
                } else
                {
                    state = State.THROWINGSTATE;
                }
                
                // if it is the first throw (i.e. the player has changed), remove the darts from the board
                if (throwNumber == ThrowNumber.THROW1)
                {
                    while (dartQueue.Count > 0)
                    {
                        GameObject d = dartQueue.Dequeue();
                        //d.GetComponentInChildren<Animation>().Play("Drop"); 
                        //d.GetComponentInChildren<Rigidbody>().isKinematic = false;
                        Destroy(d, 1f);
                    }
                }
            }
            else if (state == State.PLAYERCHANGESTATE && progress == 0)
            {
                // Debug.Log("playerstate entered");

                if (!this.showPlayerChangeScreen)
                {
                    state = State.THROWINGSTATE;
                }

                this.arePlayersChanging = false;
                this.showPlayerChangeScreen = false;

                // dismiss indicator
                this.playerChangeIndicatorLabel.CrossFadeAlpha(0.0f, 0.25f, false);
                this.playerChangeIndicatorLabel.enabled = false;
            }
        }
        
    }

    float smoothen(float t)
    {
        return t * t * (3f - 2f * t);
    }


    void updateCameraAndDartPosition()
    {
        Dictionary<State, posRot> positions = new Dictionary<State, posRot>();

        // resets camera position to original position
        positions[State.THROWINGSTATE] = new posRot(defaultCameraPosition, Quaternion.Euler(0, 0, 0));
        positions[State.PLAYERCHANGESTATE] = new posRot(playerChangeCameraPosition, Quaternion.Euler(playerChangeCameraAngle));

        if (currentDart != null)
        {
            // position dart on the board
            positions[State.CLOSECAMERASTATE] = new posRot(currentDart.transform.position + closeCameraDistance, Quaternion.Euler(closeCameraAngle));

            // store position for later
            this.positionOfPrevDart = positions[State.CLOSECAMERASTATE].pos;
        }
        else
        {
            positions[State.CLOSECAMERASTATE] = new posRot(this.positionOfPrevDart, Quaternion.Euler(closeCameraAngle));
            // hide dart from screen
            //positions[State.CLOSECAMERASTATE] = new posRot(Vector3.zero, Quaternion.Euler(Vector3.zero));
        }

        Vector3 finalPos;
        Quaternion finalRot;

        finalPos = Vector3.Lerp(positions[previousState].pos, positions[currentState].pos, progress);
        finalRot = Quaternion.Lerp(positions[previousState].rot, positions[currentState].rot, progress);

        transform.position = finalPos;
        transform.rotation = finalRot;
    }

    // stores the position and current rotation of a dart
    struct posRot
    {
        public Vector3 pos;
        public Quaternion rot;
        public posRot(Vector3 _pos, Quaternion _rot)
        {
            pos = _pos;
            rot = _rot;
        }
    }

    // stores the achievable points and the needed radius and angles for the points
    [System.Serializable]
    public struct ScoringValue
    {
        public Vector3 center;
        public float BE1xRadius;
        public float BE2xRadius;
        public float min3X;
        public float max3X;
        public float min2X;
        public float max2X;
        public ScoringAngles scoringAngles;
    }

    // stores all angles (from the center) for the transitions between point ranges
    [System.Serializable]
    public struct ScoringAngles
    {

        public float angle5_20;
        public float angle20_1;
        public float angle1_18;
        public float angle18_4;
        public float angle4_13;
        public float angle13_6;
        public float angle6_10;
        public float angle10_15;
        public float angle15_2;
        public float angle2_17;
        public float angle17_3;
        public float angle3_19;
        public float angle19_7;
        public float angle7_16;
        public float angle16_8;
        public float angle8_11;
        public float angle11_14;
        public float angle14_9;
        public float angle9_12;
        public float angle12_5;
    }

    // stores multipliers, base score and computes total points
    public struct Score
    {
        public enum ScoreMultiplier { x1 = 1, x2, x3 };
        public ScoreMultiplier scoreMultiplier;
        public int Number;
        public int Points { get { return Number * (int)scoreMultiplier; } }
    }

    // calculates the thrown score based on the position of the dart
    // measures the angle and distance from the center of the board
    // and returns the achieved score
    public void DecodeScore(Vector3 pos)
    {
        Score score = new Score();
        score.Number = -1;

        // get distance from the center of the board
        Vector2 offset = new Vector2((pos - scoringValue.center).x, (pos - scoringValue.center).y);

        // check if multipliers have been hit
        // bulls eye (= double single bull)
        if (offset.magnitude < scoringValue.BE2xRadius) 
        {
            score.Number = 25;
            score.scoreMultiplier = Score.ScoreMultiplier.x2;
            UpdateScore(score);
        }
        // single bull
        else if (offset.magnitude < scoringValue.BE1xRadius)    
        {
            score.Number = 25;
            score.scoreMultiplier = Score.ScoreMultiplier.x1;
            UpdateScore(score);
        }
        // triple field
        else if (offset.magnitude < scoringValue.max3X && offset.magnitude > scoringValue.min3X)
        {
            score.scoreMultiplier = Score.ScoreMultiplier.x3;
        }
        // double field
        else if (offset.magnitude < scoringValue.max2X && offset.magnitude > scoringValue.min2X)
        {
            score.scoreMultiplier = Score.ScoreMultiplier.x2;
        }
        // miss
        else if (offset.magnitude > scoringValue.max2X)
        {
            score.scoreMultiplier = Score.ScoreMultiplier.x1;
            score.Number = 0;
            UpdateScore(score);
        }
        // single field
        else
        {
            score.scoreMultiplier = Score.ScoreMultiplier.x1;
        }

        // get the angle of the dart measured from the center of the board
        float angle = Vector2.Angle(Vector2.up, offset.normalized);

        // safety check: negative distance from center
        if (offset.x < 0)
        {
            angle *= -1;
            angle += 360;
            angle %= 360;
        }

        // safety check: negative angle
        if (angle < 0)
        {
            Debug.LogError("Warning - Negative angle: " + angle);
        }


        if (score.Number != 0 && score.Number != 25 && score.Number != 50)
        {
            // check all possibilities and assign the correct score
            if (angle < scoringValue.scoringAngles.angle20_1 || angle >= scoringValue.scoringAngles.angle5_20)
            {
                score.Number = 20;
            }
            else if (angle < scoringValue.scoringAngles.angle1_18)
            {
                score.Number = 1;
            }
            else if (angle < scoringValue.scoringAngles.angle18_4)
            {
                score.Number = 18;
            }
            else if (angle < scoringValue.scoringAngles.angle4_13)
            {
                score.Number = 4;
            }
            else if (angle < scoringValue.scoringAngles.angle13_6)
            {
                score.Number = 13;
            }
            else if (angle < scoringValue.scoringAngles.angle6_10)
            {
                score.Number = 6;
            }
            else if (angle < scoringValue.scoringAngles.angle10_15)
            {
                score.Number = 10;
            }
            else if (angle < scoringValue.scoringAngles.angle15_2)
            {
                score.Number = 15;
            }
            else if (angle < scoringValue.scoringAngles.angle2_17)
            {
                score.Number = 2;
            }
            else if (angle < scoringValue.scoringAngles.angle17_3)
            {
                score.Number = 17;
            }
            else if (angle < scoringValue.scoringAngles.angle3_19)
            {
                score.Number = 3;
            }
            else if (angle < scoringValue.scoringAngles.angle19_7)
            {
                score.Number = 19;
            }
            else if (angle < scoringValue.scoringAngles.angle7_16)
            {
                score.Number = 7;
            }
            else if (angle < scoringValue.scoringAngles.angle16_8)
            {
                score.Number = 16;
            }
            else if (angle < scoringValue.scoringAngles.angle8_11)
            {
                score.Number = 8;
            }
            else if (angle < scoringValue.scoringAngles.angle11_14)
            {
                score.Number = 11;
            }
            else if (angle < scoringValue.scoringAngles.angle14_9)
            {
                score.Number = 14;
            }
            else if (angle < scoringValue.scoringAngles.angle9_12)
            {
                score.Number = 9;
            }
            else if (angle < scoringValue.scoringAngles.angle12_5)
            {
                score.Number = 12;
            }
            else
            {
                score.Number = 5;
            }

            UpdateScore(score);
        }

    }

    private void UpdateScore(Score score)
    {
        if (this.showScoreIndicator)
        {
            StartCoroutine(ShowScoreIndicator(score.Points.ToString()));
        }
        //Debug.Log("currentscore: " + score.Number);

        //Debug.Log("called updatescore with: " + throwNumber);

        if (isPlayer1)
        {
            infoLabel.text = "Player one";
            var tempScore = player1Score - score.Points;
            if (tempScore < 0)
            {
                // busted - no score
                this.QuitCurrentRoundForPlayer();
                return;
            } else if (tempScore == 0)
            {
                // won leg
                if (this.useDoubleOut && score.scoreMultiplier == Score.ScoreMultiplier.x2)
                {
                    this.CheckIfPlayerHasWon();
                    return;
                } else if (!this.useDoubleOut)
                {
                    this.CheckIfPlayerHasWon();
                    return;
                } else
                {
                    this.QuitCurrentRoundForPlayer();
                    return;
                }
            } else
            {
                // no special cases: just remove score of the throw from the player's total points
                player1Score = tempScore;
            }
        }
        else
        {
            infoLabel.text = "Player two";
            var tempScore = player2Score - score.Points;
            if (tempScore < 0)
            {
                // busted - no score
                this.QuitCurrentRoundForPlayer();
                return;
            }
            else if (tempScore == 0)
            {
                // won leg
                if (this.useDoubleOut && score.scoreMultiplier == Score.ScoreMultiplier.x2)
                {
                    this.CheckIfPlayerHasWon();
                    return;
                }
                else if (!this.useDoubleOut)
                {
                    this.CheckIfPlayerHasWon();
                    return;
                }
                else
                {
                    this.QuitCurrentRoundForPlayer();
                    return;
                }
            }
            else
            {
                player2Score = tempScore;
            }
        }

        infoLabel.text += " scored <i>" + score.Points + "</i>.";

        if (throwNumber == ThrowNumber.THROW1)
        {
            throwNumber = ThrowNumber.THROW2;
        } else if (throwNumber == ThrowNumber.THROW2)
        {
            throwNumber = ThrowNumber.THROW3;
        } else   // if it is the last throw, change to the other player and store current score
        {
            if (isPlayer1)
            {   
                player1ScoreBeforeRound = player1Score; // store current score
            }
            else
            {
                player2ScoreBeforeRound = player2Score;
            }

            this.ChangePlayers();
        }
        // Debug.Log("thrownumber after: " + throwNumber);
    }

    private void CheckIfPlayerHasWon()
    {

        if (this.isPlayer1)
        {
            if (player1Legs <= 1)
            {
                this.infoLabel.text = "Player 1 won the leg!";
                player1Legs++;
                this.StartNewLeg();
                return;
            } else
            {
                if (player1Sets <= 1)
                {
                    this.infoLabel.text = "Player 1 won the set!";
                    player1Sets++;
                    this.StartNewSet();
                    return;
                } else
                {
                    this.infoLabel.text = "Player 1 won the match!";
                    PlayerPrefs.SetString("Winner", "Player 1 won!");   // store winner in player prefs
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);   // load game over scene
                    return;
                }
            }
        }
        else
        {
            if (player2Legs <= 1)
            {
                this.infoLabel.text = "Player 2 won the leg!";
                player1Legs++;
                this.StartNewLeg();
                return;
            }
            else
            {
                if (player2Sets <= 1)
                {
                    this.infoLabel.text = "Player 2 won the set!";
                    player2Sets++;
                    this.StartNewSet();
                    return;
                }
                else
                {
                    this.infoLabel.text = "Player 2 won the match!";
                    PlayerPrefs.SetString("Winner", "Player 2 won!");   // store winner in player prefs
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);   // load game over scene
                    return;
                }
            }
        }
    }

    private void StartNewLeg()
    {
        this.player1Score = pointsPerLeg;
        this.player1ScoreBeforeRound = pointsPerLeg;
        this.player2Score = pointsPerLeg;
        this.player2ScoreBeforeRound = pointsPerLeg;
        this.ChangePlayers();
    }

    private void StartNewSet()
    {
        this.player1Score = pointsPerLeg;
        this.player1ScoreBeforeRound = pointsPerLeg;
        this.player2Score = pointsPerLeg;
        this.player2ScoreBeforeRound = pointsPerLeg;
        this.player1Legs = 0;
        this.player2Legs = 0;
        this.ChangePlayers();
    }

    /*
     * Gets called whenever a player reaches 'no score' (= below 0 or to 0 without doubling out)
     */
    private void QuitCurrentRoundForPlayer()
    {
        this.infoLabel.text = "No Score!";

        if (this.isPlayer1)
        {
            player1Score = player1ScoreBeforeRound; // reset score
        } else
        {
            player2Score = player2ScoreBeforeRound; // reset score
        }

        this.ChangePlayers();
    }

    private void ChangePlayers()
    {
        isPlayer1 = !isPlayer1;
        if (isPlayer1)
        {
            TurnPanelPlayer1.SetActive(true);
            TurnPanelPlayer2.SetActive(false);
        }
        else
        {
            TurnPanelPlayer1.SetActive(false);
            TurnPanelPlayer2.SetActive(true);
        }
        infoLabel.text += " Next.";
        throwNumber = ThrowNumber.THROW1;

        this.arePlayersChanging = true;
    }

    private IEnumerator ShowScoreIndicator(string message)
    {
        float delay = 0.03f;
        int numberOfSteps = 15;
        float fractionDelay = delay / numberOfSteps;
        float fontStep = 0.6f;

        this.scoreIndicatorLabel.text = message;
        this.scoreIndicatorLabel.enabled = true;

        this.scoreIndicatorLabel.CrossFadeAlpha(1.0f, 0.25f, false);

        for (int i = 0; i < numberOfSteps; i++)
        {
            yield return new WaitForSeconds(fractionDelay);
            this.scoreIndicatorLabel.fontSize += fontStep;
        }

        for (int i = 0; i < numberOfSteps; i++)
        {
            yield return new WaitForSeconds(fractionDelay);
            this.scoreIndicatorLabel.fontSize -= fontStep;
        }

        yield return new WaitForSeconds(1);

        // fade to transparent over 500ms.
        this.scoreIndicatorLabel.CrossFadeAlpha(0.0f, 0.25f, false);

        this.scoreIndicatorLabel.enabled = false;
    }

    private IEnumerator ShowPlayerChangeIndicator(string message)
    {
        // wait for other animations to finish 
        yield return new WaitForSeconds(1.6f);

        float delay = 0.03f;
        int numberOfSteps = 15;
        float fractionDelay = delay / numberOfSteps;
        float fontStep = 0.6f;

        this.playerChangeIndicatorLabel.text = message;
        this.playerChangeIndicatorLabel.enabled = true;

        this.playerChangeIndicatorLabel.CrossFadeAlpha(1.0f, 0.25f, false);

        for (int i = 0; i < numberOfSteps; i++)
        {
            yield return new WaitForSeconds(fractionDelay);
            this.playerChangeIndicatorLabel.fontSize += fontStep;
        }

        for (int i = 0; i < numberOfSteps; i++)
        {
            yield return new WaitForSeconds(fractionDelay);
            this.playerChangeIndicatorLabel.fontSize -= fontStep;
        }
    }

    private void ThrowDart()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                flickTime = 5;
                //TimeIncrease();
                startTime = Time.time;
                couldBeSwipe = true;
                touchStart = touch.position;

                Vector3 spawnPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchStart.x, touchStart.y, 5));

                GameObject dart;

                //spawn dart
                if (isPlayer1)
                {
                    dart = (GameObject)Instantiate(dartPrefabOne, spawnPosition, dartPrefabOne.transform.rotation);
                }
                else
                {
                    dart = (GameObject)Instantiate(dartPrefabTwo, spawnPosition, dartPrefabTwo.transform.rotation);
                }


                // store dart:
                currentDart = dart;
                currentDart.GetComponent<Rigidbody>().isKinematic = true;

                break;
            case TouchPhase.Moved:
                if (Mathf.Abs(touch.position.y - touchStart.y) < comfortZone)
                {
                    couldBeSwipe = false;

                    //drag Dart around
                    Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 5));
                    //currentDart.transform.position = Vector3.Lerp(currentDart.transform.position, pos, Time.deltaTime);
                    currentDart.transform.position = pos;
                }
                else
                {
                    couldBeSwipe = true;
                }
                break;
            case TouchPhase.Stationary:
                if (Mathf.Abs(touch.position.y - touchStart.y) < comfortZone)
                {
                    couldBeSwipe = false;
                }
                startTime = Time.time;
                break;
            case TouchPhase.Ended:
                flickLength = (touch.position - touchStart).magnitude;

                if (couldBeSwipe && flickLength > comfortZone)
                {
                    touchEnd = touch.position;
                    var body = currentDart.GetComponent<Rigidbody>();
                    body.isKinematic = false;

                    if (state != State.PLAYERCHANGESTATE)
                    {
                        state = State.CLOSECAMERASTATE;   // change mode to Dart
                    }

                    dartQueue.Enqueue(currentDart);

                    //Adjust dart flight
                    GetSpeed();
                    GetAngle();

                    //give the dartBody a impulse
                    body.centerOfMass = new Vector3(-5,0);
                    body.AddForce(new Vector3(worldAngle.x * dartSpeed, worldAngle.y * dartSpeed, worldAngle.z * dartSpeed));
                    //currentDart.transform.forward = Vector3.Slerp(dart.transform.forward, body.velocity.normalized, Time.deltaTime);
                }
                else
                {
                    Destroy(currentDart);
                }
                break;
        }



    }

    //helper methods for swipe

    private void GetSpeed()
    {

        diffTime = Time.time - startTime;
        // Debug.Log("FlickTime: " + flickTime);

        if (flickTime > 0)
        {
            dartVelocity = flickLength / (flickLength - flickTime);

        }
        dartSpeed = dartVelocity * 4;
        dartSpeed = dartSpeed - (dartSpeed * 1.65f);

        if (dartSpeed <= -33)
        {
            dartSpeed = -33;
        }

       //Debug.Log("Flick distance: " + flickLength);
        //Debug.Log("Velocity: " + dartVelocity);
        //Debug.Log("DartSpeed: " + dartSpeed);
        flickTime = 5;
    }

    private void GetAngle()
    {
        worldAngle = Camera.main.ScreenToWorldPoint(new Vector3(touchEnd.x, touchEnd.y + 800, ((Camera.main.nearClipPlane - 100) * 1.8f)));
    }
}