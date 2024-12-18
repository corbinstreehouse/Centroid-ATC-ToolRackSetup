; Laser support
; de-activates the laser when changing to a tool that isn't the laser number
; precondition: old tool number == #9718 (laser tool number) 
;
; Based on Avid's macros.
; COMPLETELY UNTESTED. 
; 
; Parameters
; #9718 = laser tool number

; corbin, debugging
; m225 #140 "Laser deactivate"

; Copied from Avid's code 12/18/2024 but fixed up to use variables

;--------------- Disable Laser
;S0							 	;Turn Laser Off
; remove diode laser offset to current WCS if we haven't already
IF <AVID_LASER_OFFSET_ACTIVE> THEN <C_ACTIVE_WCS_X> = [<C_ACTIVE_WCS_X> + <AVID_LASER_X_OFF>]
IF <AVID_LASER_OFFSET_ACTIVE> THEN <C_ACTIVE_WCS_Y> = [<C_ACTIVE_WCS_Y> + <AVID_LASER_Y_OFF>]
<AVID_LASER_OFFSET_STATE>  = 0 ;set flag that we have removed laser offset
#130 = 1
m225 #130 "Laser offset removed"
M38 							;disable laser					
m81 			 				;retract laser
;--------------------------

