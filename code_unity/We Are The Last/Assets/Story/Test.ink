LIST chars = Therapist, Model, Rockstar 
VAR CurrChar = Therapist

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
I'm Tiffany and you're a pice of shit.
->DONE