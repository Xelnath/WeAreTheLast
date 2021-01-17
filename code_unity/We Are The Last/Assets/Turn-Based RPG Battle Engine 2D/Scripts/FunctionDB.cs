using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClassDB;
using TMPro;
using UnityEngine.UI;

//This script contains functions used across multiple scripts
public class FunctionDB : MonoBehaviour {

	public static FunctionDB core;

	//The following functions are used to find certain element indexes in specified lists by given criteria
	public int findCharacterTemplateIndexByCharacterID (int seekId) {
		return Database.dynamic.characters.FindIndex(x => x.id == seekId);
	}
	public int findSkillIndexById (int seekId) {
		return Database.dynamic.skills.FindIndex(x => x.id == seekId);
	}

	public int findItemIndexById (int seekId) {
		return Database.dynamic.items.FindIndex(x => x.id == seekId);
	}

	public int findAttributeIndexById (int seekId, character c) {
		return c.characterAttributes.FindIndex(x => x.id == seekId);
	}
	public int findAttributeIndexByName (string name, character c) {
		return c.characterAttributes.FindIndex(x => x.name == name);
	}

	public characterAttribute findAttributeByName( InstanceID characterId, string attributeName )
	{
		var characterInstance = BattleManager.core.findCharacterInstanceById(characterId);
		var character = characterInstance.characterCopy;
		int index = findAttributeIndexByName( attributeName, character );
		if ( index < 0 ) return null;
		return characterInstance.characterCopy.characterAttributes[index];
	}

	public int findFunctionQueueIndexByCallInfo (BattleManager.BattleManagerContext ctx, callInfo ci) {
		return ctx.functionQueue.FindIndex(x => x == ci);
	}

	public int findFunctionQueueIndexByName (BattleManager.BattleManagerContext ctx, string s) {
		return ctx.functionQueue.FindIndex(x => x.functionName == s && x.isRunning);
	}

	//Getting battle manager characters list index by id
	public int findBattleManagerCharactersIndexById (InstanceID id) {
		return BattleManager.core.characterInstances.FindIndex(x => x.characterInstanceId == id);
	}

	//Getting audio clip by id
	public AudioClip findAudioClipById (int id) {

		foreach (audioInfo info in ObjectDB.core.AudioClips) {

			if (info.id == id) {
				return info.clip;
			}
		}

		return null;
	}

	//Getting sprite by id
	public Sprite findSpriteById (int id) {

		foreach (spriteInfo info in ObjectDB.core.Sprites) {

			if (info.id == id) {
				return info.sprite;
			}
		}

		return null;
	}

	//Getting character instance object by id
	public GameObject findCharInstanceGameObjectById (InstanceID id) {

		foreach (characterInfo info in BattleManager.core.characterInstances) {
			if (info.characterInstanceId == id) {
				return info.instanceObject;
			}
		}

		return null;
	}

	
	//Getting character spawn point by id
	public GameObject findCharSpawnById (InstanceID id) {

		foreach (characterInfo info in BattleManager.core.characterInstances) {
			if (info.characterInstanceId == id) {
				return info.spawnPointObject;
			}
		}

		return null;
	}

	//This function returns a list of all child objects of a transform
	public List<GameObject> childObjects (Transform tL) {
		var l = new List<GameObject>();
		foreach (Transform t in tL) {
			l.Add(t.gameObject);
		}
		return l;
	}

	//This function sets character animations
	public void setAnimation (InstanceID characterId, string animationName) {

		//Getting character
		int characterIndex = findBattleManagerCharactersIndexById (characterId);
		characterInfo character = BattleManager.core.characterInstances[characterIndex];

		//Setting animation
		character.currentAnimation = animationName;
	}

	//Checking animation status
	public bool checkAnimation (InstanceID characterId, string animationName) {

		//Getting character
		int characterIndex = findBattleManagerCharactersIndexById (characterId);
		var character = BattleManager.core.characterInstances[characterIndex];
		var instance = character.instanceObject;
		Animator charAnimator = instance.GetComponent<Animator>();

		//Returning status
		return charAnimator.GetBool(animationName);

	}

	//This function deletes all children of a gameobject
	public void emptyWindow (GameObject w) {
		foreach (Transform i in w.transform) {
			Destroy(i.gameObject);
		}
	}

	
	//This method is used to get the first active character in a characters list
	public int activeCharacter (List<InstanceID> l, int startingIndex, int inc) {

		for (int e = startingIndex + inc; e < l.Count; e++)
		{
			var instanceId = l[e];
			var character = BattleManager.core.findCharacterInstanceById( instanceId );
            var stun = FunctionDB.core.findAttributeByName( instanceId , "STUN" );
            if ( stun != null && stun.curValue > 0 )
            {
	            continue;
            }
			
			if (character.isAlive) {
				return e;
			}
		}

		return -1;
	}

	Coroutine audioTransitionRoutine;

	//Changing current audio
	public void changeAudio (int audioSourceIndex, int audioId) {
		if (audioTransitionRoutine != null) {
			StopCoroutine (audioTransitionRoutine);
		}

		audioTransitionRoutine = StartCoroutine (audioTransition(audioSourceIndex, audioId));
	}

	//This variable will be used by BattleManager's musicManager to determine whether an audio transition is in progress.
	[HideInInspector] public bool audioTransitioning;

	//Audio transition enumerator
	//Allows smooth transitions between currently playing clip and clip in question.
	IEnumerator audioTransition (int audioSourceIndex, int audioId) {

		audioTransitioning = true;

		//Getting audiosources
		var audio = GetComponents<AudioSource>();

		//Save current source volume
		var curVol = audio[audioSourceIndex].volume;

		//Mute source gradually
		while (audio[audioSourceIndex].volume > 0) {
			audio[audioSourceIndex].volume -= 0.01f;
			yield return new WaitForEndOfFrame();
		}

		//stopping audio
		stopAudio (audioSourceIndex);

		//Setting and playing new clip
		setAudio (audioSourceIndex, audioId);
		playAudio (audioSourceIndex);

		//Turning volume back on
		while (audio[audioSourceIndex].volume < curVol) {
			audio[audioSourceIndex].volume += 0.01f;
			yield return new WaitForEndOfFrame();
		}

		audioTransitioning = false;

	}

	//Playing audio once
	public void playAudioOnce (int audioSourceIndex, int audioId) {

		//Getting clip
		var clip = FunctionDB.core.findAudioClipById (audioId);

		//Getting audiosource(s)
		var audio = GetComponents<AudioSource>();

		//stopping audio
		if (audio.Length > audioSourceIndex) {
			audio[audioSourceIndex].PlayOneShot(clip);
		} else {
			Debug.Log ("Invalid source index " + audioSourceIndex.ToString());
		}
	}
	
	//Setting an audio clip is required to play, resume and pause the audio further on
	public void setAudio (int audioId, int audioSourceIndex) {
		//Getting clip
		var clip = FunctionDB.core.findAudioClipById (audioId);
		
		//Checking clip's validity
		if (clip != null) {
			//Getting audioSource(s)
			var audio = GetComponents<AudioSource>();

			//Setting audio
			if (audio.Length > audioSourceIndex) {
				audio[audioSourceIndex].clip = clip;
			} else {
				Debug.Log ("Invalid source index " + audioSourceIndex.ToString());
			}
			
		} else {
			Debug.Log ("Invalid audio clip id " + audioId.ToString());
		}
	}
	
	//Playing set audio
	public void playAudio (int audioSourceIndex) {

		//Getting audiosource(s)
		var audio = GetComponents<AudioSource>();

		//Playing audio
		if (audio.Length > audioSourceIndex) {
			audio[audioSourceIndex].Play();
		} else {
			Debug.Log ("Invalid source index " + audioSourceIndex.ToString());
		}

	}

	//Stopping audio
	public void stopAudio (int audioSourceIndex) {

		//Getting audiosource(s)
		var audio = GetComponents<AudioSource>();

		//stopping audio
		if (audio.Length > audioSourceIndex) {
			audio[audioSourceIndex].Stop();
		} else {
			Debug.Log ("Invalid source index " + audioSourceIndex.ToString());
		}
		
	}

	public void pauseAudio (int audioSourceIndex) {
		//Getting audiosource(s)
		var audio = GetComponents<AudioSource>();

		//pausing audio
		if (audio.Length > audioSourceIndex) {
			audio[audioSourceIndex].Pause();
		} else {
			Debug.Log ("Invalid source index " + audioSourceIndex.ToString());
		}
	}
	
	//This function sets character spawnpoint
	public void setSpawn(InstanceID characterId, GameObject spawnPoint)
	{
		//Getting character
		int characterIndex = findBattleManagerCharactersIndexById (characterId);
		characterInfo character = BattleManager.core.characterInstances[characterIndex];

		//Setting animation
		character.spawnPointObject = spawnPoint;
	}
	
	//Destroying a gameObject after a specified amount of time
	public IEnumerator destroyAfterTime (GameObject g, float time) {
		yield return new WaitForSeconds (time);
		Destroy(g);
	}

	//This function makes one gameObject follow another.
	public IEnumerator follow (GameObject sourceObject, GameObject targetObject, float xAdjustment, float yAdjustment) {

		while (sourceObject != null && targetObject != null) {
			Vector3 temp1 = targetObject.transform.position;
			temp1 = new Vector3 (temp1.x + xAdjustment, temp1.y + yAdjustment, sourceObject.transform.position.z);
			
			// Get the rect transform
			var followUI = sourceObject.GetComponent<RectTransform>();
			if ( followUI != null )
			{
				var canvas = sourceObject.GetComponentInParent<Canvas>();
				var canvasRect = canvas.GetComponentInParent<RectTransform>();
				var camera = Camera.main;
				Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint( camera, temp1 );
				Vector2 result;
				RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRect, screenPoint,
					canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera, out result );

				sourceObject.transform.position = canvas.transform.TransformPoint( result );
			}
			else
			{
				sourceObject.transform.position = temp1;
			}

			yield return new WaitForEndOfFrame();
		}
	}

	//This function is used to display on-screen values such as damage
	public IEnumerator displayAttributeValue ( GameObject target, float value, int type, float xAdjustment, float yAdjustment )
	{
		yield return displayValue( target, value.ToString(), displayColor(0, type), displayIcon(0, type, displayColor(0, type)), xAdjustment, yAdjustment );
	}
	public IEnumerator displayBattleValue ( GameObject target, float value, int type, float xAdjustment, float yAdjustment )
	{
		yield return displayValue( target, value.ToString(), displayColor(1, type), displayIcon(1, type, displayColor(1, type)), xAdjustment, yAdjustment );
	}

	public IEnumerator displayValue (GameObject target, string value, string color, string icon, float xAdjustment, float yAdjustment) {

		//Getting coordinates
		Vector3 coordinates = target.transform.position;
		
		//Getting directionality
		Vector3 scale = target.transform.lossyScale;
		
		//Make x adjustment relative to direction faced
		xAdjustment *= -Mathf.Sign(scale.x);

		//Adjusted coordinates
		Vector3 newCoordinates = new Vector3 (coordinates.x + xAdjustment, coordinates.y + yAdjustment, coordinates.z);

		//Getting UI body element
		GameObject body = ObjectDB.core.battleUIBody;

		//Spawning Object
		GameObject g = Instantiate (ObjectDB.core.battleUIPopupPrefab, newCoordinates, Quaternion.identity, body.transform);

		uiCoordinateCheck( g, newCoordinates );
		var tmp = g.GetComponentInChildren<TextMeshProUGUI>();
		
		//Setting text
		tmp.text = value.ToString();

		//Adding value type
		tmp.text += icon;
		
		//Setting color
		tmp.text = $"<color=#{color}>{tmp.text}</color>";

		//Making value follow target
		StartCoroutine(follow (g, target, xAdjustment, yAdjustment));

		yield return new WaitForSeconds (1);

		Destroy (g);

	}

	public string displayColor(int displayType, int typeIndex)
	{
		string textColorString = "FF0000";	//Temp default to red
		switch (displayType)
		{
			case 0:		//Attributes
				switch (typeIndex)
				{
					case 0:		//HP
						textColorString = "00FF00";
						break;
					case 1:		//MP
						textColorString = "0000FF";
						break;
					default:
						textColorString = "FFFFFF";
						break;
				}
				break;
			case 1:		//Attacks
				switch (typeIndex)
				{
					case 0:
						break;
					case 2:
						textColorString = "64FFDE";
						break;
					case 3:
						textColorString = "921DCD";
						break;
					case 4:
						textColorString = "F9FF33";
						break;
					default:
						break;
				}
				break;
		}
		return textColorString;
	}

	public string displayIcon(int displayType, int typeIndex, string color)
	{
		string valueType = "";
		
		switch (displayType)
		{
			case 0:		//Attributes
				switch (typeIndex)
				{
					case 0:		//HP
						valueType = $"<sprite name=\"healthIcon\" tint=1>";
						break;
					case 1:		//MP
						break;
					default:
						break;
				}
				break;
			case 1:		//Attacks
				switch (typeIndex)
				{
					case 0:
						valueType = $"<sprite name=\"attackIcon\" tint=1>";
						break;
					case 2:
						valueType = $"<sprite name=\"timeIcon\" tint=1>";
						break;
					case 3:
						valueType = $"<sprite name=\"gravityIcon\" tint=1>";
						break;
					case 4: 
						valueType = $"<sprite name=\"lightIcon\" tint=1>";
						break;
					default:
						break;
				}
				break;
		}

		return valueType;
	}

	public static void uiCoordinateCheck(GameObject sourceObject, Vector3 worldPosition )
	{
		if ( sourceObject.GetComponent<RectTransform>() )
		{
			var canvas = sourceObject.GetComponentInParent<Canvas>();
			var canvasRect = canvas.GetComponentInParent<RectTransform>();
			var camera = Camera.main;
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint( camera, worldPosition );
			Vector2 result;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRect, screenPoint,
				canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera, out result );

			sourceObject.transform.position = canvas.transform.TransformPoint( result );
		}
		else
		{
			sourceObject.transform.position = worldPosition;
		}
	}

	void Awake () { if (core == null) { core = this; } }

}


//(c) Cination - Tsenkilidis Alexandros