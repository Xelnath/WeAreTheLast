using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Ares {
	[System.Serializable]
	public class BattleInteractorModifier {
		enum BlockCondition {Never, IfFilteredActionFirstInChain, IfFilteredActionPresentInChain}
		enum ModifierTarget {Setter, SetterGroup, OtherGroups, All}

		public ChainEvaluator.ActionType Type {get{return type;}}

		[SerializeField] ChainEvaluator.ActionType type;
		[SerializeField] ModifierTarget target;
		[SerializeField] string modificationFormula;
		[SerializeField] bool affectsAbilities;
		[SerializeField] bool affectsItems;
		[SerializeField] bool affectsAfflictions;
		[SerializeField] BlockCondition blockCondition;

		bool AppliesToGroup(Actor actor, Actor setter){
			switch(target){
				case ModifierTarget.Setter:			return actor == setter;
				case ModifierTarget.SetterGroup:	return actor.Group == setter.Group;
				case ModifierTarget.OtherGroups:	return actor.Group != setter.Group;
				case ModifierTarget.All:			return true;
			}

			return false;
		}

		public bool IsBlocked(Ability ability, Actor caster, Actor setter){
			if(blockCondition == BlockCondition.Never || !affectsAbilities || !AppliesToGroup(caster, setter)){
				return false; 
			}

			return IsBlocked(ability.Data.Actions.Cast<ChainableAction>().ToList());
		}

		public bool IsBlocked(Item item, Actor caster, Actor setter){
			if(blockCondition == BlockCondition.Never || !affectsItems || !AppliesToGroup(caster, setter)){
				return false; 
			}

			return IsBlocked(item.Data.Actions.Cast<ChainableAction>().ToList());
		}

		public bool IsBlocked(Affliction affliction, Actor caster, Actor setter){
			if(blockCondition == BlockCondition.Never || !affectsAfflictions || !AppliesToGroup(caster, setter)){
				return false; 
			}

			return IsBlocked(affliction.Data.Actions.Cast<ChainableAction>().ToList());
		}

		public bool IsBlocked(List<ChainableAction> actions){
			switch(blockCondition){
				case BlockCondition.IfFilteredActionFirstInChain:
					return actions[0].Action == type;
				case BlockCondition.IfFilteredActionPresentInChain:
					return actions.Any(a => a.Action == type);
				default:
					return false;
			}
		}

		public bool CanModify(object evaluater, ChainableAction action, Actor caster, Actor setter){
			if(!AppliesToGroup(caster, setter) || action.Action != Type){
				return false; 
			}

			if((evaluater is Ability && !affectsAbilities) || (evaluater is Item && !affectsItems) || (evaluater is Affliction && !affectsAfflictions)){
				return false;
			}

			return true;
		}

		public float ModifyPower(float power){
			return FormulaParser.Parse(modificationFormula.Replace("POWER", power.ToString()));
		}
	}
}