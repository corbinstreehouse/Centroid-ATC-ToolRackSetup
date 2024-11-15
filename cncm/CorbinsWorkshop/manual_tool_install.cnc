; A is #1, the tool to show to install.
; by: corbin dunn

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

G53 Z0 ; go to z-zero to clear everything

G65 "\cncm\CorbinsWorkshop\goto_touch_plate.cnc"

M200 "Install Tool %.0f into the spindle and press Cycle Start to Continue." #1   ; prompt for input


