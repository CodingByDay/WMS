﻿namespace TrendNET.WMS.Device.App
{
    public class Echo
    {
        public static bool IsWebAppReady(out string result)
        {
            if (WebApp.Get("Echo.asdp", out result))
            {
                if (result.Contains("OK!"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}