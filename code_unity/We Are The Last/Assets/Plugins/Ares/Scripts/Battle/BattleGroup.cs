using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

namespace Ares {
	public class BattleGroup{
		public int Id {get; private set;}
		public string Name {get; private set;}
		public Inventory Inventory {get; private set;}
		public UnityEvent OnDefeat {get; private set;}
		public List<Actor> Actors {get{return battle.ActorInfo.Where(infoKV => infoKV.Value.Group.Id == Id).Select(infoKV => infoKV.Value.Actor).ToList();}} //all, including defeated

		static int idCount;
		Battle battle; //hold a reference back for convenience getters

		public BattleGroup(Battle battle, string name) : this(battle, name, null){}

		public BattleGroup(Battle battle, string name, Inventory inventory){
			Id = idCount++;
			Name = name;
			Inventory = inventory;
			OnDefeat = new UnityEvent();

			this.battle = battle;
		}

		public void AddActor(Actor actor, bool isParticipating){
			battle.AddActor(actor, this, isParticipating);

			actor.OnHPDeplete.AddListener(CheckDefeat);
		}

		void CheckDefeat(){
			if(Actors.All(a => a.HP <= 0)){
				OnDefeat.Invoke();
			}
		}
	}
}