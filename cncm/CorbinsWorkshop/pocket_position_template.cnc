IF #A == 1 THEN GOTO 100 ; X/Y position in front of forks as the target starting location
IF #A == 2 THEN GOTO 200 ; X/Y position with clearance (ie: the position outside the forks)
IF #A == 3 THEN GOTO 300 ; Z Position of fork
IF #A == 4 THEN GOTO 400 ; X/Y position of the fork
IF #A == 5 THEN GOTO 500 ; Z Bump position of fork (slightly above it)

N100 ;X/Y Position in Front of Forks
G53 X<XPOS_FRONT> Y<YPOS_FRONT> <SPEED>
GOTO 1000

N200 ;X/Y position with clearance
G53 X<XPOS_CLEAR> Y<YPOS_CLEAR> <SPEED>
GOTO 1000

N300 ; Z Position of fork
G53 Z<ZPOS> <SPEED>
GOTO 1000

N400 ; X/Y position of the fork 
G53 X<XPOS> Y<YPOS> <SPEED>
GOTO 1000

N500 ; Z Bump position
G53 Z<ZPOS_BUMP> <SPEED>
GOTO 1000


N1000