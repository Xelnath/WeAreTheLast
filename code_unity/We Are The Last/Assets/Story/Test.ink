VAR CurrChar = "None"

->Start
==	Start
T,1,1;Let them take the first shot.
M,1,1;Can't spend energy on this trash.
R,1,1;I can't hold back. I'm all 11.
->DONE

==	CounterAll
{CurrChar},1,1;{~Just you try.|We are stronger than this.|I have just a response for everything they've got.|Double dare you.}
->DONE