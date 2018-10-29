using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimFeedback.log;
using System;
using System.Diagnostics;
using System.Threading;

namespace SimFeedback.telemetry.iR60
{
    /// <summary>
    /// iRacing 60hz Telemetry Provider
    /// </summary>
    public sealed class iR60TelemetryProvider : AbstractTelemetryProvider
    {
        private const string sharedMemoryFile = @"Local\IRSDKMemMapFileName"; // the name of the shared memory file
        private bool isStopped = true;                                  // flag to control the polling thread
        private Thread t;                                               // the polling thread, reads telemetry data and sends TelemetryUpdated events

        /// <summary>
        /// Default constructor.
        /// Every TelemetryProvider needs a default constructor for dynamic loading.
        /// Make sure to call the underlying abstract class in the constructor.
        /// </summary>
        public iR60TelemetryProvider() : base()
        {
            Author = "JamesClonk";
            Version = "v0.1";
            BannerImage = @"img\banner_ir60.png"; // Image shown on top of the profiles tab
            IconImage = @"img\ir60.jpg";          // Icon used in the tree view for the profile
            TelemetryUpdateFrequency = 100;     // the update frequency in samples per second
        }

        /// <summary>
        /// Name of this TelemetryProvider.
        /// Used for dynamic loading and linking to the profile configuration.
        /// </summary>
        public override string Name { get { return "ir60"; } }

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing iR60TelemetryProvider");
        }

        /// <summary>
        /// A list of all telemetry names of this provider.
        /// </summary>
        /// <returns>List of all telemetry names</returns>
        public override string[] GetValueList()
        {
            return null; // TODO: return correct list of telemetry names
        }

        /// <summary>
        /// Start the polling thread
        /// </summary>
        public override void Start()
        {
            if (isStopped)
            {
                LogDebug("Starting iR60TelemetryProvider");
                isStopped = false;
                t = new Thread(Run);
                t.Start();
            }
        }

        /// <summary>
        /// Stop the polling thread
        /// </summary>
        public override void Stop()
        {
            LogDebug("Stopping iR60TelemetryProvider");
            isStopped = true;
            if (t != null) t.Join();
        }


        /// <summary>
        /// The thread funktion to poll the telemetry data and send TelemetryUpdated events.
        /// </summary>
        private void Run()
        {
            iRSDK lastTelemetryData = new iRSDK();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!isStopped)
            {
                try
                {
                    // get data from game, 
                    // if an exception will be thrown, we could not retrieve the data because the game 
                    // is not running or something went wrong
                    iRSDK telemetryData = (iRSDK)readSharedMemory(typeof(iRSDK), sharedMemoryFile);
                    // otherwise we are connected
                    IsConnected = true;

                    if (telemetryData.PacketId != lastTelemetryData.PacketId)
                    {
                        IsRunning = true;

                        sw.Restart();

                        TelemetryEventArgs args = new TelemetryEventArgs(
                            new iR60TelemetryInfo(telemetryData, lastTelemetryData));
                        RaiseEvent(OnTelemetryUpdate, args);

                        lastTelemetryData = telemetryData;
                    }
                    else if (sw.ElapsedMilliseconds > 500)
                    {
                        IsRunning = false;
                    }

                    Thread.Sleep(SamplePeriod);
                }
                catch (Exception e)
                {
                    LogError("iR60TelemetryProvider Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }
            }

            IsConnected = false;
            IsRunning = false;
        }

    }
}
