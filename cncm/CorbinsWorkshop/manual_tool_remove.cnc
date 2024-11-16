; #T is the tool number to ask the user to remove manually (no pocket to go to)
; by corbin dunn

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

G53 Z0 ; go to z-zero to clear everything

; #100 = #T ; prompts can't use letter variables.. WTF?

;m225 #110 "ZZ Tool change requested from %.0f to %.0f" #100 #100

G65 "\cncm\CorbinsWorkshop\goto_touch_plate.cnc"

;---------------------------------------

if #9777==0 then GOTO 900; #9777 is 1 if we need a virtual toolbar button

;------------------------------------------------------------------------------
; Virtual drawbar button support
; precondition: no tool in the spindle!
;------------------------------------------------------------------------------
#130 = 3 ;drawbar release timer   

N100      
m222 q0 #110 "REMOVING TOOL %.0f\nREADY TO RELEASE THE DRAWBAR?\nIf so press Y There will be a 3 second delay and then tool will release\nY for YES" #20
IF #110 != 121 then goto 100 ;loop if we get bad input
m225 #130 "Releasing tool in %.0f seconds" #130

M15 ; Unclamp

m200 "Press Cycle Start after the tool has been removed."

M16 ; clamp

m225 #130 "Tool removed and the drawbar is clamped. Continuing..."


GOTO 950 ; goto where we update the value.

;-------------------------------------------------------------
N900
M200 "Remove Tool %.0f from the spindle and press Cycle Start to Continue." #20   ; prompt for input ; #20 is T, #T doesn't work
;-------------------------------------------------------------

;-------------------------------------------------------------
N950

IF #50001 
; mark that nothing is in the spindle because it is gone at this point
G10 P976 R0 ; set the tool number in the PLC
g4 p0.2 ; wait for the update
T0 
#150 = 0 ; store here too, as Avid uses this in cncm.hom, and I don't want to edit that file.
;-------------------------------------------------------------


N1000





