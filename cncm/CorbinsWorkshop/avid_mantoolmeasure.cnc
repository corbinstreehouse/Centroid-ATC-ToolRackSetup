;full manual tool measure
;this script will allow you to measure a tool on your worksurface
;This can be useful if you have an oddball tool like a slab slayer or a dragknife

#130 = 2 ;dialog timer

IF #9701 EQ 0 THEN GOTO 1000 ;bail out if our spoilboard location isn't set.


IF #A EQ 1 then goto 100 ;if this is being called from m6 then skip the prompts


m200 "This utility will let you measure any tool off of your work surface\nThis can be useful if you have an odd or large tool that can't work with the touch plate"
M224 #110 "What's the tool number you're inserting?"

N100
If #A EQ 1 THEN #110 = #4120 ;set tool number to called tool number
If #A EQ 1 THEN GOTO 101 ;skip diameter question

IF #110 NE #9718 THEN m224 #125 "What's the DIAMETER of the tool you're inserting?" ELSE ;This will as the diameter of the tool unless it's a laser
N101
If #110 EQ #9718 THEN M81 ;deploy laser
IF #110 NE #9718 THEN #[11000+#110]=#125;set entered diameter into the tool libary if its not a laser
IF #110 EQ #9718 THEN #[11000+#9718]=#9719;set laser nozzle diameter to the tool diameter if we're on laser

; CORBIN fixed to seet the right thing; no g43 calls, 
; G43h#110 ;sets the active tool height to the tool that you just picked
;#10000=0; zeros that tool height. This is so you can remeasure the same tool number if you want.
#[10000 + #110] = 0

#150=0 ;clears out the stored tool
T#110 ;sets the tool number to the number that the user wants


IF #A NE 1 THEN m200 "Insert tool number %.0f and then press Cycle Start" #4120;don't stop if we're on laser
m200 "Jog so that your tool is just skimming the work surface\nPress Cycle Start when ready"
m200 "Are you sure your tool is exactly on the work surface?\nPress Cycle Start to confirm"
; corbin fixed
#[10000 + #110]=#5023 ;Set's tool height for active tool
m225 #130 "New tool height set"

; TODO: make this a subroutine file
If #A EQ 1 then GOTO 10000 ;end because M6 will clean the tools 

; CORBIN - check for ATC being enabled and skip this
IF #9006 == 1 THEN GOTO 1200 ; ATC enabled

IF #4120 EQ #9718 THEN GOTO 1200 ;don't clean tools if laser is in use (this allows for fast laser/router bit switching)
#120=1 ;starting tool to zero out
N1120
IF #120 EQ #4120 THEN GOTO 1150 ELSE  ;don't zero active tool
IF #120 EQ #9718 THEN GOTO 1150 ELSE  ;don't zero laser during job
#[10000 + #120] = 0 ;zero the first tool
#[11000 + #120] = 0 ;zero out diameters too
N1150
#120=[#120 + 1] ;increment tool number
IF #120 LE 100 THEN GOTO 1120 ELSE

m61 ;retract laser
G53 z0
GOTO 10000

N1000
M200 "Your work surface isn't calibrated yet. Please do that in UTILS menu"

N10000