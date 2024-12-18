; goes to the touch plate, or stops if the user doesn't have it setup

G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1000 

IF <AVID_TOFF_PLATE_SET> EQ 0 then GOTO 900

IF <AVID_PROMPT_GOTO_TO> EQ 1 THEN m200 "Heading to touch plate. Press Cycle Start to proceed."

G53 Z0 ; Be sure we are at z-zero! There are some cases where we wouldn't be at it..
G53 X<AVID_TOFF_X> Y<AVID_TOFF_Y>

GOTO 1000

N900 
m200 "Fixed touch plate not setup. Please set it up in the UTILS menu. Press Cycle Stop to exit."
GOTO N900

N1000