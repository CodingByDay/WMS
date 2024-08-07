﻿using Square.Picasso;
using WMS.ExceptionStore;

public class CustomToolbar
{
    private readonly Activity _activity;
    public readonly AndroidX.AppCompat.Widget.Toolbar _toolbar;
    private readonly int _navIconImageViewId;

    public CustomToolbar(Activity activity, AndroidX.AppCompat.Widget.Toolbar toolbar, int navIconImageViewId)
    {
        try
        {
            _activity = activity;
            _toolbar = toolbar;
            _navIconImageViewId = navIconImageViewId;
        }
        catch (Exception ex)
        {
            GlobalExceptions.ReportGlobalException(ex);
        }
    }

    public void SetNavigationIcon(string imageUrl, ImageView image = null)
    {
        try
        {
            try
            {
                ImageView navIconImageView = _toolbar.FindViewById<ImageView>(_navIconImageViewId);

                // Load and set the image with Picasso
                Picasso.With(_activity)
                    .Load(imageUrl)
                    .Into(navIconImageView);

                // Make the ImageView visible
                navIconImageView.Visibility = Android.Views.ViewStates.Visible;
            }
            catch
            {
                return;
            }
        }
        catch (Exception ex)
        {
            GlobalExceptions.ReportGlobalException(ex);
        }
    }
}
