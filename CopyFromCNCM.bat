xcopy /y /i C:\CNCM\CorbinsWorkshop\*.* CNCM\CorbinsWorkshop\
del CNCM\CorbinsWorkshop\ToolPocketPositions*.xml
xcopy /y c:\CNCM\mfunc6.mac CNCM\
::xcopy /y c:\CNCM\mfunc3.mac CNCM\ :: No longer customized
xcopy /y c:\CNCM\mfunc5.mac CNCM\
xcopy /y c:\CNCM\mfunc15.mac CNCM\
xcopy /y c:\CNCM\mfunc16.mac CNCM\

:: Avid files I had to fix up a bit..
::mantoolmeasure was fixed to not clear tools
xcopy /y c:\CNCM\AvidMacros\mantoolmeasure.mac CNCM\AvidMacros\ :: fixed reset of tool library
::xcopy /y c:\CNCM\AvidMacros\mtc.mac CNCM\AvidMacros\ :: not used by my stuff, so don't bother
xcopy /y c:\CNCM\AvidMacros\utilsrouter.mac CNCM\AvidMacros\ ::adds one more thing for me

xcopy /y c:\CNCM\resources\vcp\skins\avid_router.vcp CNCM\resources\vcp\skins\

xcopy /y /i  c:\CNCM\resources\vcp\Buttons\tool_run_atc_tools\ CNCM\resources\vcp\Buttons\tool_run_atc_tools\
xcopy /y /i c:\CNCM\resources\vcp\Buttons\tool_put_back\*.* CNCM\resources\vcp\Buttons\tool_put_back\

xcopy /y /i c:\CNCM\resources\vcp\Buttons\tool_fetch\*.* CNCM\resources\vcp\Buttons\tool_fetch\
xcopy /y /i c:\CNCM\resources\vcp\Buttons\tool_set\*.* CNCM\resources\vcp\Buttons\tool_set\
xcopy /y /i c:\CNCM\resources\vcp\Buttons\drawbar\*.* CNCM\resources\vcp\Buttons\drawbar\
