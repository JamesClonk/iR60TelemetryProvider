using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimFeedback.log;
using System;
using System.Diagnostics;
using System.Threading;
using iRSDKSharp;

namespace SimFeedback.telemetry.iR60
{
    /// <summary>
    /// iRacing 60hz Telemetry Provider
    /// </summary>
    public sealed class iR60TelemetryProvider : AbstractTelemetryProvider
    {
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
            BannerImage = @"img\banner_iracing.png"; // Image shown on top of the profiles tab
            IconImage = @"img\iracing.jpg";          // Icon used in the tree view for the profile
            TelemetryUpdateFrequency = 60;     // the update frequency in samples per second
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
            string[] values = {
                "Brake", "Clutch",
                "DriverMarker", "EngineWarnings",
                "FuelLevel", "FuelLevelPct", "FuelPress",
                "Gear", "HandbrakeRaw",
                "IsOnTrack",
                "LatAccel", "LFshockDefl", "LongAccel", "LRshockDefl",
                "ManifoldPress",
                "OilLevel", "OilPress", "OilTemp",
                "OnPitRoad",
                "Pitch", "PitchRate",
                "RFshockDefl", "Roll", "RollRate", "RPM", "RRshockDefl",
                "Rumble",
                "ShiftGrindRPM", "ShiftIndicatorPct", "ShiftPowerPct",
                "SlipAngle",
                "Speed",
                "SteeringWheelAngle", "SteeringWheelPctTorque", "SteeringWheelTorque",
                "Throttle",
                "VelocityX", "VelocityY", "VelocityZ",
                "VertAccel",
                "Voltage",  "WaterLevel", "WaterTemp",
                "Yaw", "YawRate",
            };
            return values;
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
            iRacingSDK sdk = new iRacingSDK();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!isStopped)
            {
                try
                {
                    IsConnected = true;

                    // check if the SDK is connected
                    if (sdk.IsConnected())
                    {
                        IsConnected = true;
                        IsRunning = true;
                        sw.Restart();

                        TelemetryEventArgs args = new TelemetryEventArgs(new iR60TelemetryInfo(sdk));
                        RaiseEvent(OnTelemetryUpdate, args);
                    }
                    else if (sdk.IsInitialized)
                    {
                        sdk.Shutdown();
                        IsRunning = false;
                        IsConnected = false;
                    }
                    else if (sw.ElapsedMilliseconds > 500)
                    {
                        IsRunning = false;
                    }
                    else
                    {
                        sdk.Startup();
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
