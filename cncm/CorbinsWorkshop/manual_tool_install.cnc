; A is #1, the tool number to show to install.
; by: corbin dunn

; Parameters
; [#9776 and 4]  if we need a virtual drawbar support

G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1000 ;Skip macro if graphing or searching

G53 Z0 ; go to z-zero to clear everything

G65 "\cncm\CorbinsWorkshop\goto_touch_plate.cnc"

if !<VIRTUAL_DRAWBAR_ENABLED> then GOTO 900 ; Skip if a virtual draw bar support is not enabled

;------------------------------------------------------------------------------
; Virtual drawbar button support
; precondition: no tool in the spindle!
;------------------------------------------------------------------------------
#130 = 3 ;drawbar release timer   

N100      

m222 q0 #110 "INSTALL TOOL %.0f\nREADY TO RELEASE THE DRAWBAR?\nIf so press Y There will be a 3 second delay and then tool will release\nY for YES" #1
IF #110 != 121 then goto 100 ;loop if we get bad input
m225 #130 "Opening then drawbar in %.0f seconds." #130

M15 ; Unclamp

m200 "Install tool %.0f and press Cycle Start to clamp the drawbar." #1

M16 ; clamp

m225 #130 "Drawbar clamped. Continuing..."


GOTO 1000
;------------------------------------------------------------------------------
N900


M200 "Install Tool %.0f into the spindle and press Cycle Start to Continue." #1   ; prompt for input

<PREVENT_LOOK_AHEAD> ; Maybe? Stops the message from going away early

GOTO 1000
;------------------------------------------------------------------------------


N1000

