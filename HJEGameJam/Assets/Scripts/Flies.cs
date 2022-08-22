using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Random = System.Random;

public class Flies : MonoBehaviour
{
    
    public float degreesPerSecond = 15.0f;
    public float amplitude = 0.5f;
    public float frequency = 1f;

    private Vector3 posOffset = new Vector3();
    private Vector3 tempPos = new Vector3();

    private void Start()
    {
        posOffset = transform.position;
    }

    private void Update()
    {
        
        transform.Rotate(new Vector3(0, Time.deltaTime * degreesPerSecond,0), Space.World);
        tempPos = posOffset;
        tempPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude;
        transform.position = tempPos;


    }
    
    private void OnTriggerEnter(Collider other)
    {
        gameObject.SetActive(false);

    }
}
