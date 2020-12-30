using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Ares.ActorComponents;

namespace Ares {
	public enum DelayType {None, Animation, Time}
	public enum InstantiationTargetMode {Transform, FindByName, LocalPosition, WorldPosition}
	public enum InstantiationTargetActor {Caster, Target}
	public enum AudioPlayPosition {Caster, Target, MainCamera}

	[System.Serializable]
	public abstract class EffectBase {
		public bool enabled;
	}

	[System.Serializable]
	public class AnimationEffect : EffectBase {
		public string ParameterName {get{return parameterName;}}
		public AnimatorControllerParameterType ParameterType {get{return parameterType;}}
		public bool ValueBool {get{return valueBool;}}
		public float ValueFloat {get{return valueFloat;}}
		public DelayType @DelayType {get{return delayType;}}
		public float DelayTime {get{return delayTime;}}
		public int DelayAnimationLayer {get{return delayAnimationLayer;}}

		[SerializeField] string parameterName;
		[SerializeField] AnimatorControllerParameterType parameterType;
		[SerializeField] bool valueBool = true;
		[SerializeField] float valueFloat = 1f; //also holds Int parameter values; simple cast
		[SerializeField] DelayType delayType;
		[SerializeField] float delayTime;
		[SerializeField] int delayAnimationLayer;

		static List<AudioSource> audioSources;

		public AnimationEffect(){
			Reset();
		}

		public void Reset(){
			parameterName = "";
			parameterType = AnimatorControllerParameterType.Trigger;
			valueBool = true;
			valueFloat = 1f;
			delayTime = 0f;
			delayAnimationLayer = 0;
		}

		public void SetAsTrigger(string parameterName){
			parameterType = AnimatorControllerParameterType.Trigger;
			this.parameterName = parameterName;
		}

		public void SetAsBool(string parameterName, bool value){
			parameterType = AnimatorControllerParameterType.Bool;
			this.parameterName = parameterName;
			this.valueBool = value;
		}

		public void SetAsInt(string parameterName, int value){
			parameterType = AnimatorControllerParameterType.Int;
			this.parameterName = parameterName;
			this.valueFloat = value;
		}

		public void SetAsFloat(string parameterName, float value){
			parameterType = AnimatorControllerParameterType.Float;
			this.parameterName = parameterName;
			this.valueFloat = value;
		}

		public void SetAsNoDelay(){
			delayType = DelayType.None;
		}

		public void SetAsTimeDelay(float delay){
			delayType = DelayType.Time;
			delayTime = delay;
		}

		public void SetAsAnimationDelay(int animationLayer){
			delayType = DelayType.Animation;
			delayAnimationLayer = animationLayer;
		}

		public void Trigger(Actor caster){
			ActorAnimation actorAnimation = caster.GetComponent<ActorAnimation>();

			if(actorAnimation != null){
				actorAnimation.ProcessEffect(caster, this);
			}
			else{
				Animator animator = caster.GetComponent<Animator>();

				switch(parameterType){
					case AnimatorControllerParameterType.Trigger:
						animator.SetTrigger(parameterName);
						break;
					case AnimatorControllerParameterType.Bool:
						animator.SetBool(parameterName, valueBool);
						break;
					case AnimatorControllerParameterType.Int:
						animator.SetInteger(parameterName, (int)valueFloat);
						break;
					case AnimatorControllerParameterType.Float:
						animator.SetFloat(parameterName, valueFloat);
						break;
				}
			}
		}
	}

	[System.Serializable]
	public class AudioEffect : EffectBase {
		[SerializeField] AudioClip clip;
		[SerializeField] AudioPlayPosition playPosition;
		[SerializeField, Range(0f, 1f)] float volume = 1f;
		[SerializeField] float delay;

		static List<AudioSource> audioSources;

		static AudioEffect(){
			audioSources = new List<AudioSource>();
		}

		static void Play(AudioClip clip, Vector3 position, float volume){
			AudioSource source = audioSources.FirstOrDefault(s => !s.isPlaying);

			if(source == null){
				source = new GameObject("ARES Audio Effect Player").AddComponent<AudioSource>();
				audioSources.Add(source);
			}

			source.transform.position = position;
			source.clip = clip;
			source.volume = volume;

			source.Play();
		}

		public static void DestroyAll(){
			foreach(AudioSource source in audioSources){
				Object.Destroy(source.gameObject);
			}
		}

		public AudioEffect(AudioClip clip, AudioPlayPosition playPosition, float volume, float delay = 0f){
			this.clip = clip;
			this.playPosition = playPosition;
			this.volume = volume;
			this.delay = delay;
		}

		public void Set(AudioClip clip, AudioPlayPosition playPosition, float volume, float delay){
			this.clip = clip;
			this.playPosition = playPosition;
			this.volume = volume;
			this.delay = delay;
		}

		public void Reset(){
			clip = null;
			playPosition = AudioPlayPosition.Caster;
			volume = 1f;
			delay = 0f;
		}

		public void Trigger(){
			Trigger(null, null);
		}

		public void Trigger(Actor caster, Actor[] targets){
			if(clip == null){
				return;
			}

			if(delay == 0f){
				Play(caster, targets);
			}
			else{
				BattleMonoBehaviour.Instance.StartCoroutine(CRPlayInTime(caster, targets));
			}
		}

		void Play(Actor caster, Actor[] targets){
			if(playPosition == AudioPlayPosition.Caster && caster != null){
				AudioSource.PlayClipAtPoint(clip, caster.transform.position, volume);
			}
			else if(playPosition == AudioPlayPosition.Target && targets != null && targets.Length > 0){
				if(targets.Length == 0){
					AudioSource.PlayClipAtPoint(clip, targets[0].transform.position, volume);
				}
				else{
					Vector3 targetPos = Vector3.zero;

					foreach(Actor target in targets){
						targetPos += target.transform.position;
					}

					targetPos /= targets.Length;

					AudioSource.PlayClipAtPoint(clip, targetPos, volume);
				}
			}
			else{
				AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
			}
		}
		
		IEnumerator CRPlayInTime(Actor caster, Actor[] targets){
			yield return new WaitForSeconds(delay);
			
			Play(caster, targets);
		}
	}

	[System.Serializable]
	public class InstantiationEffect : EffectBase {
		[SerializeField] InstantiationEffectInstance effect;
		[SerializeField] InstantiationTargetMode targetMode;
		[SerializeField] InstantiationTargetActor targetActor;
		[SerializeField] string targetName = "root";
		[SerializeField] Transform targetTransform;
		[SerializeField] bool parentToTarget = true;
		[SerializeField] bool inheritParentScale = false;
		[SerializeField] Vector3 offset;
		[SerializeField] Vector3 rotation;
		[SerializeField] Vector3 scale = Vector3.one;
		[SerializeField] float delay;

		#if UNITY_EDITOR
		public void NotifyTransformTargetNotSupported(){ //For ScriptableObjectss like Item and Ability
			targetMode = InstantiationTargetMode.FindByName;
		}
		#endif

		public InstantiationEffect(InstantiationEffectInstance effect, Transform spawnTarget, bool parentToTarget, Vector3 offset, Vector3 rotation, Vector3 scale, float delay=0f) :
		this(effect, offset, rotation, scale, delay){
			this.parentToTarget = parentToTarget;

			targetMode = InstantiationTargetMode.Transform;
			targetTransform = spawnTarget;

		}

		public InstantiationEffect(InstantiationEffectInstance effect, string spawnTargetName, InstantiationTargetActor spawnTargetActor, bool parentToTarget, Vector3 offset,
							Vector3 rotation, Vector3 scale, float delay=0f) : this(effect, offset, rotation, scale, delay){
			this.parentToTarget = parentToTarget;

			targetMode = InstantiationTargetMode.FindByName;
			targetActor = spawnTargetActor;
			targetName = spawnTargetName;
		}

		public InstantiationEffect(InstantiationEffectInstance effect, Vector3 spawnTarget, Space spawnTargetSpace, InstantiationTargetActor spawnTargetActor,
							Vector3 rotation, Vector3 scale, float delay=0f) : this(effect, spawnTarget, rotation, scale, delay){
			targetMode = spawnTargetSpace == Space.Self ? InstantiationTargetMode.LocalPosition : InstantiationTargetMode.WorldPosition;
			targetActor = spawnTargetActor;
		}

		InstantiationEffect(InstantiationEffectInstance effect, Vector3 offset, Vector3 rotation, Vector3 scale, float delay){
			this.effect = effect;
			this.offset = offset;
			this.rotation = rotation;
			this.scale = scale;
			this.delay = delay;
		}

		public void Reset(InstantiationTargetMode targetMode){
			this.targetMode = targetMode;
			effect = null;
			targetActor = InstantiationTargetActor.Caster;
			targetName = "";
			targetTransform = null;
			parentToTarget = false;
			offset = Vector3.zero;
			rotation = Vector3.zero;
			scale = Vector3.one;
			delay = 0f;

		}

		public void SetEffect(InstantiationEffectInstance effect, float delay){
			this.effect = effect;
			this.delay = delay;
		}

		public void SetTargetAsTransform(Transform target, Vector3 offset, Vector3 rotation, Vector3 scale, bool parentToTarget){
			targetMode = InstantiationTargetMode.Transform;
			targetTransform = target;

			SetTargetTransform(offset, rotation, scale, parentToTarget);
		}

		public void SetTargetAsName(string target, InstantiationTargetActor targetActor, Vector3 offset, Vector3 rotation, Vector3 scale, bool parentToTarget){
			this.targetActor = targetActor;
			targetMode = InstantiationTargetMode.FindByName;
			targetName = target;

			SetTargetTransform(offset, rotation, scale, parentToTarget);
		}

		public void SetTargetAsLocalCoordinates(InstantiationTargetActor targetActor, Vector3 offset, Vector3 rotation, Vector3 scale, bool parentToTarget){
			this.targetActor = targetActor;
			targetMode = InstantiationTargetMode.LocalPosition;

			SetTargetTransform(offset, rotation, scale, parentToTarget);
		}

		public void SetTargetAsWorldCoordinates(Vector3 offset, Vector3 rotation, Vector3 scale, bool parentToTarget){
			targetMode = InstantiationTargetMode.WorldPosition;

			SetTargetTransform(offset, rotation, scale, parentToTarget);
		}

		void SetTargetTransform(Vector3 offset, Vector3 rotation, Vector3 scale, bool parentToTarget){
			this.offset = offset;
			this.rotation = rotation;
			this.scale = scale;
			this.parentToTarget = parentToTarget;
		}

		public void Trigger<T>(Actor caster, Actor[] targets, T info){
			if(effect == null){
				return;
			}

			if(delay == 0f){
				InstantiationEffectInstance spawnedEffect = Instantiate(caster, targets);
				spawnedEffect.OnSpawned<T>(caster, targets, info);
			}
			else{
				BattleMonoBehaviour.Instance.StartCoroutine(CRInstantiateInTime(caster, targets, info));
			}
		}

		IEnumerator CRInstantiateInTime<T>(Actor caster, Actor[] targets, T info){
			yield return new WaitForSeconds(delay);

			InstantiationEffectInstance spawnedEffect = Instantiate(caster, targets);
			spawnedEffect.OnSpawned<T>(caster, targets, info);
		}

		InstantiationEffectInstance Instantiate(Actor caster, Actor[] targets){
			InstantiationEffectInstance spawnedEffect = null;

			switch(targetMode){
				case InstantiationTargetMode.Transform:
					spawnedEffect = SpawnEffect(targetTransform.position + offset);
					HandleParenting(spawnedEffect, targetTransform);
					break;
				case InstantiationTargetMode.FindByName:
					Transform foundTransform = null;

					if(targetActor == InstantiationTargetActor.Caster){
						foundTransform = TransformUtils.FindChild(caster.transform, targetName);
					}
					else{
						foreach(Actor target in targets){
							foundTransform = TransformUtils.FindChild(target.transform, targetName);

							if(foundTransform != null){
								break;
							}
						}
					}

					if(foundTransform == null){
						Debug.LogError(string.Format("The target {0} could not be found for the associated instantiation effect.", targetName));
						spawnedEffect = SpawnEffect(offset);
					}
					else{
						spawnedEffect = SpawnEffect(foundTransform.position + offset);
						HandleParenting(spawnedEffect, foundTransform);
					}
					break;
				case InstantiationTargetMode.LocalPosition:
					spawnedEffect = SpawnEffect((targetActor == InstantiationTargetActor.Caster ? caster : targets[0]).transform.TransformPoint(offset));
					break;
				case InstantiationTargetMode.WorldPosition:
					spawnedEffect = SpawnEffect (offset);
					break;
			}

			return spawnedEffect;
		}

		InstantiationEffectInstance SpawnEffect(Vector3 position){
			InstantiationEffectInstance instance = Object.Instantiate(effect, position, Quaternion.Euler(rotation));
			instance.transform.localScale = scale;

			return instance;
		}

		void HandleParenting(InstantiationEffectInstance effect, Transform parent){
			if(parentToTarget){
				effect.transform.SetParent(parent, true);

				if(inheritParentScale){
					effect.transform.localScale = scale;
				}
			}
		}
	}
}