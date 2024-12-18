; sets #1 if the air is okay and good, or ignored. Else it forces the user to turn it on and waits..
; corbin - copied from avid's code into this "function"

G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1000 

;check to see if we have air pressure
IF <AVID_DISABLE_AIR_CHECK> EQ 0 THEN GOTO 3 ;if we have disable air pressure check skip ahead
IF <AVID_AIR_INPUT> EQ 0 THEN GOTO 3 ;skip if there is no air pressure input
IF <AVID_AIR_INPUT_VALUE> EQ 1 THEN GOTO 2 ELSE GOTO 3;0 EQ we have pressure, not zero means we don't have pressure

#130 = 1;dialog time for warning

N2 
#129=1;dialog timer
m225 #129 "Checking for air pressure..."
m225 #129 "You have no air pressure. Please enable air."
IF <AVID_AIR_INPUT_VALUE> EQ 1 THEN GOTO 2 ELSE GOTO 3;0 eq pressure not zero e1 no pressure
N3 

M99