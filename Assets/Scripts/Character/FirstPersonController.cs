using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent (typeof (Rigidbody))]
[RequireComponent (typeof (CapsuleCollider))]

public class FirstPersonController : MonoBehaviour {

	//player UI
	public Slider health;
	private float clock;
	public bool gunEquipped = false; //equiped gun?

	public float rotUpDown;// = 0;
	//public Vector3 speed;
	public float verticalSpeed;
	public float rotLeftRight;
	public float maxVelocityChange = 10.0f;
	public float mouseSensetivity = 1.0f;

    private bool HelpTextToggle = false;

	public float jumpHeight;
	public float gravity = 9.81f;
	public float upDownRange;
	private Vector3 playerPos;
	private Ray	ray;
	private RaycastHit rayHitDown;
	private bool isGrounded = true;
	public float moveSpeed;    
	public float totalJumpsAllowed;
	public float totalJumpsMade;
	private float floorInclineThreshold = 0.3f;

	private bool runningToggle = false;
	public bool canCheckForJump;

    private bool gameOver;

	private bool isDead;
	private int messageEdited; //1 = not caring, 2 = not finished, 3 = finished

	//ACTION STRINGS
	//==================================================================
	private string Haim_str = "_Look Rotation";
	private string Vaim_str = "_Look UpDown";
	private string Strf_str = "_Strafe";
	private string FWmv_str = "_Forward";
	private string Fire_str = "_Fire";
	private string Jump_str = "_Jump";
	private string Dash_str = "_Run";
	private string Zoom_str = "_Zoom";
	//==================================================================

    //PERSONAL CHARACTER MODIFIERS
    public float runSpeedModifier;
    public float walkSpeedModifier;
    public float weightModifier;
    public float healthModifier;
    public float jumpHeightModifier;
    public float armorModifier;

	public GameObject cloneToCopyUponDeath;
	public GameObject signToPlaceUponDeath;

    public bool isZoomed = false;

    

    //cliff control
    Ray rayOrigin;
    RaycastHit hitInfo;
    Vector3 rayOriginStart;
    bool ableToInteract;
    bool canMove;

    //For looking, we are assigning rotations, but we need original values
    //that aren't getting modified, so we can re-assign them.
    private Vector3 startingCameraRotation;
    private Vector3 newRotationAngle;

	//Anim Controller
	private Animator animController;

    private bool GamePaused;


	//carrying object
	private bool isCarrying;

	void Awake () {
		GetComponent<Rigidbody>().freezeRotation = true;
		GetComponent<Rigidbody>().useGravity = false;
	}
	
	
	// Use this for initialization
	void Start () {
        GamePaused = true;

		animController = gameObject.transform.GetChild (1).GetComponent<Animator> ();
		messageEdited = 1;
        canCheckForJump = true;
        newRotationAngle = new Vector3();
        startingCameraRotation = transform.GetChild(0).transform.localRotation.eulerAngles;
        totalJumpsMade = 0;
		setControlStrings();
		rotLeftRight = 0.0f;
		isDead = false;
		isCarrying = false;
		health.value = 1.0f;
		gameObject.GetComponent<Rigidbody>().isKinematic = false;
	}



    void Update()
    {
        if (Input.GetButtonDown("Suicide") && isDead == false)
        {
            Debug.Log("Controller Death button pressed");
            killPlayer();
        }

        if (Input.GetButtonDown("Help"))
        {
            ToggleHelpText();
        }

        //suicide
        if (Input.GetKeyDown("k") && isDead == false)
        {
            killPlayer();
        }

    }

	void FixedUpdate () {

      

		rayOrigin = new Ray(transform.position, transform.up*-1);

		//if you are able to reach something, anything important or not
        if (Physics.Raycast(rayOrigin, out hitInfo)) {
            if (Time.time % 2f > 1.8f) { Debug.Log("Below me is: " + hitInfo.normal.y); }
            if (hitInfo.normal.y <= 0.4f)
            {
                canMove = false;
            }
            else
            {
                canMove = true;
            }
        }
		//player's mortality slowly coming to it's inevitable conclusion
		//210 second life span
        if (!GamePaused)
        {
		    health.value = health.value - ((0.1f * Time.deltaTime) / 18.0f);//18 = 3mins, 24 = 4mins, 30 = 5mins...
        }
		
		clock = clock + Time.deltaTime;

		if (health.value <= 0.01f && !isDead) {
			killPlayer();
		}
		
		if (isDead == false) {		
			//player rotation
			//left and right
			rotLeftRight = Input.GetAxis (Haim_str) * mouseSensetivity;
			transform.Rotate (0, rotLeftRight, 0);
			//up and down (with camera)
			rotUpDown -= Input.GetAxis (Vaim_str) * mouseSensetivity;
			rotUpDown = Mathf.Clamp (rotUpDown, -upDownRange, upDownRange);
			newRotationAngle.x = rotUpDown;
			newRotationAngle.y = startingCameraRotation.y;
			newRotationAngle.z = startingCameraRotation.z;
			transform.GetChild (0).transform.localRotation = Quaternion.Euler (newRotationAngle);



			//Movement
			//Running!!
			if (Input.GetButtonDown (Dash_str)) {
				runningToggle = !runningToggle;
			}

			//Jumping!!
			if (totalJumpsMade < totalJumpsAllowed && Input.GetButtonDown (Jump_str)) {
				totalJumpsMade += 1;
				isGrounded = false;
				canCheckForJump = false;

				GetComponent<Rigidbody>().velocity = new Vector3 (GetComponent<Rigidbody>().velocity.x, CalculateJumpVerticalSpeed (), GetComponent<Rigidbody>().velocity.z);

				Invoke ("AllowJumpCheck", 0.1f);

			}
			if (canMove) {
				if(Input.GetAxis("p1_Forward") > 0.1f || Input.GetAxis("p1_Forward") < -0.1f  
				   || Input.GetAxis("p1_Strafe") > 0.1f || Input.GetAxis("p1_Strafe") < -0.1f ){
					animController.SetInteger("isState", 1);
					if(isCarrying){
						animController.SetInteger("isState", 2);
					}
				}else{
					if(isDead){
						animController.SetInteger("isState", 3);
					}else{
						animController.SetInteger("isState", 0);
					}
				}
				Vector3 targetVelocity;
				targetVelocity = new Vector3 (Input.GetAxis (Strf_str), 0, Input.GetAxis (FWmv_str));
				
				targetVelocity = transform.TransformDirection (targetVelocity);
				targetVelocity *= moveSpeed;
				// Apply a force that attempts to reach our target velocity
				Vector3 velocity = GetComponent<Rigidbody>().velocity;
				Vector3 velocityChange = (targetVelocity - velocity);

				velocityChange.x = Mathf.Clamp (velocityChange.x, -maxVelocityChange, maxVelocityChange);
				velocityChange.z = Mathf.Clamp (velocityChange.z, -maxVelocityChange, maxVelocityChange);
				velocityChange.y = 0;

				GetComponent<Rigidbody>().AddForce (velocityChange, ForceMode.VelocityChange);
				
				// Jump
				//Manager.say("Jumping action go. Jumps Made: " + totalJumpsMade + " Jumps Allowed: " + totalJumpsAllowed, "eliot");
			}

			GetComponent<Rigidbody>().AddForce (new Vector3 (0, -gravity * GetComponent<Rigidbody>().mass, 0));
			// We apply gravity manually for more tuning control
		}
		if (messageEdited != 1) {
			if(messageEdited == 3){
				rezPlayer();
				messageEdited = 1;
			}else{
				Debug.Log(GameObject.Find ("TypeCanvas").transform.GetChild (1).GetChild(2).GetComponent<Text>().text);
				//wait till message is completely written.
				if(Input.GetKeyDown("return")){
					health.value = 1.0f;
					GameObject sign = Instantiate(signToPlaceUponDeath, gameObject.transform.position+(Vector3.up*3.2f), gameObject.transform.rotation) as GameObject;
					sign.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = 
						GameObject.Find ("TypeCanvas").transform.GetChild (1).GetChild(2).GetComponent<Text>().text;
					messageEdited = 3;
					GameObject.Find ("TypeCanvas").transform.GetChild (1).GetChild(2).GetComponent<Text>().text = "";
                    Debug.Log("Message Finished!");
					GameObject.Find ("TypeCanvas").transform.GetChild (1).gameObject.SetActive (false);
				}
				//messageEdited = 3;
			}
		}
	}  

	private float CalculateJumpVerticalSpeed () {
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt(2 * (jumpHeight+jumpHeightModifier-weightModifier) * gravity);
	}

	public bool getIsDead(){
		return isDead;
	}

	public bool getIsCarrying(){
		return isCarrying;
	}

	public void setCarrying(bool hasCarry){
		isCarrying = hasCarry;	
	}

	public void killPlayer(){
		if (isCarrying) {
			this.gameObject.transform.GetChild(0).GetChild(0).GetComponent<BoxCollider>().enabled = true;
			this.gameObject.transform.GetChild(0).GetChild(0).GetComponent<Rigidbody>().isKinematic = false;
			this.gameObject.transform.GetChild(0).GetChild(0).parent = null;
			isCarrying = false;
		}
		GameObject.Find ("DeathTracker").GetComponent<DeathTracker> ().increaseDeathCount ();
		isDead = true;
		messageEdited = 2;
		gameObject.GetComponent<Rigidbody>().isKinematic = true;
		
        //Cursor.lockState = CursorLockMode.None;
		animController.SetInteger ("isState", 3);
		GameObject.Find ("TypeCanvas").transform.GetChild (1).gameObject.SetActive (true);
        GameObject.Find("TypeCanvas").transform.GetChild(1).GetComponent<InputField>().text = "";
        Debug.Log("MEssage cleared in killplayer");
	}

	private void rezPlayer(){
        GameObject.Find("HelpText").transform.GetChild(0).gameObject.SetActive(true);
        GameObject.Find("HelpText").transform.GetChild(1).gameObject.SetActive(true);
        HelpTextToggle = true;
        GamePaused = true;
		Instantiate(cloneToCopyUponDeath, 
		            GameObject.Find("SpawnPoint").transform.position, 
		            GameObject.Find("SpawnPoint").transform.rotation
		            );
		gameObject.transform.GetChild(0).gameObject.SetActive(false);
		health.value = 1.0f;
	}

	private void setControlStrings(){
		string pName = gameObject.name;

		if(pName.Contains("1")){
			Fire_str = "p1" + Fire_str;
			FWmv_str = "p1" + FWmv_str;
			Strf_str = "p1" + Strf_str;
			Haim_str = "p1" + Haim_str;
			Vaim_str = "p1" + Vaim_str;
			Jump_str = "p1" + Jump_str;
			Dash_str = "p1" + Dash_str;
			Zoom_str = "p1" + Zoom_str;
		}
	}

    public string GetFire_Str(){
        return Fire_str;
    }

    // piece of delays OnCollisionStay's ground check so we can't jump for 2 seconds after
    public void AllowJumpCheck()
    {
        //Manager.say("CAN CJECK JUMP", "eliot");
        canCheckForJump = true;
    }

	void OnCollisionStay(Collision floor){
		Vector3 tempVect;
        // we want to prevent isGrounded from being true and totalJumpsMade = 0 until 2 seconds later
		if(isGrounded == false && canCheckForJump){
			for(int i = 0; i < floor.contacts.Length; i++){
				tempVect = floor.contacts[i].normal;
				if( tempVect.y > floorInclineThreshold){
					isGrounded = true;
					totalJumpsMade = 0;
					return;
					//Manager.say("Collision normal is: " + tempVect);
				}
			}
		}
	}
    public bool isGameOver()
    {
        return gameOver;
    }

    public void ToggleHelpText()
    {
        if (HelpTextToggle)
        {
            GameObject.Find("HelpText").transform.GetChild(0).gameObject.SetActive(false);
            GameObject.Find("HelpText").transform.GetChild(1).gameObject.SetActive(false);
            HelpTextToggle = false;
            GamePaused = false;
        }
        else
        {
            GameObject.Find("HelpText").transform.GetChild(0).gameObject.SetActive(true);
            GameObject.Find("HelpText").transform.GetChild(1).gameObject.SetActive(true);
            HelpTextToggle = true;
            GamePaused = true;
        }
    }
}



/*
			if(isGrounded && Input.GetButtonDown(Jump_str)){
				rigidbody.AddForce(Vector3.up * GM._M.jumpHeight); 
				
				Debug.Log("Jumping attempted!");
			}
			else if(Input.GetButtonDown(Jump_str)){
				Debug.Log("Jumping attempted! and FAILED");
			}
			//Running!!
			if(GM._M.runningAllowed && isGrounded && Input.GetKeyDown(KeyCode.LeftShift)){
				GM._M.movementSpeed = 10.0f;
			}
			else if(GM._M.movementSpeed == 10.0f){
				GM._M.movementSpeed = 6.0f;
			}
			
			
			float forwardSpeed = Input.GetAxis(FWmv_str);
			float sideSpeed = Input.GetAxis(Strf_str);
			
			speed = new Vector3( sideSpeed*, 0, forwardSpeed*GM._M.movementSpeed);
			
			speed = transform.rotation * speed;
			
			rigidbody.velocity = speed*Time.deltaTime;//(rigidbody.position + speed * Time.deltaTime);*/