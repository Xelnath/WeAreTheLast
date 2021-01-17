using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FloatingDamageNumber : MonoBehaviour
{
    public AnimationCurve Transparency;
    public AnimationCurve VerticalMovement;
    public AnimationCurve HorizontalMovement;

    public float HorizontalMulti = 1;
    public float VerticalMulti = 1;
    
    public GameObject child;
    public RectTransform rt;
    public TextMeshProUGUI tmp;
    private float m_time = 0f;
    
    // Start is called before the first frame update
    void Awake()
    {
        child = transform.GetChild( 0 ).gameObject;
        rt = child.GetComponent<RectTransform>();
        tmp = child.GetComponent<TextMeshProUGUI>();
        m_time = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        m_time += Time.deltaTime;

        var a = Transparency.Evaluate( m_time );
        var x = HorizontalMovement.Evaluate( m_time );
        var y = VerticalMovement.Evaluate( m_time );

        rt.anchoredPosition = new Vector3( x * HorizontalMulti, y * VerticalMulti);
        var c = tmp.color;
        c.a = a;
        tmp.color = c;
    }
}
