;------------------------------------------------------------------------------
; Filename: tool_set.cnc
; The thing that actually sets the tool we are seeing
; Can be called from other places (ie: code/app).
; Does not do error checking.
; Parameters: T - the tool number
; corbin dunn
;------------------------------------------------------------------------------
G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1000 ;Skip macro if graphing or searching

N100 

DEFINE <TOOL_NUMBER> #101
<TOOL_NUMBER> = #T 

G10 P976 R<TOOL_NUMBER> ; Set the tool # in the PLC
G4 P.1 ; force PLC to update so #4203 is updated - probably not needed.
T<TOOL_NUMBER> ; Not really needed
G43 H<TOOL_NUMBER>

; We almost don't even need to do this, but #150 is where Avid stores the tool number,
; and uses it in the .hom file, and we want to work without modifying that file.
<AVID_SAVED_TOOL> = <TOOL_NUMBER>

;M222 Q0 #111 "Tool %.0f set." <TOOL_NUMBER>

N1000                            ;End of Macro
