﻿using Android.Content;
using Android.Net;

namespace WMS.App
{
    public static class LoaderManifest
    {
        private static ProgressDialogClass progress;
        private static Context current;

        public static void LoaderManifestLoop(Context context)
        {
            progress = new ProgressDialogClass();
            progress.ShowDialogSync(context, "Ni internetne povezave... Povezovanje...");
        }

        public static void LoaderManifestLoopResources(Context context)
        {
            progress = new ProgressDialogClass();
            progress.ShowDialogSync(context, "Pridobivamo resurse, počakajte.");
        }

        public static void destroy()
        {
            try
            {
                if (progress != null)
                {
                    progress.StopDialogSync();
                }
            }
            catch
            {
                return;
            }
        }

        public static void LoaderManifestLoopStop(Context context)
        {
            try
            {
                if (progress != null)
                {
                    progress.StopDialogSync();
                }
            }
            catch
            {
                return;
            }
        }

        public static NetworkInfo GetNetworkInfo(Context context)
        {
            ConnectivityManager cm = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            return cm.ActiveNetworkInfo;
        }

        /**
	     * Check if there is any connectivity
	     * @param context
	     * @return
	     */

        public static bool IsConnected(Context context)
        {
            NetworkInfo info = GetNetworkInfo(context);
            return (info != null && info.IsConnected);
        }
    }
}