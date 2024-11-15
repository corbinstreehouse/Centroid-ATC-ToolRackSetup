; sets #1 if the air is okay and good, or ignored. Else it forces the user to turn it on and waits..
; corbin - copied from avid's code into this "function"

IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

#1 = 0 ; return variable; non-volatile. 1 means we are good, 0 means stop

;check to see if we have air pressure
IF #9724 EQ 0 THEN GOTO 3 ;if we have disable air pressure check skip ahead
IF #9763 EQ 0 THEN GOTO 3 ;skip if there is no air pressure input
IF #[50000+#9763] EQ 1 THEN GOTO 2 ELSE GOTO 3;0 EQ we have pressure, not zero means we don't have pressure

#130 = 1;dialog time for warning

N2 
#129=1;dialog timer
m225 #129 "Checking for air pressure..."
m225 #129 "You have no air pressure. Please enable air."
IF #[50000+#9763] EQ 1 THEN GOTO 2 ELSE GOTO 3;0 eq pressure not zero e1 no pressure
N3 

#1 = 1

M99