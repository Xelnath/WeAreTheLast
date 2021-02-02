LIST chars = Therapist, Model, Rockstar 
VAR CurrChar = Therapist
VAR Deaths = 0
VAR Resets = 0
VAR Victory = false

VAR Th = "0" // Therapist
VAR Mo = "1" // Model
VAR Ro = "2" // Rockstar
VAR Panic = "100"
VAR Trauma = "110"
VAR Meltdown = "120"

==	Start
{Resets>=3:->EndStart}
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

==	EndStart
{	Deaths:
-0:->First
-1:->CluelessFirst
-else:->Clueless
}
=	First
{Th},1,1;SHE'S PLAYING US
{Mo},1,1;THIS CAN'T BE HAPPENING
{Ro},1,1;IT'S A NIGHTMARE
->DONE

=	CluelessFirst
{Mo},1,1;We'll kill her.
{Th},1,1;It's the only way.
{Ro},1,1;Whatever it takes.
->DONE

=	Clueless
{shuffle:
	-{Mo},1,1;Positivity is pointless now.
	-{Mo},1,1;I refuse to perish in ignominy.
	-{Mo},1,1;I will poison her world.
	-{Ro},1,1;Swan song it is. Again.
	-{Ro},1,1;Metal will live on!
	-{Ro},1,1;I'm not dying alone!
	-{Th},1,1;We're staying strong. She doesn't get to win.
	-{Th},1,1;I've taught you how to stay the course. We're doing it now.
	-{Th},1,1;It's hard for me to. We must stay the course. We can't stop.
	-{Mo},1,1;
	-{Mo},1,1;
	-{Mo},1,1;
}
->DONE

==	FinalScreenTiff
{	Resets:
	-0:->ResetReality
	-1:->ResetTime
	-2:->ResetWhatever
	-3:->ResetVersionControl
	-else:->ResetBored
}

==	ResetReality
{	Deaths:
	-1:One more reality is consumed. Don't worry there are infinite universes. All it takes is to succeed in one.
	-2:Keep going. I don't need to remind you of my love. You're special and you can do this.
	-3:Rise up, stand up, head over heart, heart over stomach. Now get in there and smash them.
	-4:Just don't waver. Their plan is flawed. Just one victory is all we need and you have infinite tries.
	-5:This is getting boring. Can you, like, fight better? Sure it's dire but it doesn't bear repeating.
	-6:I'm officially bored. This is a neverending nightmare. How does that make you feel?
	-7:Oh my god, omigod, you guys. You died again like maggots in a vacuum pump.
	-8:What's up, last three people on Earth still with their brains in a solid state?
	-9:Let's try again. I'm a nice and kind boring bitch again. You'll do fine.
	-10:Is this really how you tried to kill me that first original time? Phenomenal arrogance.
	-else: Reset I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE

==	ResetTime
{	Deaths:
	-0:I think I'll just reset your memories. Yeah, that should be fun. Let's do it.
	-1:Don't worry! I'm turning back time right now. Go get'em darlings!
	-2:They have no idea how powerful my time travel is. Go back. Take it all back.
	-3:Learn from your mistakes. You can try as many times as you can. They are always on the first try.
	-4:Bad guys need to win everytime! Good guys need to win just once. Go get it.
	-5:Rise up, stand up, head over heart, heart over stomach. Now get in there and smash them.
	-6:Oh shit I've said that head over heart thing already. Oh well. Enjoying your torture yet?
	-7:Work with me here. Let's workshop something that's fun for me. It's like we're going in circles.
	-8:Circles maybe? You like circles? Oh fuck you gotta love the circles. Can't do shit with circles.
	-9:I'm thinking. Just go at it again. Shoosh.
	-10:I think it's another reset. You know what, I'm feeling magnanimous. Go at it again.
	-else: ResetTime I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE
==	ResetWhatever
{	Deaths:
	-0:You're pushing my buttons. Like, you really are. Let's have a do over.
	-1:You're immortal now! Yes, I've made you immortal! You die and you come back and you do it again!
	-2:You'll never die, you'll never relent! I have faith in you!
	-3:The enemy is tough but you're getting tougher every run! Victory is inevitable!
	-4:Learn from this mistake. Like really try. C'mon. Try!
	-5:I though this whole grab the last humans and make them immortal would be fun.
	-6:Yeah, like, whatever.
	-7:You died again? Jeez, color me surprised.
	-8:...
	-9:...
	-10:I've got an idea. Let me get back to you.
	-else: ResetWhatever I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE
==	ResetVersionControl
{	Deaths:
	-0:I'm giving you all of your memories back. All your deaths and failures. Enjoy.
	-1:Yes, finally! Fun!
	-2:Yes, resentment! A two way street, yes. YES! 
	-3:I hate you so much. Your whole deal, so despicable. Who knew honesty feels so awesome. 
	-4:You did kill a lot of my goons. All of that was real. I just have an infinite amount of them.
	-5:This game is rigged. You don't get to win. Not ever. Even if I'm on the battlements, I can only lose control over you. That means you die.
	-6:I like that you still persevere. I don't need to alter your brains and make you do stuff. You just march and die all on your own.
	-7:Can you even remember how it all begun? I sure don't. It's been so long ago. So mundane now. 
	-8:Did you create me? Summon me? Did I turn on you? You're a hateful bunch I see no problem with that hypothesis. That's right. I remember now. That makes sense!
	-9:You're my curse you know that. I've saved the last ones and everything but you bore me so much. And yet without you I'd have nothing.
	-10:I'm going to put you on autopilot. It's so pedestrian again. I'll pop back from time to time don't worry.
	-else: ResetVersionControl I'm Tiff and whatever happened is deveveloper's fault. Ergo, it's a bug.
}
->DONE
==	ResetBored
{shuffle:
	-	Yeah Yeah. You died again. Shocker.
	-	Still kicking? Good for you.
	-	Yes, yes. I was definitely paying attention.
	-	What's that? Oh, you died again. Cool.
	-	Should I reset you again? Nah, it's a hassle.
	-	The returns sure are diminishing.
	-	You're no longer fun. I just want you to know that.
	-	Everyone who you love is still dead. Just saying.
	-	You will never die. Enjoy.
	-	...
	-	...
	-	...
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

== Intervene
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

== Sacrifice_Intervene
{Th},1,1;Sacrifice_Intervene BARK
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
->DONE

== Wave_1

->DONE

== Wave_2
{Th},1,1;This is a nasty beast. Not sure I can counter that.
->DONE

== Wave_3
{Ro},1,1;No need to panic. Same shit.
->DONE

== Wave_4
{Mo},1,1;Smells familiar.
->DONE

== Wave_5
{Mo},1,1;Now this I know very well.
->DONE

== Wave_6
{Th},1,1;Usual suspect, huh?
->DONE

== Wave_7
{Th},1,1;Be careful this combo has claimed millions.
->DONE

== Wave_8
{Ro},1,1;I feel called out.
->DONE

== Wave_9
{Ro},1,1;Now this is personal. No worries, I've dealt with this in the past.
->DONE

== Wave_10
{Th},1,1;TBD BOSS ONE
->DONE

== Wave_11
{Th},1,1;This is too much trauma to deal with for free.
->DONE

== Wave_12
{Ro},1,1;Are they Lennoning me?
->DONE

== Wave_13
{Ro},1,1;They are double Lennoning me!
->DONE

== Wave_14
{Ro},1,1;I feel good about this. I feel right. I feel wrath!
->DONE

== Wave_15
{Mo},1,1;This isn't us. Never us.
->DONE

== Wave_16
{Mo},1,1;This was me. This will never be me again.
->DONE

== Wave_17
{Th},1,1;Careful. Stress is the worst deceiver.
->DONE

== Wave_18
{Th},1,1;These one's are called rockstar killers.
{Ro},1,1;What's their play?
{Th},1,1;You're their prey.
->DONE

== Wave_19
{Ro},1,1;Look at that. Therapy breakers.
{Th},1,1;Uncalled for. Don't turn on each other.
->DONE

== Wave_20
{Mo},1,1;TBD BOSS ONE
->DONE

== Wave_21
{Mo},1,1;Overwhelming vanity? That's what I call my mirror.
->DONE

== Wave_22
{Mo},1,1;NOOOOOOOOOOOOO
{Ro},1,1;TOOOO MUCH
{Th},1,1;Stop! Breathe! Fight!
->DONE

== Wave_23
{Th},1,1;Years of therapy undone in a second.
->DONE

== Wave_24
{Th},1,1;Fuck.
{Ro},1,1;She's rattled. Not good.
->DONE

== Wave_25
{Mo},1,1;I hate it. All of it.
->DONE

== Wave_26
{Mo},1,1;Too much. It's too much. It's coming back. All of it.
{Th},1,1;Resist. For all you care. Resist.
->DONE

== Wave_27
{Th},1,1;These monsters must die. They must go.
{Mo},1,1;Send them down the runway to hell.
{Ro},1,1;That's our concerto.
->DONE

== Wave_28
{Th},1,1;I won't take this no more!
->DONE

== Wave_29
{Mo},1,1;A saint?
{Th},1,1;A lie about to die.
{Ro},1,1;Only LA has saints.
{Th},1,1;It's a song not a saying.
{Ro},1,1;Same difference.
->DONE

== Wave_30
{Ro},1,1;TBD BOSS THREE
->DONE

== Wave_31
{Ro},1,1;TBD TIFFANY!
->DONE


== Wave
{	Deaths:
	-1:{Ro},1,1;The audience these days, jeeez.
	-2:{Th},1,1;Blocking their bullshit is getting draining.
	-3:{Mo},1,1;Why they gotta be so ugly.
	-4:{Th},1,1;Just stay strong, people.
	-5:{Ro},1,1;I'm wasted on this rabble.
	-6:{Mo},1,1;The audience these days, jeeez.
	-7:{Ro},1,1;The audience these days, jeeez. 
}
->DONE



