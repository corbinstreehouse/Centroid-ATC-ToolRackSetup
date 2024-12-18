; Just a common place for variables to be "included"
; by corbin

; Centroid defines
DEFINE <ACTIVE_TOOL> #4120 ; TODO: rename to C_ACTIVE_TOOL

DEFINE <C_POSITIONING_MODE> #4003  ; 90.0(abs)or91.0(inc)
DEFINE <C_UNITS_OF_MEASURE> #4006 ; 20.0(inches)or21.0(metric)
DEFINE <C_WCS> #4014 ;  54.0–71.0(WCS#1–18)

DEFINE <C_ACTIVE_WCS_X> #2500
DEFINE <C_ACTIVE_WCS_Y> #2600

DEFINE <C_MACHINE_X> #5021 ; Machine x coordinate
DEFINE <C_MACHINE_Y> #5022
DEFINE <C_MACHINE_Z> #5023

DEFINE <C_DIA_OFFSET> 11000
DEFINE <C_ACTIVE_DIA> #11000 ; Router: Diameter offset amount, active D

DEFINE <C_TIME> #25012 ; time in seconds


; Useful things
DEFINE <PREVENT_LOOK_AHEAD> IF #50001
DEFINE <GRAPHING_OR_SEARCHING> [#4201 || #4202]
DEFINE <IN_INCHES> [<C_UNITS_OF_MEASURE> EQ 20]
DEFINE <IN_MM> [<C_UNITS_OF_MEASURE> EQ 21]



; Corbin's Workshop defines
DEFINE <ATC_ENABLED> [#9776 and 1]   ; 9776 and 1 means it is enabled
DEFINE <VIRTUAL_DRAWBAR_ENABLED> [#9776 and 4]
DEFINE <SPINDLE_WAIT_DURATION> #9778
DEFINE <SPINDLE_STOP_TIME> #151
DEFINE <SPINDLE_STATUS> #60005 ; PLC output value 5 for the spindle status (1 is running), 0 is off


; Avid defines
DEFINE <AVID_SAVED_TOOL> #150 
DEFINE <AVID_LASER_PRIOR_T> #157 ; store the last tool before switching from the laser; only for calibration
DEFINE <AVID_LASER_OFFSET_STATE> #158 ; 1 if active, 0 if not active
DEFINE <AVID_LASER_OFFSET_ACTIVE> [#158 == 1]

DEFINE <AVID_Z_PLATE_INPUT> #9700 ;Z plate input | Integer representing the input number for the mobile Z plate
DEFINE <AVID_SPOILBOARD_CALIBRATED> #9701 ;Spoilboard Cal Reset | Tells the macros if the user has calibrated the spilboard or not 0=no 1=yes
DEFINE <AVID_TOFF_PLATE_SET> #9702 ;Touch off Plate location set | Tells the macros if the fixed touch plate location has been taught or not 0=no 1=yes
DEFINE <AVID_PREVENT_DIG_IN> #9703 ;Prevent Spoilboard dig in | Prevent spoilboard dig in 0=no 1=yes
DEFINE <AVID_FEEDRATE_WARNING> #9704 ;Feedrate/Spindeoverride warnings | 1=warn me 0=don't warn me
DEFINE <AVID_TOOL_HEIGHT_NUM> #9706 ;Tool Height Input Number | Tool height input number

DEFINE <AVID_TOFF_X> #9708 ;Tool touch off X location | X location of the tool touch off
DEFINE <AVID_TOFF_Y> #9709 ;Tool Touch off Y location | Y location of the tool touch off

DEFINE <AVID_SPOIL_LOC> #9713 ;Spoilboard location | "location" means the Z height of the spoilboard
DEFINE <AVID_LASER_X_OFF> #9715 ;Laser X offset | X offset for laser
DEFINE <AVID_LASER_Y_OFF> #9716 ;Laser Y offset | Y offset for laser
DEFINE <AVID_LASER_TOOL> #9718 ;Laser tool number | The tool number for the laser
DEFINE <AVID_LASER_DIA> #9719

DEFINE <AVID_DISABLE_AIR_CHECK> #9724 ; EQ 0 THEN GOTO 3 ;if we have disable air pressure check skip ahead
DEFINE <AVID_AIR_INPUT> #9763 ; EQ 0 THEN GOTO 3 ;skip if there is no air pressure input
DEFINE <AVID_AIR_INPUT_VALUE> #[50000+<AVID_AIR_INPUT>] ; EQ 1 THEN GOTO 2 ELSE GOTO 3;0 EQ we have pressure, not zero means we don't have pressure

DEFINE <738> #9738 ;Park Location x
DEFINE <739> #9739 ;Park location y
DEFINE <742> #9742 ;Home offset X
DEFINE <743> #9743 ;Home offset y
DEFINE <748> #9748 ;Rotary | If they have a rotary 1, if not 0
DEFINE <749> #9749 ;Rotary along X or Y | 1 for X 2 for Y
DEFINE <750> #9750 ;Rotary mach location x
DEFINE <751> #9751 ;Rotary mach loaction y
DEFINE <753> #9753 ;Spoilboard Dig in | Spoilboard dig in amount
DEFINE <759> #9759 ;Rotary Angle
DEFINE <772> #9772 ;Plasma calibration done | 1=yes 0=no
DEFINE <560> #9560 ;laser crosshair offset x | Laser crosshair offset x
DEFINE <561> #9561 ;laser crosshair offset y | Laser crosshair offset y

DEFINE <AVID_PROMPT_GOTO_TO> #9769 ; if 1, prompt to goto the touch plate

DEFINE <525> #9525 ;Torch voltage factor