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


;---------------------------------
; laser safety checks
;
#120 = 0

<PREVENT_LOOK_AHEAD>

;check to see if laser offset is calibrated
IF #9715 EQ -99 || #9716 EQ -99 || #9715 EQ 0 && #9716 EQ 0 THEN #120=666 ;This means that we have to fail out of the tool change
IF #9715 EQ -99 || #9716 EQ -99 || #9715 EQ 0 && #9716 EQ 0 THEN M200 "Laser XY offset hasn't been calibrated\nPlease do this in the UTILS menu" ;if laser XY has never been setup bail out.

;check to see if we can actually hit the touch plate
;IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN M200 "Can't reach touch plate with laser. Please move and recalibrate touch plate or laser\nCanceling Job" ;Check to see if we can hit the touch plate with the laser
;IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN M200 "Can't reach touch plate with laser. Please move and recalibrate touch plate or laser\nCanceling Job"
;IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN #120=666 ;set fail flag
;IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN #120=666
;IF [#9708-#9715] LE #23501 || [#9708-#9715] GE #23601 THEN GOTO 3 ;bail
;IF [#9709-#9716] LE #23502 || [#9709-#9716] GE #23602 THEN GOTO 3

;--------------------------------------------------
<PREVENT_LOOK_AHEAD>
;check to see if we have air pressure; loops till we get it or cycle stop
M98 "\cncm\CorbinsWorkshop\check_air_pressure.cnc" 
;--------------------------------------------------

N3
IF #120 EQ 666 THEN GOTO 800 ;bail out if laser safety check fails


; ---------------- COPIED FROM AVID's mfunc6.mac, 12/18/2024, but fixed up  -----------------------
S0			 	     			;Set laser power to zero

; TODO: this is buggy; it will potentially change the wrong WCS if it switched between tools
; add diode laser offset to current WCS if we haven't already
IF !<AVID_LASER_OFFSET_ACTIVE> THEN <C_ACTIVE_WCS_X> = [<C_ACTIVE_WCS_X> - <AVID_LASER_X_OFF>]
IF !<AVID_LASER_OFFSET_ACTIVE> THEN <C_ACTIVE_WCS_Y> = [<C_ACTIVE_WCS_Y> - <AVID_LASER_Y_OFF>]
<AVID_LASER_OFFSET_STATE> = 1

#130 = 1
m225 #130 "Laser XY offset applied"

M37								;enable laser			 			
m61 							;deploy laser
G43 H<AVID_LASER_TOOL> 						;set tool height to current tool (in this case the laser)
<AVID_SAVED_TOOL>=<AVID_LASER_TOOL>						;set tool number for laser
;IF #10000 EQ 0 THEN M34 ELSE 	;set laser height if we need to
<C_ACTIVE_DIA> = <AVID_LASER_DIA>					;set laser nozzle diameter

;------------------------------------------------
GOTO 1000 ; success; go to the end


N800 ;Errors
#100 = 0
M225 #100 "Laser safety checks failed\nCycle Stop to Abort"
GOTO 800 ; infinite loop
; -------------------------

N1000

