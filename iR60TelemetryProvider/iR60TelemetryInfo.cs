using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using iRSDKSharp;

namespace SimFeedback.telemetry.iR60
{
    /// <summary>
    /// This class provides the telemetry data by looking up the value from the data struct
    /// and does some stateful calculations where more than one data sample is required.
    /// </summary>
    public sealed class iR60TelemetryInfo : EventArgs, TelemetryInfo
    {
        private readonly iRacingSDK _telemetryData;

        private float _lastLFshockDefl;
        private float _lastLRshockDefl;
        private float _lastRFshockDefl;
        private float _lastRRshockDefl;

        public iR60TelemetryInfo(iRacingSDK sdk)
        {
            _telemetryData = sdk;
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

                default:
                    int arrayIndexPos = -1;
                    int squareBracketPos = name.IndexOf('[');
                    if (squareBracketPos != -1)
                    {
                        int.TryParse(name.Substring(squareBracketPos + 1, 1), out arrayIndexPos);
                        name = name.Substring(0, squareBracketPos);
                    }

                    if (_telemetryData.VarHeaders.ContainsKey(name))
                    {
                        data = _telemetryData.GetData(name);
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
                float speed = (float)_telemetryData.GetData("Speed");

                if (speed > 5)
                {
                    float VelocityX = (float)_telemetryData.GetData("VelocityX");
                    float VelocityY = (float)_telemetryData.GetData("VelocityY");
                    float YawRate = (float)_telemetryData.GetData("YawRate");
                    // Porsche GT3 Cup
                    // Fahrzeug Länge: 4.564
                    // Radstand: 1.980 x 2.456
                    float t1 = VelocityY - YawRate * (1.980f / 2);  // Breite 
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
                float _LFshockDefl = (float)_telemetryData.GetData("LFshockDefl");
                float _LRshockDefl = (float)_telemetryData.GetData("LRshockDefl");
                float _RFshockDefl = (float)_telemetryData.GetData("RFshockDefl");
                float _RRshockDefl = (float)_telemetryData.GetData("RRshockDefl");

                const float x = 1000.0f;
                float[] data =
                    {
                    _LFshockDefl - _lastLFshockDefl,
                    _LRshockDefl - _lastLRshockDefl,
                    _RFshockDefl - _lastRFshockDefl,
                    _RRshockDefl - _lastRRshockDefl
                };

                _lastLFshockDefl = _LFshockDefl;
                _lastLRshockDefl = _LRshockDefl;
                _lastRFshockDefl = _RFshockDefl;
                _lastRRshockDefl = _RRshockDefl;

                return data.Max() * x;
            }
        }
    }
}
