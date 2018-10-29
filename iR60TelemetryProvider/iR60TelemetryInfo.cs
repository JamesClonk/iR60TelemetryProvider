using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SimFeedback.telemetry.iR60
{
    /// <summary>
    /// This class provides the telemetry data by looking up the value from the data struct
    /// and does some stateful calculations where more than one data sample is required.
    /// </summary>
    public sealed class iR60TelemetryInfo : EventArgs, TelemetryInfo
    {
        private readonly iRSDK _telemetryData;
        private readonly iRSDK _lastTelemetryData;

        public iR60TelemetryInfo(iRSDK telemetryData, iRSDK lastTelemetryData)
        {
            _telemetryData = telemetryData;
            _lastTelemetryData = lastTelemetryData;
        }

        public TelemetryValue TelemetryValueByName(string name)
        {
            object data;

            TelemetryValue tv;
            switch (name)
            {
                default:
                    int arrayIndexPos = -1;
                    int squareBracketPos = name.IndexOf('[');
                    if (squareBracketPos != -1)
                    {
                        int.TryParse(name.Substring(squareBracketPos + 1, 1), out arrayIndexPos);
                        name = name.Substring(0, squareBracketPos);
                    }
                    Type eleDataType = typeof(iRSDK);
                    PropertyInfo propertyInfo;
                    FieldInfo fieldInfo = eleDataType.GetField(name);
                    if (fieldInfo != null)
                    {
                        data = fieldInfo.GetValue(_telemetryData);
                        if (arrayIndexPos != -1 && data.GetType().IsArray)
                        {
                            float[] array = (float[])data;
                            data = array[arrayIndexPos];
                        }

                    }
                    else if ((propertyInfo = eleDataType.GetProperty(name)) != null)
                    {
                        data = propertyInfo.GetValue(_telemetryData, null);
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
    }
}
