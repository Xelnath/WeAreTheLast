LIST chars = Therapist, Model, Rockstar 
VAR CurrChar = Therapist
VAR Deaths = 0

VAR Th = "0" // Therapist
VAR Mo = "1" // Model
VAR Ro = "2" // Rockstar
VAR Panic = "100"
VAR Trauma = "110"
VAR Meltdown = "120"

->Start
==	Start
{Th},1,1;Let them take the first shot.
{Mo},1,1;Can't spend energy on this trash.
{Ro},1,1;I can't hold back. I'm all 11.
->DONE

== Death_Therapist
{Th},1,1;{~Not like this.|Not again.|I've done everything right.|Why have I even bothered.} 
->DONE

== Death_Model
{Mo},1,1;{~How could you?|My face.|I felt something.|You'll regret this.} 
->DONE

== Death_Rockstar
{Ro},1,1;{~Can't kill legends.|You've made world bland.|I knew this was coming.|Good lord is taking me away.} 
->DONE



==	Heal
{CurrChar == Th:{Th},1,1;Refreshing.}
{CurrChar == Mo:{Mo},1,1;This wine is the shit.}
{CurrChar == Ro:{Ro},1,1;Mmm beer.}
->DONE

==	Attack
{CurrChar},1,1;{~Fuck you.|This is what you get.|Hurts right?|There is more whence this came.}
->DONE


//SkillBarks
//Therapist
==	CounterAll
{Th},1,1;{~Just you try.|We are stronger than this.|I have just a response for everything they've got.|Double dare you.}
->DONE

==	Counter
{Th},1,1;{~Pathetic.|No, YOU get hurt.}
->DONE

== Defend
{Th},1,1;DEFEND BARK
->DONE

== Cleanse
{Th},1,1;Cleanse BARK
->DONE

//Model

== Redirect
{Mo},1,1;REDIRECT BARK
->DONE

== PoisonMind
{Mo},1,1;PoisonMind BARK
->DONE

== MindControl
{Mo},1,1;MindControl BARK
->DONE

== GravitySqueeze
{Mo},1,1;GravitySqueeze BARK
->DONE

== StunRage
{Mo},1,1;StunRage BARK
->DONE

//Rockstar

== Lightshow
{Ro},1,1;Lightshow BARK
->DONE

== Dazzle
{Ro},1,1;Dazzle BARK
->DONE

== Regenerate
{Ro},1,1;Regenerate BARK
->DONE

== Wardrum
{Ro},1,1;Wardrum BARK
->DONE

//Unsorted

== Entropy
->DONE

== DarkBrilliance
->DONE

== GravitationalFlux
->DONE

== SelfDoubt
->DONE

== Purge
->DONE

== Fixation
->DONE

== HealHurt
->DONE

== Domination
->DONE

== ParalyzeAll
->DONE

== Doomsayer
->DONE

//Sacrifice SKill Reaction
//Therapist

==	Sacrifice_CounterAll
{Th},1,1;Sacrifice_CounterAll BARK
->DONE

== Sacrifice_Defend
{Th},1,1;Sacrifice_Defend BARK
->DONE

== Sacrifice_Cleanse
{Th},1,1;Sacrifice_Cleanse BARK
->DONE

//Model

== Sacrifice_Redirect
{Mo},1,1;Sacrifice_Redirect BARK
->DONE

== Sacrifice_PoisonMind
{Mo},1,1;Sacrifice_PoisonMind BARK
->DONE

== Sacrifice_GravitySqueeze
{Mo},1,1;Sacrifice_GravitySqueeze BARK
->DONE

//Rockstar

== Sacrifice_Lightshow
{Ro},1,1;Sacrifice_Lightshow BARK
->DONE

== Sacrifice_Regenerate
{Ro},1,1;Sacrifice_Regenerate BARK
->DONE

== Sacrifice_Wardrum
{Ro},1,1;Sacrifice_Wardrum BARK
->DONE

== Sacrifice_Dazzle
{Ro},1,1;Sacrifice_Dazzle BARK
->DONE

//Unsorted


==	Sacrifice_Heal
{CurrChar == Th:T,1,1;Refreshing.}
{CurrChar == Mo:P,1,1;This wine is the shit.}
{CurrChar == Ro:R,1,1;Mmm beer.}
->DONE

== Sacrifice_Entropy
->DONE

== Sacrifice_DarkBrilliance
->DONE

== Sacrifice_GravitationalFlux
->DONE

== Sacrifice_SelfDoubt
->DONE



== Sacrifice_Purge
->DONE

== Sacrifice_Fixation
->DONE

== Sacrifice_HealHurt
->DONE

== Sacrifice_Domination
->DONE

== Sacrifice_ParalyzeAll
->DONE

== Sacrifice_Doomsayer
->DONE

== Sacrifice_StunRage

->DONE

== Wave_Test
{CurrChar},1,1;They persist...
{Ro},1,1:How many more of these fuckers are there?
->DONE

==	FinalScreenTiff
{	
	-Deaths == 1:Oh no. One more reality is consumed. Don't worry there are infinite universes. All it takes is to succeed in one.
	-Deaths == 2:Keep going. I don't need to remind you of my love. You're special and you can do this.
	-Deaths == 3:Rise up, stand up, head over heart, heart over stomach. Now get in there and smash them.
	-Deaths == 4:Just don't waver. Their plan is flawed. Just one victory is all we need and you have infinite tries.
	-Deaths == 5:This is getting boring. Can you, like, fight better? Sure it's dire but it doesn't bear repeating.
	-Deaths == 6:I'm officially bored. This is a neverending nightmare. How does that make you feel?
	-else:I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE