using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")] public Transform orientation;
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")] public float maxSlideTime;
    public float slideForce;
    private float slideTimer;


    public float slideYScale;
    private float startYScale;

    [Header("Input")] public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticaleInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        startYScale = playerObj.localScale.y;
    }

    private void Update()
    {
        
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticaleInput = Input.GetAxisRaw("Vertical");
        
        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticaleInput != 0))
        {
            StartSlide();
        }

        if (Input.GetKeyUp(slideKey) && pm.sliding)
        {
            StopSlide();
        }

        if (!pm.isSomethingAboveMe && !pm.sliding && pm.state != PlayerMovement.MovementState.crouching)
        {
            playerObj.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }

    }

    private void FixedUpdate()
    {
        if (pm.sliding)
        {
            SlidingMovement();
        }
    }

    private void StartSlide()
    {
        pm.sliding = true;
        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        Vector3 inpuitDirection = orientation.forward * verticaleInput + orientation.right * horizontalInput;
        if (!pm.OnSlope() || rb.velocity.y > -0.1f)
        {
            
            rb.AddForce(inpuitDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
            
        }
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inpuitDirection) * slideForce, ForceMode.Force);
        }
        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
       
        pm.sliding = false;
        
    }
}