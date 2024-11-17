; Laser support
; de-activates the laser when changing to a tool that isn't the laser number
; precondition: old tool number == #9718 (laser tool number) 
;
; Based on Avid's macros.
; COMPLETELY UNTESTED. 
; 
; Parameters
; #9718 = laser tool number


;--------------- Disable Laser
;S0							 	;Turn Laser Off
g52 x0 y0						;remove offset relative to laser and spindle
M38 							;disable laser					
m81 			 				;retract laser
;--------------------------

