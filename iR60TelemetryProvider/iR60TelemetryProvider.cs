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
            Version = "v1.0";
            BannerImage = @"img\banner_iracing.png"; // Image shown on top of the profiles tab
            IconImage = @"img\iracing.jpg";          // Icon used in the tree view for the profile
            TelemetryUpdateFrequency = 60;     // the update frequency in samples per second
        }

        /// <summary>
        /// Name of this TelemetryProvider.
        /// Used for dynamic loading and linking to the profile configuration.
        /// </summary>
        public override string Name { get { return "iracing 60hz"; } }

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
                "Brake", "BrakeRaw", "Clutch",
                "DriverMarker", "EngineWarnings",
                "FuelLevel", "FuelLevelPct", "FuelPress",
                "Gear", "HandbrakeRaw", "IsOnTrack",
                "LatAccel", "LFshockDefl", "LFshockVel", "LongAccel", "LRshockDefl", "LRshockVel",
                "ManifoldPress", "OilLevel", "OilPress", "OilTemp", "OnPitRoad",
                "Pitch", "PitchRate",
                "RFshockDefl", "RFshockVel", "Roll", "RollRate", "RPM", "RRshockDefl", "RRshockVel",
                "Rumble", "RumbleHz",
                "ShiftGrindRPM", "ShiftIndicatorPct", "ShiftPowerPct",
                "SlipAngle", "Speed",
                "SteeringWheelAngle", "SteeringWheelPctTorque", "SteeringWheelTorque",
                "Throttle",
                "TireLF_RumblePitch", "TireRF_RumblePitch", "TireLR_RumblePitch", "TireRR_RumblePitch",
                "VelocityX", "VelocityY", "VelocityZ", "VertAccel",
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
            int _lastSessionTick = 0;

            iRacingSDK sdk = new iRacingSDK();
            Session session = new Session();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!isStopped)
            {
                try
                {
                    // check if the SDK is connected
                    if (sdk.IsConnected())
                    {
                        IsConnected = true;

                        // check if car is on track and if we got new data
                        if ((bool)sdk.GetData("IsOnTrack") && _lastSessionTick != (int)sdk.GetData("SessionTick"))
                        {
                            IsRunning = true;
                            _lastSessionTick = (int)sdk.GetData("SessionTick");

                            sw.Restart();

                            TelemetryEventArgs args = new TelemetryEventArgs(new iR60TelemetryInfo(sdk, session));
                            RaiseEvent(OnTelemetryUpdate, args);
                        }
                        else if (sw.ElapsedMilliseconds > 500)
                        {
                            IsRunning = false;
                        }
                    }
                    else
                    {
                        sdk.Startup();

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

            sdk.Shutdown();

            IsConnected = false;
            IsRunning = false;
        }

    }
}
