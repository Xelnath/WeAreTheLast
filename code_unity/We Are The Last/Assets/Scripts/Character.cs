using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public GameObject bubbleLoc;
    public string id;
    public Vector3 bubblePos => bubbleLoc.transform.position;
}
