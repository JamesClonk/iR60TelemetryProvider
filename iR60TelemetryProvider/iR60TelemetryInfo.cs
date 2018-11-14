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
                case "LongAccel":
                    data = LongAccel;
                    break;
                case "LatAccel":
                    data = LatAccel;
                    break;

                case "Pitch":
                    data = RadianToDegree("Pitch") * -1; // invert Pitch
                    break;
                case "Roll":
                    data = RadianToDegree("Roll");
                    break;
                case "Yaw":
                    data = RadianToDegree("Yaw");
                    break;
                case "PitchRate":
                    data = RadianToDegree("PitchRate");
                    break;
                case "RollRate":
                    data = RadianToDegree("RollRate");
                    break;
                case "YawRate":
                    data = RadianToDegree("YawRate");
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

        private float GetFloat(string name, float defaultValue = 0.0f)
        {
            if (_sdk.VarHeaders.ContainsKey(name))
            {
                return (float)_sdk.GetData(name);
            }
            else
            {
                return defaultValue;
            }
        }

        private float RadianToDegree(string name)
        {
            return GetFloat(name) * (float)(180 / Math.PI);
        }

        private float SlipAngle
        {
            get
            {
                float v = 0.0f;
                float speed = GetFloat("Speed");

                if (speed > 5)
                {
                    float VelocityX = GetFloat("VelocityX");
                    float VelocityY = GetFloat("VelocityY");
                    float YawRate = GetFloat("YawRate");
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
                float _CFshockDefl = GetFloat("CFshockDefl");
                float _LFshockDefl = GetFloat("LFshockDefl");
                float _LRshockDefl = GetFloat("LRshockDefl");
                float _RFshockDefl = GetFloat("RFshockDefl");
                float _RRshockDefl = GetFloat("RRshockDefl");

                const float x = 1000.0f;
                float[] data =
                    {
                    _CFshockDefl - (float)_session.Get("CFshockDefl", 0.0f),
                    _LFshockDefl - (float)_session.Get("LFshockDefl", 0.0f),
                    _LRshockDefl - (float)_session.Get("LRshockDefl", 0.0f),
                    _RFshockDefl - (float)_session.Get("RFshockDefl", 0.0f),
                    _RRshockDefl - (float)_session.Get("RRshockDefl", 0.0f)
                };

                _session.Set("CFshockDefl", _CFshockDefl);
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
                    GetFloat("TireLF_RumblePitch"),
                    GetFloat("TireRF_RumblePitch"),
                    GetFloat("TireLR_RumblePitch"),
                    GetFloat("TireRR_RumblePitch")
                };

                return data.Max();
            }
        }

        private float VertAccel
        {
            get
            {
                return (float)(GetFloat("VertAccel") 
                    * Math.Cos(GetFloat("Pitch")) 
                    * Math.Cos(GetFloat("Roll")) - G) / G;
            }
        }

        private float LongAccel
        {
            get
            {
                return (float)(GetFloat("LongAccel") * (Math.Cos(GetFloat("Pitch")) / G));
            }
        }

        private float LatAccel
        {
            get
            {
                return (float)(GetFloat("LatAccel") * (Math.Cos(GetFloat("Roll")) / G));
            }
        }
    }
}
