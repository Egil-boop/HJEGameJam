using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    public StatsHolder statsHolder;

    private void setUpValuesFromStatsHolder()
    {
        #region Movement Stats

        walkSpeed = statsHolder.WalkSpeed;
        sprintSpeed = statsHolder.sprintSpeed;
        slideSpeed = statsHolder.slideSpeed;
        wallRunSpeed = statsHolder.wallRunSpeed;
        dashSpeed = statsHolder.dashSpeed;
        dashSpeedFactor = statsHolder.dashSpeedFactor;
        climbSpeed = statsHolder.climbSpeed;
        groundDrag = statsHolder.groundDrag;
        maxYSpeed = statsHolder.maxYSpeed;

        speedIncreaseMultiplier = statsHolder.speedIncreaseMultiplier;
        slopeIncreaseMultiplier = statsHolder.slopeIncreaseMultiplier;

        #endregion

        #region SlopeHandeling

        maxSlopeAngle = statsHolder.maxSlopeAngle;

        #endregion

        #region Crouch

        crouchSpeed = statsHolder.crouchSpeed;
        crouchYScale = statsHolder.crouchYScale;

        #endregion

        #region Inputs

        sprintKey = statsHolder.sprintKey;
        jumpKey = statsHolder.jumpKey;
        crouchKey = statsHolder.crouchKey;

        #endregion

        #region jumping

        jumpForce = statsHolder.jumpForce;
        jumpCooldown = statsHolder.jumpCooldown;
        airMulti = statsHolder.airMulti;

        #endregion
    }

    public void SetMaxYSpeed(float speed)
    {
        maxYSpeed = speed;
    }

    [Header("Refereces")] public Climbing climbingScript;
    public Transform orientation;

    
    private float moveSpeed;
    private float walkSpeed;
    private float sprintSpeed;
    private float slideSpeed;
    private float wallRunSpeed;
    private float dashSpeed;
    private float dashSpeedFactor;
    private float climbSpeed;
    private float groundDrag;
    private float maxYSpeed;
    
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;

    private float speedIncreaseMultiplier;
    private float slopeIncreaseMultiplier;

    private float horizontalInput;
    private float verticalInput;

    private float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exetingSlopeBool;
    
    private float crouchSpeed;
    private float crouchYScale;
    private float startYScale;

    private float jumpForce;
    private float jumpCooldown;
    private float airMulti;
    private bool readyToJump = true;

    private Vector3 moveDirection;

    private KeyCode jumpKey;
    private KeyCode sprintKey;
    private KeyCode crouchKey;

    [Header("Ground Check")] public float playerHeight;
    
    public LayerMask whatIsGround;
    public bool isSomethingAboveMe;

    private Rigidbody rb;

    public MovementState state;

    public enum MovementState
    {
        walking,
        dashing,
        sprinting,
        freeze,
        unlimited,
        crouching,
        climbing,
        wallrunning,
        sliding,
        air
    }

    private bool grounded;

    public bool GetGrounded()
    {
        return grounded;
    }

    public bool sliding;
    public bool wallrunning;
    public bool climbing;
    public bool dashing;
    public bool restricted;
    public bool freeze;
    public bool unlimited;

    public bool activeGrapple;
    private bool enableMovementOnNextTouch;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        setUpValuesFromStatsHolder();
    }

    // Update is called once per frame
    void Update()
    {
        isSomethingAboveMe = Physics.BoxCast(transform.position,
            Vector3.one, Vector3.up, quaternion.identity,
            crouchYScale + 1);
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);
        MyInput();
        SpeedControl();
        StateHandler();


        if (state == MovementState.walking || state == MovementState.sprinting ||
            state == MovementState.crouching && !activeGrapple)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private bool keepMomentum;
    private MovementState lastState;

    private void StateHandler()
    {
        if (dashing)
        {
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedFactor;
        }
        else if (freeze)
        {
            state = MovementState.freeze;
            rb.velocity = Vector3.zero;
            desiredMoveSpeed = 0f;
        }
        else if (unlimited)
        {
            state = MovementState.unlimited;
            desiredMoveSpeed = 999;
            return;
        }
        else if (climbing)
        {
            state = MovementState.climbing;
            desiredMoveSpeed = climbSpeed;
        }
        else if (wallrunning)
        {
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallRunSpeed;
        }

        else if (sliding)
        {
            state = MovementState.sliding;
            if (OnSlope() && rb.velocity.y < 0.1f)
            {
                keepMomentum = true;
                desiredMoveSpeed = slideSpeed;
            }
            else
            {
                desiredMoveSpeed = sprintSpeed;
            }
        }

        else if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }
        else if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        else if (grounded)
        {
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }
        else
        {
            state = MovementState.air;

            if (desiredMoveSpeed < sprintSpeed)
            {
                desiredMoveSpeed = walkSpeed;
            }
            else
            {
                desiredMoveSpeed = sprintSpeed;
            }
        }

        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;

        if (lastState == MovementState.dashing)
        {
            keepMomentum = true;
        }

        if (desiredMoveSpeedHasChanged)
        {
            if (keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyMoveSpeedWhenDashing());
            }
            else
            {
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed;
            }
        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
        if (Mathf.Abs(desiredMoveSpeed - moveSpeed) < 0.1f)
        {
            keepMomentum = false;
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        bool isKeyDown = Input.GetKey(crouchKey);
        if (!isKeyDown && !isSomethingAboveMe)
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void Jump()
    {
        exetingSlopeBool = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        exetingSlopeBool = false;
        readyToJump = true;
    }


    private void MovePlayer()
    {
        if (activeGrapple)
        {
            return;
        }

        if (restricted)
        {
            return;
        }

        if (state == MovementState.dashing)
        {
            return;
        }

        if (climbingScript.exitingWall) return;

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope() && !exetingSlopeBool)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20f, ForceMode.Force);
            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection * moveSpeed * 10, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection * moveSpeed * 10 * airMulti, ForceMode.Force);
        }

        if (!wallrunning)
        {
            rb.useGravity = !OnSlope();
        }
    }

    private void SpeedControl()
    {
        if (activeGrapple)
        {
            return;
        }

        if (OnSlope())
        {
            if (rb.velocity.magnitude > moveSpeed && !exetingSlopeBool)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatvel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatvel.magnitude > moveSpeed)
            {
                Vector3 limitedvel = flatvel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedvel.x, rb.velocity.y, limitedvel.z);
            }
        }

        if (maxYSpeed != 00 && rb.velocity.y > maxYSpeed)
        {
            rb.velocity = new Vector3(rb.velocity.x, maxYSpeed, rb.velocity.z);
        }
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private float speedChangeFactor;


    private IEnumerator SmoothlyMoveSpeedWhenDashing()
    {
        float time = 0;
        float difference = MathF.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor;

        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);

            time += Time.deltaTime * boostFactor;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = MathF.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;


        while (time < difference)
        {
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);
                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
            {
                time += Time.deltaTime * speedIncreaseMultiplier;
            }

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
            GetComponent<Grappling>().StopGrapple();
        }
    }

    public void JumpToPosition(Vector3 grapplePoint, float highestPointOnArc)
    {
        activeGrapple = true;
        velocityToSet = CalculateJumpVelocity(transform.position, grapplePoint, highestPointOnArc);
        Invoke(nameof(SetVelocity), 0.1f);

        Invoke(nameof(ResetRestrictions), 3f);
    }

    private Vector3 velocityToSet;

    private void SetVelocity()
    {
        enableMovementOnNextTouch = true;
        rb.velocity = velocityToSet;
    }

    private Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
                                               + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }
}