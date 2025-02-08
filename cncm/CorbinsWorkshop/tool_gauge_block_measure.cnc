; corbin dunn
; Ask's the user for the gauge block height and sets the tool length based on the height from their current z-zero 
;

G65 "\cncm\CorbinsWorkshop\defines.cnc"

<PREVENT_LOOK_AHEAD>
IF <GRAPHING_OR_SEARCHING> THEN GOTO 1000 ;Skip macro if graphing or searching

DEFINE <NEW_TOOL> #101
DEFINE <GAUGE_BLOCK_HEIGHT> #102
DEFINE <PROMPT_ANSWER> #103
DEFINE <ALERT_DURATION> #104
DEFINE <OLD_TOOL> #105
DEFINE <OLD_TOOL_HEIGHT> #106
DEFINE <HEIGHT_DIFF> #107
DEFINE <NEW_TOOL_HEIGHT> #108
DEFINE <STARTING_MACHINE_Z> #109

DEFINE <YES> 121
DEFINE <ERROR_LINE_NO_TOOL> 800
DEFINE <ERROR_LINE_NO_HEIGHT> 801
DEFINE <ERROR_BAD_TOOL_NUMBER> 802

DEFINE <ACTIVE_WCS_Z> #2700
DEFINE <MACHINE_Z> #5023
DEFINE <HEIGHT_TABLE_START> 10000

<OLD_TOOL> = <SAVED_TOOL>
<ALERT_DURATION> = 2.0 ; seconds


<OLD_TOOL_HEIGHT> = #[<HEIGHT_TABLE_START> + <OLD_TOOL>]

; Check the current tool's height..it has to be set for things to work correctly!
; if <OLD_TOOL> <= 0 then GOTO <ERROR_LINE_NO_TOOL> 
; if <OLD_TOOL_HEIGHT> == 0 then GOTO <ERROR_LINE_NO_HEIGHT>

; save this current tool's WCS Z-zero in machine coordinates
<STARTING_MACHINE_Z> = <ACTIVE_WCS_Z> + <OLD_TOOL_HEIGHT>

m224 <GAUGE_BLOCK_HEIGHT> "Tool Height Setter\nActive Tool: T%.0f\nZ-zero should be set to under your gauge block.\nWhat's the height of your gauge block?" <OLD_TOOL>

; Save off the current offset's z-zero

;m224 <NEW_TOOL> "What is the new tool you are measuring the height of?"
;if [<NEW_TOOL> <= 0 || <NEW_TOOL> > 200] GOTO <ERROR_BAD_TOOL_NUMBER>

; Activate that tool and the height for it..
; G65 "\cncm\CorbinsWorkshop\tool_set.cnc" T<NEW_TOOL>
<NEW_TOOL> = <OLD_TOOL> ; Making things simplier than what I had before.
m200 "Jog the bit to the top of your gauge block.\n Press Cycle Start to measure the tool length when you are ready." <NEW_TOOL>

<HEIGHT_DIFF> = <MACHINE_Z> - <STARTING_MACHINE_Z> - <GAUGE_BLOCK_HEIGHT>
;m200 "HEIGHT_DIFF: %f, STARTING_MACHINE_Z:%f, MACHINE_Z: %f, GAUGE_BLOCK_HEIGHT: %f" <HEIGHT_DIFF> <STARTING_MACHINE_Z> <MACHINE_Z> <GAUGE_BLOCK_HEIGHT>

<NEW_TOOL_HEIGHT> = <OLD_TOOL_HEIGHT> + <HEIGHT_DIFF>
; TODO: save it!
;m200 "NEW_TOOL_HEIGHT: %f" <NEW_TOOL_HEIGHT>
#[<HEIGHT_TABLE_START> + <NEW_TOOL>] = <NEW_TOOL_HEIGHT>

M221 <ALERT_DURATION> "Tool height set saved for T%.0f!" <NEW_TOOL>
GOTO 1000

;------------------ errors
N<ERROR_LINE_NO_TOOL>
m200 "Abort: You must enter a starting tool that is already measured. Cycle Stop to abort."
GOTO <ERROR_LINE_NO_TOOL>

N<ERROR_LINE_NO_HEIGHT>
m200 "Abort: The starting tool needs to already be measured. Cycle Stop to abort."
GOTO <ERROR_LINE_NO_HEIGHT>

N<ERROR_BAD_TOOL_NUMBER>
m200 "Abort: Bad tool number!"
GOTO <ERROR_BAD_TOOL_NUMBER>


N1000



