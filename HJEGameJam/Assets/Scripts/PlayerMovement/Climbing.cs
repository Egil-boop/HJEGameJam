using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UI;

public class Climbing : MonoBehaviour
{
    [Header("References")] public Transform orientation;
    public Rigidbody rb;
    public LayerMask whatIsWall;
    public PlayerMovement pm;
    private LedgeGrabbing lg;

    [Header("Input")] public KeyCode walkingForwardKey = KeyCode.W;

    [Header("Climbing")] public float climbSpeed;
    public float maxClimbTime;
    private float climbTimer;
    private bool climbing;

    [Header("ClimbJump")] public float climbJumpUpForce;
    public float climbJumpBackForce;

    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps;
    private int climbJumpsLeft;

    [Header("Detection")] public float detectionLenght;
    public float spherecastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    [Header("ExitingWall")] public bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;


    private RaycastHit frontWallHit;
    private bool wallFront;

    public bool GetWallFront()
    {
        return wallFront;
    }

    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;

    private void Start()
    {
        lg = GetComponent<LedgeGrabbing>();
    }


    private void Update()
    {
        WallCheck();
        StateMachine();

        if (climbing && !exitingWall)
        {
            ClimbMovement();
        }
    }

    private void StateMachine()
    {
        if (lg.holding)
        {
            if (climbing)
            {
                StopClimbing();
            }
        }
        else if (wallFront && Input.GetKey(walkingForwardKey) && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing && climbTimer > 0)
            {
                StartClimbing();
            }

            if (climbTimer > 0)
            {
                climbTimer -= Time.deltaTime;
            }

            if (climbTimer <= 0)
            {
                StopClimbing();
            }
        }
        else if (exitingWall)
        {
            if (climbing)
            {
                StopClimbing();
            }

            if (exitWallTimer > 0)
            {
                exitWallTimer -= Time.deltaTime;
            }

            if (exitWallTimer < 0)
            {
                exitingWall = false;
            }
        }

        else
        {
            if (climbing)
            {
                StopClimbing();
            }
        }

        if (wallFront && Input.GetKeyDown(jumpKey) && climbJumps > 0)
        {
            ClimbJump();
        }
    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, spherecastRadius,
            orientation.forward, out frontWallHit, detectionLenght, whatIsWall);

        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall ||
                       Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;

        if ((wallFront && newWall) || pm.GetGrounded())
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private void StartClimbing()
    {
        climbing = true;
        pm.climbing = true;
        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
        // Camera Fov
    }

    private void ClimbMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);
    }

    private void StopClimbing()
    {
        climbing = false;
        pm.climbing = false;
    }

    private void ClimbJump()
    {
        if (pm.GetGrounded())
        {
            return;
        }

        if (lg.holding || lg.exitingLedge)
        {
            return;
        }

        exitingWall = true;
        exitWallTimer = exitWallTime;
        Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

        rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
        climbJumpsLeft--;
    }
}