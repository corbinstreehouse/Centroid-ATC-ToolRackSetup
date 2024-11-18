; goes to the touch plate, or stops if the user doesn't have it setup

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

IF #9709 EQ -1 THEN GOTO 1000

IF #9769 EQ 1 THEN m200 "Heading to touch plate. Press Cycle Start to proceed."

IF #50001 ;stop look ahead
G53 Z0 ; Be sure we are at z-zero! There are some cases where we wouldn't be at it..

IF #4120 EQ #9718 THEN G53 x[#9708-#9715] y[#9709-#9716] ELSE G53 X#9708 Y#9709
GOTO 1000

N900 
m200 "Fixed touch plate not setup. Please set it up in the UTILS menu. Press Cycle Stop to exit."
GOTO N900

N1000