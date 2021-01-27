using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public GameObject bubbleLoc;
    public GameObject healthLoc;
    public string id;
    public Vector3 bubblePos => bubbleLoc.transform.localPosition;
    public Vector3 healthPos => healthLoc?.transform.localPosition ?? new Vector3(0f,1.1f,0f);
}
