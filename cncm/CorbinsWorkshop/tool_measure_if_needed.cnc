;------------------------------------------------------------------------------
; Filename: tool_measure_if_needed.cnc
; Parameters: T - the tool number
; corbin dunn
;------------------------------------------------------------------------------
IF #50001                        ;Prevent lookahead from parsing past here
IF #4201 || #4202 THEN GOTO 1000 ;Skip macro if graphing or searching

DEFINE <TOOL_NUMBER> #101
<TOOL_NUMBER> = #T 
DEFINE <HEIGHT_OFFSET> 10000 ; Parameter

if [<TOOL_NUMBER> <= 0] then GOTO 1000 ; no tool
if [#[<HEIGHT_OFFSET> + <TOOL_NUMBER>] != 0] then GOTO 1000 ; height is set

; if the height is 0 ask to measure it now
M222 Q0 #111 "Tool %.0f has no height set. Would you like to measure it now? Press Y to measure it now." <TOOL_NUMBER>
IF #111 != 121 THEN GOTO 1000

G65 "\cncm\CorbinsWorkshop\tool_measure_height.cnc" A1 T<TOOL_NUMBER>

GOTO 1000

N1000                            ;End of Macro
