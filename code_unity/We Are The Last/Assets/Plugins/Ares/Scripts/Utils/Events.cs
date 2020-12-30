using UnityEngine.Events;

namespace Ares {
	public class AbilityEvent : UnityEvent<Ability> {}
	public class Ability_BoolEvent : UnityEvent<Ability, bool> {}
	public class Ability_ActorsEvent : UnityEvent<Ability, Actor[]> {}
	public class Ability_StringEvent : UnityEvent<Ability, string> {}
	public class Ability_Int_StringEvent : UnityEvent<Ability, int, string> {}
	public class AbilityResultsEvent : UnityEvent<Actor, AbilityResults> {}

	public class ActorEvent : UnityEvent<Actor> {}
	public class Actor_BoolEvent : UnityEvent<Actor, bool> {}
	public class Actor_Ability_AbilityActionEvent : UnityEvent<Actor, Ability, AbilityAction> {}
	public class Actor_ActionInputEvent : UnityEvent<Actor, ActionInput> {}
	public class Actor_SingleTargetInputEvent : UnityEvent<Actor, TargetInputSingleActor> {}
	public class Actor_NumActorsTargetInputEvent : UnityEvent<Actor, TargetInputNumActors> {}
	public class Actor_GroupTargetInputEvent : UnityEvent<Actor, TargetInputGroup> {}
	public class Actor_Item_ItemActionEvent : UnityEvent<Actor, Item, ItemAction> {}

	public class AfflictionResultsEvent : UnityEvent<Actor, AfflictionResults> {}
	public class Actors_Ability_AbilityActionEvent : UnityEvent<Actor[], Ability, AbilityAction> {}
	public class Actors_Item_ItemActionEvent : UnityEvent<Actor[], Item, ItemAction> {}

	public class Affliction_AfflictionActionEvent : UnityEvent<Affliction, AfflictionAction> {}
	public class AfflictionEvent : UnityEvent<Affliction> {}
	public class Affliction_IntEvent : UnityEvent<Affliction, int> {}
	
	public class EndReasonEvent : UnityEvent<Battle.EndReason> {}

	public class EnvironmentVariableEvent : UnityEvent<EnvironmentVariable> {}
	public class EnvironmentVariable_IntEvent : UnityEvent<EnvironmentVariable, int> {}

	public class IntEvent : UnityEvent<int> {}

	public class ItemEvent : UnityEvent<Item> {}
	public class Item_BoolEvent : UnityEvent<Item, bool> {}
	public class Item_ActorsEvent : UnityEvent<Item, Actor[]> {}
	public class Item_IntEvent : UnityEvent<Item, int> {}
	public class Item_StringEvent : UnityEvent<Item, string> {}
	public class Item_Int_StringEvent : UnityEvent<Item, int, string> {}
	public class ItemResultsEvent : UnityEvent<Actor, ItemResults> {}

	public class RoundStateEvent : UnityEvent<Battle.RoundState> {}

	public class Stat_IntEvent : UnityEvent<Stat, int> {}
}