/* A basic template for a manager script to set up and manage
 * a battle. It is entirely possible to split this script
 * up into multiple ones, seperating battle and actor creation, UI, etc.
 */

using UnityEngine;

namespace Ares.Examples {
	public class BattleManagerTemplate : MonoBehaviour {
		[SerializeField, Header("Battle")] BattleRules rules; // The rules and settings that the battle will adhere to.

		Battle battle; // A reference to the actual Battle object

		void Awake(){
			// Spawn all dynamic actors and set up their Actor* components here.
		}

		void Start(){
			// Set up battle
			battle = new Battle(rules);

			// Set up the required battle delegates and events
			battle.OnActorNeedsActionInput.AddListener(ShowActionInput);
			battle.OnActorNeedsSingleTargetInput.AddListener(ShowTargetInput);
			battle.OnActorNeedsActorsTargetInput.AddListener(ShowTargetInput);
			battle.OnActorNeedsGroupTargetInput.AddListener(ShowTargetInput);
			battle.OnActorHasGivenAllNeededInput.AddListener(HideInput);
			battle.OnBattleEnd.AddListener(OnBattleEnd);

			// Set up groups and win conditions
			BattleGroup group1 = battle.AddGroup("Group 1 Name");
			BattleGroup group2 = battle.AddGroup("Group 2 Name");

			group1.OnDefeat.AddListener(() => EndBattle(false));
			group2.OnDefeat.AddListener(() => EndBattle(true));

			// Add all actors to their respective groups

			// Start the battle and get it initialized
			battle.Start(true);

			// If we'd started the battle with `progressAutomatically = false`, we could wait a while here to open menus etc.
			// before manually progressing to the first round by calling `battle.ProgressBattle()`.
		}
			
		void ShowActionInput(Actor actor, ActionInput actionInput){
			// Set up and show the UI for selecting an actor's item or ability.

			// `actionInput.ValidAbilities` and `.ValidItems` are filtered lists of all abilities and items that can be used
			// given the current state of the battle.

			// `actor.Abilities` can be used to access all abilities.

			// The actor's full inventory can be accessed from either `actor.inventory`, `actor.Group.Inventory`
			// or both, depending on how your game works and which items you wish to show when.
			// These inventories can be filtered based on the `rules.ItemComsumptionMoment`.
			// Typically `OnRoundStart` and `OnTurn` moments would use the `Inventory.Filter.All` filter,
			// and `OnTurnButMarkPendingOnSelect` would use `Inventory.Filter.ExcludePending`.

			// To select an item or ability, call the respective callback method inside `actionInput`.
			// These callbacks will return a `success` bool.
		}

		void ShowItemInput(Actor actor, StackedItem[] items, ActionInput actionInput){
			// Set up and show the UI for selecting an item for the current actor to use.
			// When a target is selected, call `actionInput.ItemSelectCallback(chosenItem)`
		}

		void ShowTargetInput(Actor actor, TargetInputSingleActor targetInput){
			// Set up and show the UI for selecting the chosen action's target actor.
			// When a target is selected, call `actionInput.TargetSelectCallback(chosenActor)`.
			// This callback will return a `success` bool.
		}

		void ShowTargetInput(Actor actor, TargetInputNumActors targetInput){
			// Set up and show the UI for selecting the chosen action's target actors.
			// When a target is selected, call `actionInput.TargetSelectCallback(chosenActors)`.
			// This callback will return a `success` bool.
		}

		void ShowTargetInput(Actor actor, TargetInputGroup targetInput){
			// Set up and show the UI for selecting the chosen action's target group.
			// When a target is selected, call `actionInput.TargetSelectCallback(chosenBattleGroup)`.
			// This callback will return a `success` bool.
		}

		void HideInput(Actor actor){
			// Hide the UI now that the actor has received all needed input.
		}
			
		void EndBattle(bool playerWon){
			// A win condition has been met; end the battle.
			battle.EndBattle(Battle.EndReason.WinLoseConditionMet);

			// Show victory/ defeat animations and UI
		}

		void OnBattleEnd(Battle.EndReason endReason){
			if(endReason == Battle.EndReason.OutOfTurns){
				// Show tie screen or determine winner
			}
		}
	}
}