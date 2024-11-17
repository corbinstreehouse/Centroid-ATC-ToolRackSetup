; tool_measure_height.cnc
; measures tool heights automatically with the touch plate
;
; by corbin dunn & eric 
; 

; #A = 1 ; set to 1 if we should not prompt the user to insert the tool
; #T = The tool to measure

; #110 = tool number from #4120
; #125 = tool diameter
; #106 = touch feed

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

#110 = #T
#125 = #[11000 + #110] ; diameter

; m225 #140 "Tool height measure for %.0f diameter %f" #110 #125

; -------------------- tool diameter check; required for large tools to not hit the spoilboard ---------------
if #125 > 0 THEN GOTO 100

; if it is the laser, then skip that? Set it to the diameter of the thingy?
if #110 == #9718 THEN GOTO 100

m224 #125 "TOOL HEIGHT\nWhat's the DIAMETER of the tool %.0f?" #110 ;This will be the diameter of the tool unless it's a laser
#[11000+#110] = #125 ;set entered diameter into the tool libary if its not a laser

; --------------------

N100

; variables copied from Avid's code.
#111 = .4  ;what we consider a large tool when the touch plate is below the spoilboard
IF #4006 EQ 20 THEN #111 = #111 ELSE #111 = [#111 * 25.4] ;change max tool diameter to metric if we're in metric
#104 = .75 ;what we consider a large tool when the touch plate is above the spoilboard
IF #4006 EQ 20 THEN #104 = #104 ELSE #104 = [#104 * 25.4] ;change max tool diameter to metric if we're in metric
#105 = 5 ;slow feedrate
IF #4006 EQ 20 THEN #105 = #105 ELSE #105 = [#105 * 25.4] ;change slow feedrate to metric
#106 = 40 ;fast feedrate
IF #4006 EQ 20 THEN #106 = #106 ELSE #106 = [#106 * 25.4] ;change fast feedrate to metric
#107 = .25 ;amount we pop up between probes
IF #4006 EQ 20 THEN #107 = #107 ELSE #107 = [#107 * 25.4] ;change fast feedrate to metric

; The rest is copied from Avid's m6 subprogram O9105; Probe tool length, with the CORBIN mods marked



#130 = 3 ;dialog box timer
N50
IF #[50000 + #9706] EQ 0 THEN GOTO 51 ELSE m225 #130 "Touch Plate is triggered. Clear to continue";1 is clear 0 is triggered
IF #[50000 + #9706] EQ 0 THEN GOTO 51 ELSE GOTO 50;1 is clear 0 is triggered
N51

;CORBIN - added #A check to avoid dialog
IF [#110 NE #9718 && #A == 0] THEN m200 "Insert tool number %.0f and then press Cycle Start" #110 ;don't stop if we're on laser
IF #110 EQ #9718 THEN G53 x[#9708-#9715] y[#9709-#9716] ELSE G53 X#9708 Y#9709 ;get back to touch plate if we have jogged away

;large tool with touch plate below spoilboard
If #[11000+#110] GE #111 && #9713 GE 0 THEN GOTO 52

;large tool with touch plate above spoilboard
If #[11000+#110] GE #104 && #9713 LT 0 THEN GOTO 52
GOTO 60


N52


M224 #112 "You're trying to probe a large tool that might not fit on your touch plate\nWould you like to\n#)1:Move in XY a little so you can get the flutes over the touch plate\n#)2:Manually measure this tool off of your work surface?\n#)3:Skip this and measure the tool normally"

IF #112 EQ 1 THEN GOTO 53
IF #112 EQ 2 THEN GOTO 54
IF #112 EQ 3 THEN GOTO 60
GOTO 52 ;loop back if we get invalid input
N53 ;kick user over to manual tool measure
m200 "Use the jog keys to position your tool above the tool height offsetter so that the flutes will hit properly\nThen press Cycle Start"
m200 "Are you sure your tool is ready to be probed?\nPlease make any further adjustments and if you're ready press Cycle Start"
GOTO 60 ;proceed with probing

N54
G65 "\cncm\AvidMacros\mantoolmeasure.mac" A1 T#110 D#125
GOTO 62 ;we're done
N60

#[10000 + #110]=0 ;zero the height of the current tool ; CORBIN NOTE - probably not needed

M5; make sure the spindle is off!

IF #110 EQ #9718 THEN M61 ;(drop laser)

M115 /Z P#9706 f#106 ;moves down in Z until the touch plate is hit
G91 z#107 f#106 ;moves up a bit so we can hit the switch again in incremental mode
G90 ;goes back to absolute position
N61
#130 = 1
IF #[50000 + #9706] EQ 1 THEN m225 #130 "Touch Plate is triggered. Clear to continue";1 is clear 0 is triggered
IF #[50000 + #9706] EQ 1 THEN GOTO 61 ELSE ;1 is clear 0 is triggered

M115 /Z P#9706 f#105 ;moves down in Z until the touch plate is hit
#103=[#5023 + #9713]; this should be the tool length to the spoilboard 5023 is machine coordinate of Z axis
;IF #110 EQ #9718 THEN #101=[#5023 - #9713 + #9717] ;this adds a little height for the laser focus
IF #110 EQ #9718 THEN #103=[#103 + #9717] ;this adds a little height for the laser focus
G53 z0; This brings us back up to the highest Z can go so we don't smash up a bit

; CORBIN - changed from #10000 to actually set the height for the tool, which may not be active yet (no G43)
;#10000=#103 ;Set's tool height for active tool
#[10000 + #110] = #103


#130 = 1
IF [[#9776 and 1] == 0] THEN m225 #130 "Tool %.0f measured!" #110

;IF #110 EQ #9718 THEN M81 (retract laser) 
; ^ corbin note, maybe shouldn't have that line in here (was commented out prior)
N62

M99