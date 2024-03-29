﻿namespace WMS.App
{
    internal class CleanupLocation
    {
        public string Name { get; set; }
        public string Ident { get; set; }
        public string Location { get; set; }
        public string SSCC { get; set; }
        public string Serial { get; set; }

        /// <summary>
        ///  Empty constructor.
        /// </summary>
        public CleanupLocation()
        {
        }

        /// <summary>
        ///  Full constructor.
        /// </summary>
        /// <param name="Location"></param>
        /// <param name="SSCC"></param>
        /// <param name="Ident"></param>
        public CleanupLocation(string Name, string Ident, string Location, string SSCC, string Serial)
        {
            this.Name = Name;
            this.Ident = Ident;
            this.Location = Location;
            this.SSCC = SSCC;
            this.Serial = Serial;
        }
    }
}