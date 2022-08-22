using System;
using Unity.VisualScripting;
using UnityEditor.Searcher;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class LedgeGrabbing : MonoBehaviour
{
    [Header("References")] public PlayerMovement pm;
    public Transform orientation;
    public Transform cam;
    public Rigidbody rb;

    [Header("Ledge Grabbing")] public float moveToLedgeSpeed;
    public float maxLedgeGrabDistance;
    public float minTimeOnledge;
    private float currentTimeOnLedge;
    public bool holding;

    [Header("Ledge Jumping")] public KeyCode jumpKey = KeyCode.Space;
    public float ledgeJumpForwardForce;
    public float ledgeJumpUpward;

    [Header("Exiting")] public bool exitingLedge;
    public float exitLedgeTime;
    private float exitLedgeTimer;
    
    
    [Header("Ledge Detection")] public float ledgeDetectionLength;
    public float ledgeSphereCastRadius;
    public LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform currLedge;

    private RaycastHit ledgeHit;

    private void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }

    private void LedgeDetection()
    {
        bool ledgeDetected = Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, 
            out ledgeHit, ledgeDetectionLength, whatIsLedge);
        
        if(!ledgeDetected){
            return;
        }
        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);

        if (ledgeHit.transform == lastLedge)
        {
            return;
        }
        
        if (distanceToLedge < maxLedgeGrabDistance && !holding)
        {
            EnterLedgeHold();
        }
        
    }

    private void SubStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        bool anyInputKeyPressed = horizontalInput != 0 || verticalInput != 0;
        
        // substate 1 Hold

        if (holding)
        {
            FreezRigibodyOnLedge();
            currentTimeOnLedge += Time.deltaTime;
            if (currentTimeOnLedge > minTimeOnledge && anyInputKeyPressed)
            {
                ExitLedgeHold();
            }

            if (Input.GetKeyDown(jumpKey))
            {
                LedgeJump();
            }
        } else if (exitingLedge)
        {
            if (exitLedgeTimer > 0)
            {
                exitLedgeTimer -= Time.deltaTime;
            }
            else
            {
                exitingLedge = false;
            }
        }
        


    }

    private void LedgeJump()
    {
        ExitLedgeHold();
        Invoke(nameof(DelayedJumpForce), 0.05f);
    }

    private void DelayedJumpForce()
    {
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpForwardForce;
        rb.velocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }
    
    private void EnterLedgeHold()
    {
        holding = true;

        pm.unlimited = true;
        pm.restricted = true;
        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }

    private void FreezRigibodyOnLedge()
    {
        rb.useGravity = false;
        Vector3 directionToLedge = currLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, currLedge.position);

        if (distanceToLedge > 1f)
        {
            if (rb.velocity.magnitude < moveToLedgeSpeed)
            {
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime);
            }
        }
        else
        {
            if (!pm.freeze)
            {
                pm.freeze = true;
            }

            if (pm.unlimited)
            {
                pm.unlimited = false;
            }
        }
        
        if(distanceToLedge > maxLedgeGrabDistance) ExitLedgeHold();

    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;
        
        holding = false;
        currentTimeOnLedge = 0f;


        pm.unlimited = false;
        pm.freeze = false;
        pm.restricted = false;

        rb.useGravity = true;
        
        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f);
    }

    private void ResetLastLedge()
    {
        lastLedge = null;
    }

}