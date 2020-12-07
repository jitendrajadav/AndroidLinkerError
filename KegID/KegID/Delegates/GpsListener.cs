﻿using Shiny.Locations;
using System;
using System.Threading.Tasks;

namespace KegID.Delegates
{
    public sealed class GPSLocationService : IGpsDelegate
    {
        public Task OnReading(IGpsReading reading)
        {
            return Task.CompletedTask;
        }
    }

    public class GpsListener : IGpsListener
    {
        public event EventHandler<GpsReadingEventArgs> OnReadingReceived;

        void UpdateReading(IGpsReading reading)
        {
            OnReadingReceived?.Invoke(this, new GpsReadingEventArgs(reading));
        }

        public class LocationDelegate : IGpsDelegate
        {
            IGpsListener _gpsListener;

            public LocationDelegate(IGpsListener gpsListener)
            {
                _gpsListener = gpsListener;
            }

            public Task OnReading(IGpsReading reading)
            {
                (_gpsListener as GpsListener)?.UpdateReading(reading);
                return Task.CompletedTask;
            }
        }
    }
}
