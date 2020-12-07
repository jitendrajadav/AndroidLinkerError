using KegID.Views;
using KegID.ViewModel;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Prism.DryIoc;
using Prism.Ioc;
using KegID.Services;
using Xamarin.Forms;
using Prism.Plugin.Popups;
using Microsoft.AppCenter.Distribute;
using System;
using System.Threading.Tasks;
using KegID.DependencyServices;
using KegID.Common;
using Xamarin.Essentials;
using Prism;
using AsyncAwaitBestPractices;
using System.Diagnostics;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace KegID
{
    public partial class App : PrismApplication
    {
        public static string CurrentLanguage = "EN";
        protected override IContainerExtension CreateContainerExtension() => PrismContainerExtension.Current;

        public App() : this(null) { }

        public App(IPlatformInitializer initializer) : base(initializer) { }

        protected override async void OnInitialized()
        {
            SafeFireAndForgetExtensions.SetDefaultExceptionHandling(LogException);
            InitializeComponent();

            VersionTracking.Track();
            Xamarin.Forms.Device.SetFlags(new[] { "CarouselView_Experimental", "SwipeView_Experimental" });

            switch (Xamarin.Forms.Device.RuntimePlatform)
            {
                case Xamarin.Forms.Device.Android:
                    var permission = await DependencyService.Get<IPermission>().VerifyStoragePermissions();
                    break;
            }

            //TaskScheduler.UnobservedTaskException += (sender, e) =>
            //{
            //    Logger.Log(e.Exception.Message);
            //};

#if DEBUG
            //HotReloader.Current.Run(this);
            ConstantManager.BaseUrl = ConstantManager.TestApiUrl;
#elif RELEASE
            ConstantManager.BaseUrl = ConstantManager.StageApiUrl;
#endif
            var versionUpdated = VersionTracking.CurrentVersion.CompareTo(VersionTracking.PreviousVersion);
            if (string.IsNullOrEmpty(Settings.UserId))
            {
                await NavigationService.NavigateAsync("LoginView");
            }
            else if (versionUpdated > 0 && VersionTracking.IsFirstLaunchForCurrentVersion && VersionTracking.PreviousVersion != null)
            {
                await NavigationService.NavigateAsync("WhatIsNewView");
            }
            else
            {
                if (TargetIdiom.Tablet == Xamarin.Forms.Device.Idiom)
                    await NavigationService.NavigateAsync("MainPageTablet");
                else
                    await NavigationService.NavigateAsync("MainPage");
            }
        }
        private void LogException(Exception obj)
        {
            Debug.WriteLine(obj.Message);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //Main Navigation for MasterPage.
            //containerRegistry.RegisterForNavigation<NavigationPage>();

            containerRegistry.RegisterForNavigation<LoginView, LoginViewModel>();
            containerRegistry.RegisterForNavigation<MainPage, MainViewModel>();

            //Popup Navigation Register
            containerRegistry.RegisterPopupNavigationService();

            //Services Register
            containerRegistry.Register<IInitializeMetaData, InitializeMetaData>();
            containerRegistry.Register<IZebraPrinterManager, ZebraPrinterManager>();
            containerRegistry.Register<IManifestManager, ManifestManager>();
            containerRegistry.Register<IGetIconByPlatform, GetIconByPlatform>();
            containerRegistry.Register<ISyncManager, SyncManager>();
            containerRegistry.Register<IUuidManager, UuidManager>();
            containerRegistry.Register<ICalcCheckDigitMngr, CalcCheckDigitMngr>();
        }

        protected async override void OnStart()
        {
            await Distribute.SetEnabledAsync(true);
            // In this example OnReleaseAvailable is a method name in same class
            Distribute.ReleaseAvailable = OnReleaseAvailable;

            //AppCenter.LogLevel = LogLevel.Verbose;
            AppCenter.Start("uwp=0404c586-124c-4b55-8848-910689b6881b;" +
                   "android=31ceef42-fd24-49d3-8e7e-21f144355dde;" +
                   "ios=b80b8476-04cf-4fc3-b7f7-be06ba7f2213",
                   typeof(Analytics), typeof(Crashes), typeof(Distribute));

            var _syncManager = Container.Resolve<SyncManager>();
            _syncManager.NotifyConnectivityChanged();
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        private bool OnReleaseAvailable(ReleaseDetails releaseDetails)
        {
            // Look at releaseDetails public properties to get version information, release notes text or release notes URL
            string versionName = releaseDetails.ShortVersion;
            string versionCodeOrBuildNumber = releaseDetails.Version;
            string releaseNotes = releaseDetails.ReleaseNotes;
            Uri releaseNotesUrl = releaseDetails.ReleaseNotesUrl;

            // custom dialog
            var title = "Version " + versionName + " available!";
            Task answer;

            // On mandatory update, user cannot postpone
            if (releaseDetails.MandatoryUpdate)
            {
                answer = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install");
            }
            else
            {
                answer = Current.MainPage.DisplayAlert(title, releaseNotes, "Download and Install", "Maybe tomorrow...");
            }
            answer.ContinueWith((task) =>
            {
                // If mandatory or if answer was positive
                if (releaseDetails.MandatoryUpdate || (task as Task<bool>)?.Result == true)
                {
                    // Notify SDK that user selected update
                    Distribute.NotifyUpdateAction(UpdateAction.Update);
                }
                else
                {
                    // Notify SDK that user selected postpone (for 1 day)
                    // Note that this method call is ignored by the SDK if the update is mandatory
                    Distribute.NotifyUpdateAction(UpdateAction.Postpone);
                }
            });

            // Return true if you are using your own dialog, false otherwise
            return true;
        }
    }
}
