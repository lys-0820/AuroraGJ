using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandBob : MonoBehaviour
{
    public float bobSpeed = 3f;
    public float bobAmount = 0.02f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.localPosition = startPos + new Vector3(0, offset, 0);
    }
}

