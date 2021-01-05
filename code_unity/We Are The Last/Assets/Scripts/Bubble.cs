using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bubble : MonoBehaviour
{
    public Text txt;
    public Image back;

    public float CUSize;
    public float WideSize;

    public List<Sprite> img;
    public int imgCount;

    // Start is called before the first frame update
    void Start()
    {
        PicSwitch();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PicSwitch()
    {
        if (imgCount < img.Count-1)
        {
            imgCount++;
        }
        else
        {
            imgCount = 0;
        }
        back.sprite = img[imgCount];
        LeanTween.delayedCall(Random.Range(0.2f, 0.3f), PicSwitch);
    }
}
