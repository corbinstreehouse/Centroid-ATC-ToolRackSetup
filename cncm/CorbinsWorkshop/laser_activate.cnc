; Laser support
;activates the laser when changing to a tool that is the laser number
; precondition: new tool number == #9718 (laser tool number) 
;
; Based on Avid's macros.
; COMPLETELY UNTESTED. 
; 
; Parameters
; #9718 = laser tool number
; #9715/#9716 - I'm guessing this is the laser offset from the spindle location
; #9719 - laser nozzle diameter

S0			 	     			;Set laser power to zero				
g52 x-#9715 y-#9716     		;offest to account for location of laser relative to spindle
M37								;enable laser			 			
m61 							;deploy laser
; corbin note: the caller should do the G43; I'm not sure we need this....and having it isn't quite correct.
G43 H#9718						;set tool height to current tool (in this case the laser)
#[11000 + #9718] = #9719					;set laser nozzle diameter

;---------------------------------
; laser safety checks
#120 = 0

;check to see if laser offset is calibrated
IF #9715 EQ -99 || #9716 EQ -99 || #9715 EQ 0 || #9716 EQ 0 THEN #120=666 ;This means that we have to fail out of the tool change
IF #9715 EQ -99 || #9716 EQ -99 || #9715 EQ 0 || #9716 EQ 0 THEN M200 "Laser XY offset hasn't been calibrated\nPlease do this in the UTILS menu" ;if laser XY has never been setup bail out.

;check to see if we can actually hit the touch plate
IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN M200 "Can't reach touch plate with laser. Please move and recalibrate touch plate or laser\nCanceling Job" ;Check to see if we can hit the touch plate with the laser
IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN M200 "Can't reach touch plate with laser. Please move and recalibrate touch plate or laser\nCanceling Job"
IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN #120=666 ;set fail flag
IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN #120=666
IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN GOTO 3 ;bail
IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN GOTO 3

;check to see if we have air pressure; loops till we get it or cycle stop
M98 "\cncm\CorbinsWorkshop\check_air_pressure.cnc" ; Sets #1 as the "return" variable, 1 = air good or set to ignore, 0 = air bad stop

N3 
