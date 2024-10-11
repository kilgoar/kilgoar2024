using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
	private Vector3 movement = new Vector3(0,0,0);
	private float movementSpeed = .1f;
	private float rotationSpeed = .25f;
	private InputControl<Vector2> mouseMovement;
	private Vector3 globalMove = new Vector3(0,0,0);
	private float pitch = 90f;
	private float yaw = 0f;
	private float sprint = 1f;
	Quaternion dutchlessTilt;
	
    void Start()
    {

    }

    void Update()
    {
		Camera cam = Camera.current;
		Keyboard key = Keyboard.current;
		
        if (Mouse.current.rightButton.isPressed) {
            mouseMovement = Mouse.current.delta;

            pitch -= mouseMovement.ReadValue().y * rotationSpeed;
			yaw += mouseMovement.ReadValue().x * rotationSpeed;
            if(pitch > 89f || pitch < -89f) {
				cam.transform.rotation *= Quaternion.Euler(pitch, yaw, 0f);
			}
		
            Quaternion dutchlessTilt = Quaternion.Euler(pitch, yaw, 0f);
            cam.transform.rotation = dutchlessTilt;
			
        }
		
		if(key.shiftKey.isPressed){
			sprint = 3f;
		}
		else{
			sprint = 1f;
		}
		
		if(key.wKey.isPressed){
			movement.z = 1f;
			globalMove = cam.transform.forward * movement.z * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}
		
		if(key.sKey.isPressed){
			movement.z = -1f;
			globalMove = cam.transform.forward * movement.z * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}
		
		if(key.aKey.isPressed){
			movement.x = -1f;
			globalMove = cam.transform.right * movement.x * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}
		
		if(key.dKey.isPressed){
			movement.x = 1f;
			globalMove = cam.transform.right * movement.x * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}	

		if(key.ctrlKey.isPressed){
			movement.z = -1f;
			globalMove = cam.transform.up * movement.z * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}
		
		if(key.spaceKey.isPressed){
			movement.z = 1f;
			globalMove = cam.transform.up * movement.z * movementSpeed * sprint;
			cam.transform.position += globalMove;
		}	
    }
}
