﻿using Microsoft.AppCenter.Analytics;
using TrendNET.WMS.Core.Data;

public class ApiResultSet
{
    public bool Success { get; set; }
    public string Error { get; set; }
    public int Results { get; set; }
    public List<Row> Rows { get; set; }

    public NameValueObjectList ConvertToNameValueObjectList(String ObjectName)
    {
        NameValueObjectList RetVal = new NameValueObjectList();
        RetVal.ObjectName = ObjectName;
        RetVal.Items = new List<NameValueObject>();
        if (this.Success && this.Rows.Count > 0)
        {
            foreach (Row row in this.Rows)
            {
                // NameValueObject creation
                NameValueObject RetValRow = new NameValueObject();
                RetValRow.ObjectName = ObjectName;
                // NameValue creation
                foreach (var prop in row.Items)
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

                        case Int64 i64:
                            RetValRow.SetInt(prop.Key, (int)i64);
                            break;
                    }
                }
                RetVal.Items.Add(RetValRow);
            }
        }
        return RetVal;
    }
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

                    default:
                        Analytics.TrackEvent("New data type" + prop.Value.GetType().Name);
                        break;
                }
            }
        }
        return RetValRow;
    }

    public Dictionary<string, object> Items { get; set; }

    public object GetProperty(string propertyName, string type)
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
        var objectValue = GetProperty(propertyName, "string");
        try
        {
            return (string)objectValue;
        }
        catch
        {
            // prijavu v app center pa uporabniku return default value for type
            return null;
        }
    }

    public bool? BoolValue(string propertyName)
    {
        var objectValue = GetProperty(propertyName, "bool");
        try
        {
            return (bool?)objectValue;
        }
        catch
        {
            return null;
        }
    }

    public Int64? IntValue(string propertyName)
    {
        var objectValue = GetProperty(propertyName, "int64");

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
        var objectValue = GetProperty(propertyName, "double");
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