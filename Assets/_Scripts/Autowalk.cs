﻿// This script moves your player automatically in the direction he is looking at. You can 
// activate the autowalk function by pull the cardboard trigger, by define a threshold angle 
// or combine both by selecting both of these options.
// The threshold is an value in degree between 0° and 90°. So for example the threshold is 
// 30°, the player will move when he is looking 31° down to the bottom and he will not move 
// when the player is looking 29° down to the bottom. This script can easally be configured
// in the Unity Inspector.Attach this Script to your CardboardMain-GameObject. If you 
// haven't the Cardboard Unity SDK, download it from https://developers.google.com/cardboard/unity/download

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class Autowalk : MonoBehaviour 
{

	public AudioSource AudioFile;
	// public Image panel;
	private const int RIGHT_ANGLE = 90; 
	
	// This variable determinates if the player will move or not 
	private bool isWalking = false;
	
	CardboardHead head = null;
	
	//This is the variable for the player speed
	[Tooltip("With this speed the player will move.")]
	public float speed;
	
	[Tooltip("Activate this checkbox if the player shall move when the Cardboard trigger is pulled.")]
	public bool walkWhenTriggered;
	
	[Tooltip("Activate this checkbox if the player shall move when he looks below the threshold.")]
	public bool walkWhenLookDown;
	
	[Tooltip("This has to be an angle from 0° to 90°")]
	public double thresholdAngle;
	
	[Tooltip("Activate this Checkbox if you want to freeze the y-coordiante for the player. " +
	         "For example in the case of you have no collider attached to your CardboardMain-GameObject" +
	         "and you want to stay in a fixed level.")]
	public bool freezeYPosition; 
	
	[Tooltip("This is the fixed y-coordinate.")]
	public float yOffset;

	private bool canToggleWalking = true;
	
	void Start () 
	{
		head = Camera.main.GetComponent<StereoController>().Head;
		// panel.gameObject.SetActive(false);
	}
	
	void Update () 
	{
		if (!canToggleWalking) {
			if (isWalking) {
				walk();
			}
			return;
		}

        // Walk when the Cardboard Trigger is used 
        if (walkWhenTriggered && !walkWhenLookDown && !isWalking && Cardboard.SDK.CardboardTriggered) 
		{
			isWalking = true;
			if (AudioFile != null) {
				AudioFile.Play ();
			}
		} 
		else if (walkWhenTriggered && !walkWhenLookDown && isWalking && Cardboard.SDK.CardboardTriggered) 
		{
			isWalking = false;
			if (AudioFile != null) {
				AudioFile.Stop ();
			}
		}
		
		// Walk when player looks below the threshold angle 
		if (walkWhenLookDown && !walkWhenTriggered && !isWalking &&  
		    head.transform.eulerAngles.x >= thresholdAngle && 
		    head.transform.eulerAngles.x <= RIGHT_ANGLE) 
		{
			isWalking = true;
		} 
		else if (walkWhenLookDown && !walkWhenTriggered && isWalking && 
		         (head.transform.eulerAngles.x <= thresholdAngle ||
		         head.transform.eulerAngles.x >= RIGHT_ANGLE)) 
		{
			isWalking = false;
		}

		// Walk when the Cardboard trigger is used and the player looks down below the threshold angle
		if (walkWhenLookDown && walkWhenTriggered && !isWalking &&  
		    head.transform.eulerAngles.x >= thresholdAngle && 
		    Cardboard.SDK.CardboardTriggered &&
		    head.transform.eulerAngles.x <= RIGHT_ANGLE) 
		{
			isWalking = true;
		} 
		else if (walkWhenLookDown && walkWhenTriggered && isWalking && 
		         head.transform.eulerAngles.x >= thresholdAngle &&
		         (Cardboard.SDK.CardboardTriggered ||
		         head.transform.eulerAngles.x >= RIGHT_ANGLE)) 
		{
			isWalking = false;
		}

		if (isWalking) 
		{
			walk();
        }
		
		if(freezeYPosition)
		{
			transform.position = new Vector3(transform.position.x, yOffset, transform.position.z);
		}
    }

	public void walk() {
		Vector3 direction = new Vector3(head.transform.forward.x, 0, head.transform.forward.z).normalized * speed * Time.deltaTime;
		Quaternion rotation = Quaternion.Euler(new Vector3(0, -transform.rotation.eulerAngles.y, 0));
		transform.Translate(rotation * direction);
	}

	public void setCanWalk(bool canWalk) {
		canToggleWalking = canWalk;
	}
    
    void OnCollisionEnter(Collision col)
    {
		Debug.Log(col.gameObject.tag);
		Debug.Log(col.gameObject);
		if (col.gameObject.tag == "floor") {
			return;
		}
        isWalking = false;
		if (AudioFile != null) {
			AudioFile.Stop();
		}
        //Application.LoadLevel("DungeonScene");
        //SceneManager.LoadScene("DungeonScene", LoadSceneMode.Single);

    }
}