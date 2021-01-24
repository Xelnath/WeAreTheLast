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
{	Deaths:
-0:->First
-1:->DONE
-2:->DONE
-3:->CluelessFirst
-4:->Clueless
-5:->Clueless
-6:->DespairFirst
-7:->Clueless
-8:->Clueless
-9:->Despair
-10:->Despair
-11:->Despair
-else:->Despair
}

=	First
{Th},1,1;Let them take the first shot.
{Mo},1,1;Can't spend energy on this trash.
{Ro},1,1;I can't hold back. I'm all 11.
->DONE

=	CluelessFirst
{Mo},1,1;Do we need to do this?
{Th},1,1;We have commitments.
{Ro},1,1;There is no stopping me.
->DONE

=	Clueless
{stopping:
	-{Mo},1,1;I need a spa.
	-{Ro},1,1;Let's rev it up a notch!
	-{Th},1,1;It was a lie. Must have been.
	-{Th},1,1;Reverse psychology or some shit. Gotta be.
}
->DONE

=	DespairFirst
{Ro},1,1;The hell she said?
{Mo},1,1;I don't know how to take it.
{Th},1,1;It must have been some clue. We should listen.
->DONE

=	Despair
{stopping:
	-{Th},1,1;The bitch didn't lie.
	-{Ro},1,1;If this is a fucking private concert, I swear.
	-{Th},1,1;This has to stop. We can't go on like this.
}
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
{Th},1,1;{~Just you try.|We are stronger than this.|I have just a response for everything they've got.|Double dare you.||}
->DONE

==	Counter
{Th},1,1;{~This is your best effort?|How does your own medicine taste?||}
->DONE

== Defend
{Th},1,1;{~You got this.|We talked about it, I got you.||}
->DONE

== Cleanse
{Th},1,1;{~Do your affirmations, you'll be fine.|Think again.||}
->DONE

//Model

== Redirect
{Mo},1,1;{~You ain't hitting that.|Think again.||}
->DONE

== PoisonMind
{Mo},1,1;{~Are you sure this works for you?|This is bullshit. Why are you among them?||}
->DONE

== MindControl
{Mo},1,1;{~Just follow this face.|You can't turn away anyway, can you?||}
->DONE

== GravitySqueeze
{Mo},1,1;{~This look makes everybody weak in the knees.|Did you feel the gravity of going against me?||}
->DONE

== StunRage
{Mo},1,1;StunRage BARK
->DONE

//Rockstar

== Lightshow
{Ro},1,1;{~Too much for ya huh?|Yes, it is me. Too overwhelming?||}
->DONE

== Dazzle
{Ro},1,1;{~Look at me, lose sight.|Many people ahve went blind from my pyro.||}
->DONE

== Regenerate
{Ro},1,1;{~Nobody is quitting till the song is over.|Power through, the afterparty is always worth it.||}
->DONE

== Wardrum
{Ro},1,1;{~I call this one "War Drums".|YES YOU FUCKERS LET'S GOOOO||}
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


==	FinalScreenTiff
{	Deaths:
	-1:Oh no. One more reality is consumed. Don't worry there are infinite universes. All it takes is to succeed in one.
	-2:Keep going. I don't need to remind you of my love. You're special and you can do this.
	-3:Rise up, stand up, head over heart, heart over stomach. Now get in there and smash them.
	-4:Just don't waver. Their plan is flawed. Just one victory is all we need and you have infinite tries.
	-5:This is getting boring. Can you, like, fight better? Sure it's dire but it doesn't bear repeating.
	-6:I'm officially bored. This is a neverending nightmare. How does that make you feel?
	-7:Oh my god, omigod, you guys. You died again like maggots in a vacuum pump.
	-8:What's up, last three people still with their brains in a solid state?
	-9:Let's try again. I'm a nice and kind boring bitch again. You'll do fine.
	-10:Is this really how you tried to kill me the first time? Phenomenal arrogance.
	-11:If you don't entertain me, I think I'll just reset your memories.
	-else:I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE