using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class RelativeTextSize : MonoBehaviour {
	[SerializeField, Range(.01f, .2f)] float size;

	#if UNITY_EDITOR
	[SerializeField] bool updateInEditor;
	#endif

	void Awake(){
		GetComponent<Text>().fontSize = (int)(Camera.main.pixelHeight * size);
	}

	#if UNITY_EDITOR
	void Update(){
		if(updateInEditor && !Application.isPlaying){
			GetComponent<Text>().fontSize = (int)(Camera.main.pixelHeight * size);
		}
	}
	#endif
}