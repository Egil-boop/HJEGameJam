using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu( fileName = "StatsHolder", menuName = "ScriptableObject/StatsHolder")]
public class StatsHolder : ScriptableObject
{
    
    [Header("Inputs")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    
    [Header("Movement speedSettings")]
    public float WalkSpeed = 5;
    public float sprintSpeed = 7;
    public float slideSpeed = 25;
    public float wallRunSpeed  = 3;
    public float dashSpeed = 12;
    public float dashSpeedFactor = 50;
    public float climbSpeed = 3;
    public float groundDrag = 4;
    public float maxYSpeed = 4;
    public float crouchSpeed = 2;
    
    public float speedIncreaseMultiplier = 1.5f;
    public float slopeIncreaseMultiplier = 2.5f;
  
    [Header("Angle")]
    public float maxSlopeAngle = 40;
    
    [Header("Scale")]
    public float crouchYScale = 0.5f;
    
    [Header("Jumping")]
    public float jumpForce = 7;
    public float jumpCooldown = 0.25f;
    public float airMulti = 0.4f;
    
}
