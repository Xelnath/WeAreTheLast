using UnityEngine;
using System.Collections;

namespace Ares.Examples {
	public class Clouds : MonoBehaviour {
		[SerializeField] float rollSpeed = 1f;
		[SerializeField] float flipSpeed = 1f;
		[SerializeField] float falloffSpeed = 1f;
		[SerializeField] AnimationCurve flipCurve;

		Material material;

		void Start(){
			material = GetComponent<MeshRenderer>().material;
		}
		
		void Update(){
			material.SetTextureOffset("_MainTex", new Vector2(0f, Time.time * rollSpeed));
			material.SetTextureOffset("_DispTex", new Vector2(0f, Time.time * rollSpeed));
			material.SetFloat("_Flip", flipCurve.Evaluate((Mathf.Sin(Time.time * flipSpeed) + 1) * 0.5f));
			material.SetFloat("_Falloff", (Mathf.Sin(Time.time * falloffSpeed) + 1) * 1.5f);
			GetComponent<MeshFilter>().mesh.RecalculateNormals();
		}
	}
}