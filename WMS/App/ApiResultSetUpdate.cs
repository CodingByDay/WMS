﻿using TrendNET.WMS.Core.Data;



namespace API
{

    public class ApiResultSetUpdate
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int? Results { get; set; }
        public int? Affected { get; set; }
        public int? Identity { get; set; }
        public List<Row>? Rows { get; set; }


    }

    public class Row
    {
        public NameValueObject ConvertToNameValueObject(String ObjectName)
        {
            NameValueObject RetValRow = new NameValueObject();
            if (this.Items != null && this.Items.Count > 0)
            {
                // NameValueObject creation
                RetValRow.ObjectName = ObjectName;
                // NameValue creation
                foreach (var prop in this.Items)
                {
                    switch (prop.Value)
                    {
                        case string s:
                            RetValRow.SetString(prop.Key, s);
                            break;

                        case int i:
                            RetValRow.SetInt(prop.Key, i);
                            break;

                        case double d:
                            RetValRow.SetDouble(prop.Key, d);
                            break;

                        case bool b:
                            RetValRow.SetBool(prop.Key, b);
                            break;
                        case DateTime dt:
                            RetValRow.SetDateTime(prop.Key, dt);
                            break;
                        default:
                            break;
                    }
                }
            }
            return RetValRow;
        }

        public Dictionary<string, object> Items { get; set; }

        public object GetProperty(string propertyName)
        {
            try
            {
                if (Items.TryGetValue(propertyName, out var value))
                {
                    if (value is object)
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public string StringValue(string propertyName)
        {
            var objectValue = GetProperty(propertyName);
            try
            {
                return (string)objectValue;
            }
            catch
            {
                return null;
            }
        }

        public bool? BoolValue(string propertyName)
        {
            var objectValue = GetProperty(propertyName);
            try
            {
                return (bool?)objectValue;
            }
            catch
            {
                return null;
            }
        }

        public DateTime? DateTimeValue(string propertyName)
        {
            var objectValue = GetProperty(propertyName);
            try
            {
                return (DateTime?)objectValue;
            }
            catch
            {
                return null;
            }
        }

        public Int64? IntValue(string propertyName)
        {
            var objectValue = GetProperty(propertyName);

            var type = objectValue.GetType();
            try
            {
                return (Int64?)objectValue;
            }
            catch
            {
                return null;
            }
        }

        public double? DoubleValue(string propertyName)
        {
            var objectValue = GetProperty(propertyName);
            try
            {
                return (double?)objectValue;
            }
            catch
            {
                return null;
            }
        }
    }

}