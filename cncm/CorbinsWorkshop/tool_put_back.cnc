; #A is the pocket number
; #T is the tool (not really needed)
; this requires a valid pocket already to be checked

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

#107 = #A
#108 = #T
;#300 = "\cncm\CorbinsWorkshop\Generated\pocket_#107_position.cnc"; filename for this pocket's info ; ugh..this doesn't work
 
G53 Z0 ; go to z-zero to clear everything

#110 = 3 ; time to show the message (non modal)
m221 #110 "Putting back tool %.0f into pocket %.0f" #108 #107

#109 = #9778 ;set my custom parameter #9778 spindle wait time
if #109 <= 0 then #109 = 15 ; default wait

; First move to the front of the RACK
; This is needed because some 8" Z machines with long bits will hit the top of the tool holders, 
; unless we go in front of the empty tool holder to put back at. This happens when the tool change
; starts really close to the tool rack. (reported as an issue in my Mach 4 code)

;m225 #110 "Going to front of rack intermediate pos"
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#107_position.cnc" A1 ; Move to in front of the RACK

; Move to in front of the fork, but still Z-high
;m225 #110 "Going to front of fork, still high"
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#107_position.cnc" A2 ; X/Y position with clearance

;wait for the spindle to stop -----------------------------
; wait for the spindle to stop: #151 = the time the spindle stop command was given, M5, mfunc5.mac sets this value to #25012 (now)
if [#151 > 0] then #138 = #25012 - #151 else #138 = 100000 ; big number for time elapsed
if [#138 < 0] then #138 = 100000 ; negative time? something was messed up
#111 = #109 - #138 ; #111 is the time left to wait for it to stop; if it is negative, then we are good to go; make it really small...
if #111 < 0 then GOTO 100 ; skip the wait

m221 #111 "Waiting for the spindle to stop (%f seconds)" #111 ; non modal, no wait..
G4 P[#111]

N100

;-----------------------------

; Move down to in front of the fork
;m225 #110 "moving down to front of fork"
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#107_position.cnc" A3 ; Z fork position

; TODO: wait for the spindle to stop at this point, via a timer..

; Engage the fork
;m225 #110 "Engage fork"
G65 "\cncm\CorbinsWorkshop\Generated\pocket_#107_position.cnc" A4 ; Fork X/Y position

M98 "\cncm\CorbinsWorkshop\check_air_pressure.cnc" 

M15 ; open drawbar

; dwell for a brief moment, otherwise my rack will almost get pulled off
G4 P0.6

;m225 #110 "go high"
; TODO: Maybe don't go to z-0 if not needed to save a litle time
G53 Z0                       ;Clear Z from Tool

M16 ; close drawbar

IF #50001 ; force no look ahead

; mark that nothing is in the spindle because it is gone at this point
G10 P976 R0 ; set the tool number to 0 in the PLC
g4 p0.1 ; wait for the update

; T0 ; corbin set the value here? I'll assume the caller does it.

N1000