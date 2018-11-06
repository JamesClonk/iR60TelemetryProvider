using System;
using System.Linq;
using iRSDKSharp;

namespace SimFeedback.telemetry.iR60
{
    /// <summary>
    /// This class provides the telemetry data by looking up the value from the data struct
    /// and does some stateful calculations where more than one data sample is required.
    /// </summary>
    public sealed class iR60TelemetryInfo : EventArgs, TelemetryInfo
    {
        private readonly iRacingSDK _sdk;
        private readonly Session _session;
        private const float G = 9.81f; 

        public iR60TelemetryInfo(iRacingSDK sdk, Session session)
        {
            _sdk = sdk;
            _session = session;
        }

        public TelemetryValue TelemetryValueByName(string name)
        {
            object data;

            TelemetryValue tv;
            switch (name)
            {
                case "SlipAngle":
                    data = SlipAngle;
                    break;
                case "Rumble":
                    data = Rumble;
                    break;
                case "RumbleHz":
                    data = RumbleHz;
                    break;
                case "VertAccel":
                    data = VertAccel;
                    break;

                default:
                    if (_sdk.VarHeaders.ContainsKey(name))
                    {
                        data = _sdk.GetData(name);
                    }
                    else
                    {
                        throw new UnknownTelemetryValueException(name);
                    }
                    break;
            }

            tv = new iR60TelemetryValue(name, data);
            object value = tv.Value;
            if (value == null)
            {
                throw new UnknownTelemetryValueException(name);
            }
            return tv;
        }

        private float SlipAngle
        {
            get
            {
                float v = 0.0f;
                float speed = (float)_sdk.GetData("Speed");

                if (speed > 5)
                {
                    float VelocityX = (float)_sdk.GetData("VelocityX");
                    float VelocityY = (float)_sdk.GetData("VelocityY");
                    float YawRate = (float)_sdk.GetData("YawRate");
                    // Porsche GT3 Cup
                    // Fahrzeug Länge: 4.564
                    // Radstand: 1.980 x 2.456
                    float t1 = VelocityY - YawRate * (1.980f / 2); // Breite 
                    float t2 = VelocityX - YawRate * (2.456f / 2); // Länge 
                    v = (float)(Math.Atan(t1 / t2) * (180.0 / Math.PI));
                }
                return v;
            }
        }

        private float Rumble
        {
            get
            {
                float _LFshockDefl = (float)_sdk.GetData("LFshockDefl");
                float _LRshockDefl = (float)_sdk.GetData("LRshockDefl");
                float _RFshockDefl = (float)_sdk.GetData("RFshockDefl");
                float _RRshockDefl = (float)_sdk.GetData("RRshockDefl");

                const float x = 1000.0f;
                float[] data =
                    {
                    _LFshockDefl - (float)_session.Get("LFshockDefl", 0.0f),
                    _LRshockDefl - (float)_session.Get("LRshockDefl", 0.0f),
                    _RFshockDefl - (float)_session.Get("RFshockDefl", 0.0f),
                    _RRshockDefl - (float)_session.Get("RRshockDefl", 0.0f)
                };

                _session.Set("LFshockDefl", _LFshockDefl);
                _session.Set("LRshockDefl", _LRshockDefl);
                _session.Set("RFshockDefl", _RFshockDefl);
                _session.Set("RRshockDefl", _RRshockDefl);

                return data.Max() * x;
            }
        }

        private float RumbleHz
        {
            get
            {
                float[] data =
                    {
                    (float)_sdk.GetData("TireLF_RumblePitch"),
                    (float)_sdk.GetData("TireRF_RumblePitch"),
                    (float)_sdk.GetData("TireLR_RumblePitch"),
                    (float)_sdk.GetData("TireRR_RumblePitch")
                };

                return data.Max();
            }
        }

        private float VertAccel
        {
            get
            {
                return (float)(
                    (float)_sdk.GetData("VertAccel") 
                    * Math.Cos((float)_sdk.GetData("Pitch")) 
                    * Math.Cos((float)_sdk.GetData("Roll")) - G) / G;
            }
        }
    }
}
