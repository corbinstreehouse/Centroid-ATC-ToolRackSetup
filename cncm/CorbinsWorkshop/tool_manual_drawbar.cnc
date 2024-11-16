;------------------------------------------------------------------------------
; Filename: tool_manual_drawbar.cnc
; Description: Tool unclamp macro, copied from Eric's m15
; By: corbin dunn
; Notes:s
; Requires: M15 and M16 to open and close the drawbar.
;
; Inputs:
; Outputs:
;------------------------------------------------------------------------------

IF #50010                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching
#130 = 3 ;drawbar release timer   

N100      

m222 q0 #110 "READY TO RELEASE THE DRAWBAR?\nIf so press Y There will be a 3 second delay and then tool will release\nY for YES"
IF #110 != 121 then goto 100 ;loop if we get bad input
m225 #130 "Releasing tool in %.0f seconds" #130

M15 ; Unclamp

m225 #130 "Tool released!"
m200 "Install a new tool and press Cycle Start to clamp drawbar"



M16 ; clamp


N1000                            ;End of Macro