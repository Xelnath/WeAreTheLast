using UnityEngine;
using UnityEditor;
using Ares.ActorComponents;

namespace Ares.Development {
	public class ActorAddComponent : MonoBehaviour {
		class ActorFullStackMenuItems {
			[MenuItem("Component/Ares/Player Actor + Actor Components", false, -21)]
			static void AddFullStackPlayerActor(MenuCommand command){
				foreach(GameObject gameObject in Selection.gameObjects){
					Undo.AddComponent<PlayerActor>(gameObject);
					AddActorComponents(gameObject);
				}
			}

			[MenuItem("Component/Ares/AI Actor + Actor Components", false, -20)]
			static void AddFullStackAIActor(MenuCommand command){
				Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Add AI Actor + Actor Components");

				foreach(GameObject gameObject in Selection.gameObjects){
					Undo.AddComponent<AIActor>(gameObject);
					AddActorComponents(gameObject);
				}
			}

			static void AddActorComponents(GameObject gameObject){
				Undo.AddComponent<ActorAnimation>(gameObject);
				Undo.AddComponent<ActorInstantiation>(gameObject);
				Undo.AddComponent<ActorAudio>(gameObject);
			}
		}
	}
}