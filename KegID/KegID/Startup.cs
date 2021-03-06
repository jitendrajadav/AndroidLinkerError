﻿using KegID.Delegates;
using Microsoft.Extensions.DependencyInjection;
using Prism.DryIoc;
using Shiny;
using Shiny.Prism;

namespace KegID
{
    public class Startup : PrismStartup
    {
        public Startup() : base(PrismContainerExtension.Current)
        {

        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            //var job = new JobInfo(typeof(BackgroundJob), nameof(BackgroundJob))
            //{
            //    Repeat = true,
            //    BatteryNotLow = true,
            //    DeviceCharging = true,
            //    RequiredInternetAccess = InternetAccess.Unmetered
            //};

            services.UseGps<GPSLocationService>();
            services.AddSingleton<IGpsListener, GpsListener>();
            services.UseGps<GpsListener.LocationDelegate>();

            //services.RegisterJob(job);
        }
    }
}
