using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CentroidAPI;
using System.Xml.Linq;
using System.IO;
using System.Globalization;
using System.Xml.XPath;

namespace ToolRackSetup
{
    class RuntimeController
    {
        CNCPipe _pipe;

        DateTime _startTime = DateTime.MinValue; // The actual time the job started running
        TimeSpan _lastActualRuntime = TimeSpan.Zero; // Will be .Zero if it hasn't been run
        int _lineCount = 0;
        int _currentLineNumber = 0;
        bool _jobIsRunning = false;
        string _jobFilename = string.Empty;
        double _percentageThrough = 0;

        public RuntimeController(CNCPipe pipe)
        {
            _pipe = pipe;
            Update();
        }

        private void UpdateState()
        {

        }

        private static int GetLineCount(string filename)
        {
            if (filename.Length > 0 && File.Exists(filename))
            {
                var lineCount = 0;
                using (var reader = File.OpenText(filename))
                {
                    while (reader.ReadLine() != null)
                    {
                        lineCount++;
                    }
                }
                return lineCount;
            } else
            {
                return 0;
            }
        }

        private void LoadCurrentJobDetails()
        {
            _pipe.state.GetFullPathJobNameCurrent(out _jobFilename);
            string currentJobName = "";
            _pipe.state.GetJobNameCurrent(out currentJobName);
            
            // Synchronous load or async this on a background thread
            _lineCount = GetLineCount(_jobFilename);
            _currentLineNumber = 0;
            _startTime = DateTime.Now;
            // If we already started, see what the time elapsed is..


            _percentageThrough = 0;
            // See if we ran this job and load the runtime
            _lastActualRuntime = LoadSavedRuntimeForJob(_jobFilename);            

            Debug.Print("JobFileName: {0}, lineCount: {1} currentJobName : {2}", _jobFilename, _lineCount, currentJobName);

        }

        private bool GetIfLastJobRanSuccessfully()
        {
            string[] messages = new string[0];
            _pipe.message_window.GetMessages(out messages);
            if (messages.Length > 0) {
                string lastMessage = messages[messages.Length - 1];
                return lastMessage.Equals("306 Job finished", StringComparison.InvariantCultureIgnoreCase);

            } else
            {
                return false;
            }
        }

        private const string cncmPath = "c:\\cncm\\";
      //  private const string corbinsWorkshopPath = cncmPath + "CorbinsWorkshop\\";
        private const string savedRuntimeFilePath = cncmPath + "JobRuntimes.xml";

        // Format:
        // <Jobs>
        //   <Job>
        //       <Path>filename path</Path>
        //       <Runtime>234.223<Runtime>

        static TimeSpan LoadSavedRuntimeForJob(string jobFilename)
        {
            try
            {
                if (File.Exists(savedRuntimeFilePath))
                {
                    XDocument doc = XDocument.Load(savedRuntimeFilePath);
                    string query = String.Format("/Jobs/Job[Path='{0}']", jobFilename);
                    XElement? element = doc.XPathSelectElement(query);
                    if (element != null)
                    {
                        // Validate the last mod time hasn't changed
                        if (File.Exists(jobFilename))
                        {
                            // Todo: invariant culture for dates
                            if (DateTime.TryParse(element.XPathSelectElement("DateModified")?.Value, out var lastRuntimeDateMod))
                            {
                                DateTime lastModified = File.GetLastWriteTime(jobFilename);
                                // If it was modified since we did the last run, don't use it..
                                if (lastModified > lastRuntimeDateMod)
                                {
                                    return TimeSpan.Zero;
                                }

                            }
                        }

                        if (TimeSpan.TryParse(element.XPathSelectElement("Runtime")?.Value, out var runtimeDuration))
                        {
                            return runtimeDuration;
                        }                        
                    }
                }
            }
            catch (Exception e) {
                Debug.Print("LoadSavedRuntimeForJob exception: {0}", e.Message);
            } // supress exceptions
            return TimeSpan.Zero;
        }

        static void SaveRuntimeForJob(string jobFilename, TimeSpan runtime)
        {
            XDocument doc;
            XElement? topLevelJobs = null;
            XElement? jobElement = null;
            if (File.Exists(savedRuntimeFilePath))
            {
                doc = XDocument.Load(savedRuntimeFilePath);
                string query = String.Format("/Jobs/Job[Path='{0}']", jobFilename);
                jobElement = doc.XPathSelectElement(query);
            } else
            {
                doc = new XDocument();
            }
            if (topLevelJobs == null)
            {
                topLevelJobs = new XElement("Jobs");
                doc.Add(topLevelJobs);
            }

            if (jobElement == null)
            {
                jobElement = new XElement("Job");
                topLevelJobs.Add(jobElement);
            } else
            {
                jobElement.RemoveAll(); // Start clean..
            }

            DateTime lastModified = DateTime.Now;
            if (File.Exists(jobFilename))
            {
                lastModified = File.GetLastWriteTime(jobFilename);
            }

            jobElement.Add(new XElement("Path", jobFilename));
            jobElement.Add(new XElement("DateModified", lastModified)); // should probably be culture invariant 
            jobElement.Add(new XElement("Runtime", runtime.ToString()));

            doc.Save(savedRuntimeFilePath);
        }


        private void JobIsRunningChangedTo(bool value)
        {
            _jobIsRunning = value;
            Debug.WriteLine("Job Running State changed to: {0}", _jobIsRunning);
            if (_jobIsRunning)
            {
                LoadCurrentJobDetails();

            }
            else
            {
                // Save the times now
                DateTime endTime = DateTime.Now;
                TimeSpan actualRuntime = endTime - _startTime;
                // Job isn't running anymore; figure out if we ended successfully or not
                if (GetIfLastJobRanSuccessfully())
                {
                    // Make sure we are at the end for the UI display
                    _percentageThrough = 1.0;
                    _currentLineNumber = _lineCount;
                    // Save off the info for the next run of the same job
                    SaveRuntimeForJob(_jobFilename, actualRuntime);
                }
            }
        }

        private void UpdatePercentageThroughJob()
        {
            Debug.Assert(_jobIsRunning);
            int current_line_number = 0;
            int program_number = 0;

            _pipe.state.GetCurrentStackLevelZeroLineInfo(out current_line_number, out program_number);

            // Only update for the main program at stack 0; otherwise, we get a line number from a different file that is running
            _currentLineNumber = current_line_number;

            if (_lineCount > 0) {
                _percentageThrough = (double)_currentLineNumber / (double)_lineCount * 100.0;
            } else
            {
                _percentageThrough = 0;
            }

            TimeSpan timePassed = DateTime.Now - _startTime;

            TimeSpan timeLeft = TimeSpan.Zero;
            if (_lastActualRuntime == TimeSpan.Zero)
            {
                // If we haven't run this before, do an estimate for when we will end based on the time passed
                if (_percentageThrough > 0) { 
                    TimeSpan estimatedRuntime = timePassed / _percentageThrough; // Estimate..
                    timeLeft = estimatedRuntime - timePassed;
                }
            } else
            {
                timeLeft = _lastActualRuntime - timePassed;
            }
            if (timeLeft < TimeSpan.Zero)
            {
                timeLeft = TimeSpan.Zero;
            }

            string timeLeftStr = String.Format("{0:00}:{1:00}:{2:00}", timeLeft.Hours, timeLeft.Minutes, timeLeft.Seconds);


            Debug.WriteLine("line {0} / {1}, percent: {2}, TimeLeft: {3}", _currentLineNumber, _lineCount, _percentageThrough, timeLeftStr);

        }

        public void Update()
        {
            // See if our state is changing
            bool newJobIsRunning = _pipe.IsJobRunning();
            if (newJobIsRunning != _jobIsRunning)
            {
                JobIsRunningChangedTo(newJobIsRunning);
            } else
            {
                if (_jobIsRunning)
                {
                    // If we are running, update the percentage
                    UpdatePercentageThroughJob();
                }
            }

        }
    }
}
