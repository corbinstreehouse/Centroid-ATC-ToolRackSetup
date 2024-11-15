; #T is the tool number to ask the user to remove manually (no pocket to go to)
; by corbin dunn

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

G53 Z0 ; go to z-zero to clear everything

; #100 = #T ; prompts can't use letter variables.. WTF?

;m225 #110 "ZZ Tool change requested from %.0f to %.0f" #100 #100

G65 "\cncm\CorbinsWorkshop\goto_touch_plate.cnc"

M200 "Remove Tool %.0f from the spindle and press Cycle Start to Continue." #20   ; prompt for input ; #20 is T, #T doesn't work

IF #50001 
; mark that nothing is in the spindle because it is gone at this point
G10 P976 R0 ; set the tool number in the PLC
g4 p0.2 ; wait for the update
T0 
#150 = 0 ; store here too, as Avid uses this in cncm.hom, and I don't want to edit that file.


N1000





