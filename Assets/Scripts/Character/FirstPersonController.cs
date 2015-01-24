using UnityEngine;
using System.Collections;
using System;

[RequireComponent (typeof (Rigidbody))]
[RequireComponent (typeof (CapsuleCollider))]

public class FirstPersonController : MonoBehaviour {

	public bool gunEquipped = false; //equiped gun?

	public float rotUpDown;// = 0;
	//public Vector3 speed;
	public float verticalSpeed;
	public float rotLeftRight;
	public float maxVelocityChange = 10.0f;
	public float mouseSensetivity = 1.0f;

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

    public bool isZoomed = false;

    //For looking, we are assigning rotations, but we need original values
    //that aren't getting modified, so we can re-assign them.
    private Vector3 startingCameraRotation;
    private Vector3 newRotationAngle;

	void Awake () {
		rigidbody.freezeRotation = true;
		rigidbody.useGravity = false;
	}
	
	
	// Use this for initialization
	void Start () {
        canCheckForJump = true;
        newRotationAngle = new Vector3();
        startingCameraRotation = transform.GetChild(0).transform.localRotation.eulerAngles;
        totalJumpsMade = 0;
		setControlStrings();
		rotLeftRight = 0.0f;
	}


	
	// Update is called once per frame
	void FixedUpdate () {

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
            
						rigidbody.velocity = new Vector3 (rigidbody.velocity.x, CalculateJumpVerticalSpeed (), rigidbody.velocity.z);

						Invoke ("AllowJumpCheck", 0.1f);
				}
				if (true) {

						//Gram being a massive jerk. DONT EVER ENABLE THIS. IM WARNING YOU
						Vector3 targetVelocity;
						targetVelocity = new Vector3 (Input.GetAxis (Strf_str), 0, Input.GetAxis (FWmv_str));
						/*if (!GM._M.invertControls)
								targetVelocity = new Vector3 (Input.GetAxis (Strf_str), 0, Input.GetAxis (FWmv_str));
						else
								targetVelocity = new Vector3 (-Input.GetAxis (Strf_str), 0, -Input.GetAxis (FWmv_str));
						// THis too. WHYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY?
						if (GM._M.noStrafe) {
								targetVelocity.x = 0;
						}*/
						targetVelocity = transform.TransformDirection (targetVelocity);
						targetVelocity *= moveSpeed;
						// Apply a force that attempts to reach our target velocity
						Vector3 velocity = rigidbody.velocity;
						Vector3 velocityChange = (targetVelocity - velocity);

						velocityChange.x = Mathf.Clamp (velocityChange.x, -maxVelocityChange, maxVelocityChange);
						velocityChange.z = Mathf.Clamp (velocityChange.z, -maxVelocityChange, maxVelocityChange);
						velocityChange.y = 0;

						rigidbody.AddForce (velocityChange, ForceMode.VelocityChange);

						// Jump
						//Manager.say("Jumping action go. Jumps Made: " + totalJumpsMade + " Jumps Allowed: " + totalJumpsAllowed, "eliot");
				}

				rigidbody.AddForce (new Vector3 (0, -gravity * rigidbody.mass, 0));
				// We apply gravity manually for more tuning control


	}  
	private float CalculateJumpVerticalSpeed () {
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt(2 * (jumpHeight+jumpHeightModifier-weightModifier) * gravity);
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