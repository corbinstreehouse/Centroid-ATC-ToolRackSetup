using CentroidAPI;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ToolRackSetup
{
    // Settings that write parameter values
    public class ParameterSettings : ObservableObject
    {
        CNCPipe _pipe;
        private double _spindleWaitTime = 12.0; // seconds
        private bool _shouldCheckAirPressure = true;
        private bool _enableVirtualDrawbar = false; // If true, we do more stuff
        private int _pocketCount = 0;
        public ParameterSettings(CNCPipe pipe)
        {
            _pipe = pipe;
            _enableATC = _pipe.parameter.GetToolOptionValue(ATCToolOptions.EnableATC);
            _shouldCheckAirPressure = _pipe.parameter.GetBoolValue(ParameterKey.ShouldCheckAir);
            _spindleWaitTime = _pipe.parameter.GetValue(ParameterKey.SpindleWaitTime);
            // if not set, default it to 12
            if (_spindleWaitTime <= 0)
            {
                _spindleWaitTime = 12;
            }
            _enableVirtualDrawbar = _pipe.parameter.GetToolOptionValue(ATCToolOptions.EnableVirtualDrawbar);
            _pocketCount = (int)_pipe.parameter.GetValue(ParameterKey.MaxToolBins);
            _promptWhenGoingToTouchPlate = _pipe.parameter.GetBoolValue(ParameterKey.PromptToGoToTouchPlate);
        }

        public bool PromptWhenGoingToTouchPlate
        {
            get
            {
                return _promptWhenGoingToTouchPlate;
            }
            set
            {
                if (_promptWhenGoingToTouchPlate != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.PromptToGoToTouchPlate, value);
                    SetProperty(ref _promptWhenGoingToTouchPlate, value);
                }
            }
        }

        public double SpindleWaitTime
        {
            get => _spindleWaitTime;
            set
            {
                if (_spindleWaitTime != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.SpindleWaitTime, value);
                    SetProperty(ref _spindleWaitTime, value);
                }
            }
        }


        public bool ShouldCheckAirPressure
        {
            get => _shouldCheckAirPressure;
            set
            {
                if (_shouldCheckAirPressure != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.ShouldCheckAir, value);
                    SetProperty(ref _shouldCheckAirPressure, value);
                }

            }
        }

        private bool _enableATC = false;
        private bool _promptWhenGoingToTouchPlate;

        public void WriteCentroidATCParamsOff()
        {
            try
            {
                _pipe.parameter.SetValue(ParameterKey.CentroidHasATC, 0);
                _pipe.parameter.SetValue(ParameterKey.CentroidHasEnhancedATC, 0);
                _pipe.parameter.SetValue(ParameterKey.CentroidATCType, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to write parameters");
            }
        }

        public bool EnableATC
        {
            get => _enableATC;
            set
            {
                if (_enableATC != value)
                {

                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableATC, value);
                    SetProperty(ref _enableATC, value);
                    if (value)
                    {
                        WriteCentroidATCParamsOff();
                    }
                }

            }
        }

        public bool EnableVirtualDrawbar
        {
            get => _enableVirtualDrawbar;
            set
            {
                if (_enableVirtualDrawbar != value)
                {
                    _pipe.parameter.SetToolOptionValue(ATCToolOptions.EnableVirtualDrawbar, value); // throws on error
                    SetProperty(ref _enableVirtualDrawbar, value);
                }
            }
        }

        public int PocketCount
        {
            get => _pocketCount;
            set
            {
                if (_pocketCount != value)
                {
                    _pipe.parameter.SetValue(ParameterKey.MaxToolBins, value); // throws on error
                    SetProperty(ref _pocketCount, value);
                }
            }
        }

    }

}
