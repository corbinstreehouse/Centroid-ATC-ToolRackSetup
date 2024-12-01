using CentroidAPI;
using static CentroidAPI.CNCPipe.Parameter;
using static CentroidAPI.CNCPipe.Tool;

using System;

namespace ToolRackSetup
{
    public enum ParameterKey
    {
        // System parameters
        CentroidHasATC = 6, // Currentlly needs to be 0
        CentroidHasEnhancedATC = 160, // Currently needs to be 0, can have variuous bits set which cause the PLC to be used for the tool number
        MaxToolBins = 161, // Read/written

        // Custom parameters by Avid (see resetparams.mac)
        SpoilboardCalibrated = 701,
        TouchOffPlateSet = 702,
        ShouldCheckAir = 724, // Avid setting that I read and write
        TouchOffPlateX = 708,
        TouchOffPlateY = 709,
        LaserToolNumber = 718,

        PromptToGoToTouchPlate = 769,




        // Custom parameters by Corbin
        ATCToolOptions = 776, // bitset, see ATCToolOptions enum

        //  EnableVirtualDrawbar = 777, // virtual drawbar button support; prefer to be 0. (corbin)
        SpindleWaitTime = 778, // spindle wait time, in seconds (corbin)


        CentroidATCType = 830, // 0 = None, 7 = Rack type...; this needs to be 0! Otherwise the wizard keeps re-writing stuff.

        CurrentToolNumber = 976, // System, used by the PLC and I read/write it.

    }

    // Bitset values
    public enum ATCToolOptions
    {
        EnableATC = 1,  // 1 if it is enabled; checked in some code for clearing the tool table
        TurnOffRelay2WithSpindle = 2, // Turn off relay 2 with the spindle instead of the end of the job
        EnableVirtualDrawbar = 4, // Enable vitual drawbar button
    }


    public static class CNCPipeExtensions
    {

        public static bool GetToolOptionValue(this CNCPipe.Parameter parameter, ATCToolOptions option)
        {
            return parameter.GetBitValue((int)ParameterKey.ATCToolOptions, (int)option);
        }
        public static void SetToolOptionValue(this CNCPipe.Parameter parameter, ATCToolOptions option, bool value)
        {
            parameter.SetBitValue((int)ParameterKey.ATCToolOptions, (int)option, value);
        }

        public static void SetBitValue(this CNCPipe.Parameter parameter,
            int parameter_num, int bit, bool onOrOff)
        {
            // Get the existing value and turn on/off the appropriate bit
            int value = (int)parameter.GetValue(parameter_num);
            if (onOrOff)
            {
                value = value | bit;
            }
            else
            {
                value = value & ~bit;
            }

            parameter.SetValue(parameter_num, value);
        }


        public static bool GetBitValue(this CNCPipe.Parameter parameter, int parameterNum, int bit)
        {
            int value = (int)parameter.GetValue(parameterNum);
            return (value & bit) == bit;
        }


        // Throws an exception on error!
        public static void SetValue(this CNCPipe.Parameter parameter, int parameter_num, double value)
        {
            CNCPipe.ReturnCode rc = parameter.SetMachineParameter(parameter_num, value);
            if (rc != CNCPipe.ReturnCode.SUCCESS)
            {
                string reason = rc.ToString();
                string eMsg = String.Format("Failed to set machine parameter {0} to {1}.\nEnsure you are not running a job!\nError: {2}", parameter_num, value, reason);
                throw new Exception(eMsg);
            }
        }

        public static void SetValue(this CNCPipe.Parameter parameter, ParameterKey parameterKey, double value)
        {
            parameter.SetValue((int)parameterKey, value);
        }

        public static void SetValue(this CNCPipe.Parameter parameter, ParameterKey parameterKey, bool value)
        {
            parameter.SetValue((int)parameterKey, Convert.ToDouble(value));
        }

        public static double GetValue(this CNCPipe.Parameter p, int parameterNum)
        {
            double value = 0;
            CNCPipe.ReturnCode rc = p.GetMachineParameterValue(parameterNum, out value);
            if (rc != CNCPipe.ReturnCode.SUCCESS)
            {
                string reason = rc.ToString();
                string eMsg = String.Format("Failed to get machine parameter {0}.\nEnsure that CNC12 is running!\nError: {1}", parameterNum, reason);
                throw new Exception(eMsg);
            }
            return value;
        }
        public static double GetValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            return p.GetValue((int)parameterKey);
        }

        public static bool GetBoolValue(this CNCPipe.Parameter p, ParameterKey parameterKey)
        {
            double d = p.GetValue(parameterKey);
            return System.Convert.ToBoolean(d);
        }
    }

}