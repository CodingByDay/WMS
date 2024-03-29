﻿using Android.Content;
using Android.Views;

namespace WMS.App
{
    internal class UnfinishedTakeoverAdapter : BaseAdapter
    {
        public List<UnfinishedTakeoverList> sList;
        private Context sContext;

        public UnfinishedTakeoverAdapter(Context context, List<UnfinishedTakeoverList> list)
        {
            sList = list;
            sContext = context;
        }

        public override int Count
        {
            get
            {
                return sList.Count;
            }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View row = convertView;
            try
            {
                if (row == null)
                {
                    row = LayoutInflater.From(sContext).Inflate(Resource.Layout.UnfinishedTakeover, null, false);
                }

                TextView Document = row.FindViewById<TextView>(Resource.Id.Document);
                Document.Text = sList[position].Document;
                Document.SetTextColor(Android.Graphics.Color.Black);

                TextView Issuer = row.FindViewById<TextView>(Resource.Id.Issuer);
                Issuer.Text = sList[position].Issuer;
                Issuer.SetTextColor(Android.Graphics.Color.Black);
                TextView Date = row.FindViewById<TextView>(Resource.Id.Date);
                Date.Text = sList[position].Date;
                Date.SetTextColor(Android.Graphics.Color.Black);

                TextView NumberOfPositions = row.FindViewById<TextView>(Resource.Id.NumberOfPositions);
                NumberOfPositions.Text = sList[position].NumberOfPositions;
                NumberOfPositions.SetTextColor(Android.Graphics.Color.Black);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return row;
        }
    }
}