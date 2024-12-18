; #T = requested tool
; TODO: mabye pass the old tool too..
G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1200 ;Skip macro if graphing or searching

#110 = #T

N100

; Check for ATC being enabled and skip cleanup.
; Check both the system one (9006) and Corbin's bit (9776 and 1)
IF [#9006 == 1] || [#9776 and 1] THEN GOTO 1200 ; ATC enabled 

IF #110 EQ #9718 THEN GOTO 1200 ;don't clean tools if laser is in use (this allows for fast laser/router bit switching)

#120=1 ;starting tool to zero out
N1120
IF #120 EQ #110 THEN GOTO 1150 ELSE  ;don't zero active tool
IF #120 EQ #9718 THEN GOTO 1150 ELSE  ;don't zero laser during job
#[10000 + #120] = 0 ;zero the first tool
#[11000 + #120] = 0 ;zero out diameters too
N1150
#120=[#120 + 1] ;increment tool number
IF #120 LE 100 THEN GOTO 1120 ELSE


N1200