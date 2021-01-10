LIST chars = Therapist, Model, Rockstar 
VAR CurrChar = Therapist
VAR Deaths = 0

->Start
==	Start
T,1,1;Let them take the first shot.
M,1,1;Can't spend energy on this trash.
R,1,1;I can't hold back. I'm all 11.
->DONE

==	CounterAll
{CurrChar},1,1;{~Just you try.|We are stronger than this.|I have just a response for everything they've got.|Double dare you.}
->DONE

==	Heal
{CurrChar == "T":T,1,1;Refreshing.}
{CurrChar == "P":P,1,1;This wine is the shit.}
{CurrChar == "R":R,1,1;Mmm beer.}
->DONE

==	Attack
{CurrChar},1,1;{~Fuck you.|This is what you get.|Hurts right?|There is more whence this came.}
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