; #A is the pocket number, which should be already checked
; #B is old tool number, in case i need it

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

#108 = #A ; A pocket number
#109 = #B ; B is the old tool #
#110 = 3 ; time to show the message
#111 = #C ; C is the new tool number


; Agh, I thought it would be great to just save the string and call it with "G65 #300 Ax", but that doesn't work..doing 
;#300 = "\cncm\CorbinsWorkshop\Generated\pocket_%.0f_position.cnc" #A ; filename for this pocket's info
m221 #110 "Fetching tool tool %.0f from pocket %.0f" #111 #108

G53 Z0 ; go to z-zero to clear everything

;m225 #110 "go to above the fork"

; Move to above the fork
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#108_position.cnc" A4 ; X/Y position

M98 "\cncm\CorbinsWorkshop\check_air_pressure.cnc" 

M15 ; open drawbar

;m225 #110 "go to zbump"
; Move to Z-bump pos (slightly above fork)
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#108_position.cnc" A5 ; Z Bump position

M16 ; close drawbar

G4 P0.5 ; wait for a brief moment for it to clamp

;m225 #110 "go to fork pos"
; Go down to the Z-pos
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#108_position.cnc" A3 ; Z pos position
 
 ;m225 #110 "pull out from fork"
; Pull out of the fork
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#108_position.cnc" A2 ; X/Y position with clearance

;m225 #110 "go up"
G53 Z0 ;Clear Z from Tool

; Go to the intermediate pos to avoid collisions
;m225 #110 "go to intermediate pos"
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#108_position.cnc" A1 ; Move to in front of the RACK

; maybe Set the tool now, early ? I need it added..
;IF #50001 ; force no look ahead
; Txxxx

N1000