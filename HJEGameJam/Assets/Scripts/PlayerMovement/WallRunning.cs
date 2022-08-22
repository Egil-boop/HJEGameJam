using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;
using UnityEditor.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

public class WallRunning : MonoBehaviour
{
    [Header("Wallrunnig")] public LayerMask whatIsWall;
    public LayerMask whatIsGround;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;

    public float climbSpeed;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")] private float horizontalInput;
    private float verticalInput;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;
    private bool upwardsRunning;
    private bool downwardsRunning;

    [Header("Detection")] public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Refrences")] public Transform orientation;
    private PlayerMovement pm;
    private Rigidbody rb;
    public PlayerCam cam;
    public LedgeGrabbing lg;
    
    [Header("Exiting")] private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;
    
    
    

    [Header("Gravity")] public bool useGravity;
    public float gravityCounterForce;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        lg = GetComponent<LedgeGrabbing>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance,
            whatIsWall);
      
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance,
            whatIsWall);
        
        if (wallRight && rightWallHit.collider.gameObject != null)
        {
            if (AbortIfOnSameWall(rightWallHit.collider.gameObject)) return;
            pm.wallThatIRemember = rightWallHit.collider.gameObject;
        }
        else if (wallLeft && leftWallHit.collider.gameObject != null)
        {
            if (AbortIfOnSameWall(leftWallHit.collider.gameObject)) return;
            pm.wallThatIRemember = leftWallHit.collider.gameObject;
        }
    }

    private bool AbortIfOnSameWall(Object g)
    {
        if (pm.wallThatIRemember != null && pm.wallThatIRemember.name == g.name
                                         && wallRunTimer <= 0)
        {
            exitingWall = true;
            exitWallTimer = exitWallTime;
            return true;
        }

        return false;
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    private void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);


        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall)
        {
            if (!pm.wallrunning)
            {
                StartWallRun();
            }

            if (wallRunTimer > 0)
            {
                wallRunTimer -= Time.deltaTime;
            }

            if (wallRunTimer <= 0 && pm.wallrunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
                return;
            }

            if (Input.GetKeyDown(jumpKey))
            {
                WallJump();
            }
        }
        else if (exitingWall)
        {
            if (pm.wallrunning)
            {
                StopWallRun();
            }

            if (exitWallTimer > 0)
            {
                exitWallTimer -= Time.deltaTime;
            }

            if (exitWallTimer <= 0)
            {
                exitingWall = false;
            }
        }
        else
        {
            StopWallRun();
        }
    }

    private void FixedUpdate()
    {
        if (pm.wallrunning)
        {
            WallRunningMovement();
        }
    }

    private void StartWallRun()
    {
        pm.wallrunning = true;

        wallRunTimer = maxWallRunTime;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.y);
        cam.DoFov(90f);
        if (wallLeft)
        {
            cam.DoTilt(-5f);
        }

        if (wallRight)
        {
            cam.DoTilt(5f);
        }
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (upwardsRunning)
        {
            rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
        }

        if (downwardsRunning)
        {
            rb.velocity = new Vector3(rb.velocity.x, -climbSpeed, rb.velocity.z);
        }

        // Push Towards wall
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        {
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }

        // weaken
        if (useGravity)
        {
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
        }
    }

    private void StopWallRun()
    {
        pm.wallrunning = false;
        cam.DoFov(80f);
        cam.DoTilt(0f);
    }

    private void WallJump()
    {
        if (lg.holding || lg.exitingLedge)
        {
            return;
        }
        
        
        exitingWall = true;
        exitWallTimer = exitWallTime;
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }
}