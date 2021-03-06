﻿using Acr.UserDialogs;
using KegID.Common;
using KegID.DependencyServices;
using KegID.LocalDb;
using KegID.Messages;
using KegID.Model;
using KegID.Services;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Navigation;
using Realms;
using Scandit.BarcodePicker.Unified;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Zebra.Sdk.Printer.Discovery;

namespace KegID.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        #region Properties

        private readonly IInitializeMetaData _initializeMetaData;
        private readonly IUuidManager _uuidManager;

        public string DraftmaniFests { get; set; }
        public bool IsVisibleDraftmaniFestsLabel { get; set; }
        public string APIBase { get; set; }

        public string Stock { get; set; }
        public string Empty { get; set; }
        public string InUse { get; set; }
        public string Total { get; set; }
        public string AverageCycle { get; set; }
        public string Atriskegs { get; set; }

        public ObservableCollection<Dashboard> Dashboards { get; set; } = new ObservableCollection<Dashboard>();

        #endregion

        #region Commands

        public DelegateCommand MoreCommand { get; }
        public DelegateCommand MaintainCommand { get; }
        public DelegateCommand PalletizeCommand { get; }
        public DelegateCommand PalletsCommand { get; }
        public DelegateCommand FillCommand { get; }
        public DelegateCommand ManifestCommand { get; }
        public DelegateCommand StockCommand { get; }
        public DelegateCommand EmptyCommand { get; }
        public DelegateCommand PartnerCommand { get; }
        public DelegateCommand KegsCommand { get; }
        public DelegateCommand InUsePartnerCommand { get; }
        public DelegateCommand MoveCommand { get; }

        #endregion

        #region Constructor

        public MainViewModel(INavigationService navigationService, IInitializeMetaData initializeMetaData, IUuidManager uuidManager) : base(navigationService)
        {
            _initializeMetaData = initializeMetaData;
            _uuidManager = uuidManager;

            MoveCommand = new DelegateCommand(MoveCommandRecieverAsync);
            MoreCommand = new DelegateCommand(MoreCommandRecieverAsync);
            MaintainCommand = new DelegateCommand(MaintainCommandRecieverAsync);
            PalletizeCommand = new DelegateCommand(PalletizeCommandRecieverAsync);
            PalletsCommand = new DelegateCommand(PalletsCommandRecieverAsync);
            FillCommand = new DelegateCommand(FillCommandRecieverAsync);
            ManifestCommand = new DelegateCommand(ManifestCommandRecieverAsync);
            StockCommand = new DelegateCommand(StockCommandRecieverAsync);
            EmptyCommand = new DelegateCommand(EmptyCommandRecieverAsync);
            PartnerCommand = new DelegateCommand(PartnerCommandRecieverAsync);
            KegsCommand = new DelegateCommand(KegsCommandRecieverAsync);
            InUsePartnerCommand = new DelegateCommand(InUsePartnerCommandRecieverAsync);
        }

        #endregion

        #region Methods

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            CheckDraftmaniFests();
        }

        private async Task LoadMetadData()
        {
            if (!Settings.IsMetaDataLoaded)
            {
                Settings.IsMetaDataLoaded = true;
                UserDialogs.Instance.ShowLoading("Wait while downloading meta-data...");

                _initializeMetaData.DeleteInitializeMetaData();
                await RunSafe(_initializeMetaData.LoadBatchAsync());
                await RunSafe(_initializeMetaData.LoadPartnersAsync());
                await RunSafe(_initializeMetaData.LoadOperators());
                await RunSafe(_initializeMetaData.LoadMaintainTypeAsync());
                await RunSafe(_initializeMetaData.LoadAssetSizeAsync());
                await RunSafe(_initializeMetaData.LoadAssetTypeAsync());
                await RunSafe(_initializeMetaData.LoadAssetVolumeAsync());
                await RunSafe(_initializeMetaData.LoadOwnerAsync());
                await RunSafe(_initializeMetaData.LoadDashboardPartnersAsync());
                await RunSafe(_initializeMetaData.LoadBrandAsync());
                await RunSafe(_initializeMetaData.LoadPartnerTypeAsync());
                await RunSafe(_initializeMetaData.LoadGetSkuListAsync());
                UserDialogs.Instance.HideLoading();
            }
        }

        private void StartPrinterSearch()
        {
            DiscoveryHandlerImplementation discoveryHandler = new DiscoveryHandlerImplementation();
            try
            {
                DependencyService.Get<IConnectionManager>().FindBluetoothPrinters(discoveryHandler);
            }
            catch (NotImplementedException)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Bluetooth discovery not supported on this platform", "OK");
                });
            }
        }

        private void HandleUnsubscribeMessages()
        {
            MessagingCenter.Unsubscribe<SettingToDashboardMsg>(this, "SettingToDashboardMsg");
            MessagingCenter.Unsubscribe<InvalidServiceCall>(this, "InvalidServiceCall");
            MessagingCenter.Unsubscribe<CheckDraftmaniFests>(this, "CheckDraftmaniFests");
        }

        private void HandleReceivedMessages()
        {
            MessagingCenter.Subscribe<InvalidServiceCall>(this, "InvalidServiceCall", _ =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    var param = new NavigationParameters
                    {
                        { "IsLogOut",true}
                    };
                    await NavigationService.NavigateAsync("/LoginView", param, useModalNavigation: true, animated: false);
                });
            });

            MessagingCenter.Subscribe<SettingToDashboardMsg>(this, "SettingToDashboardMsg", _ => Device.BeginInvokeOnMainThread(() => RefreshDashboardRecieverAsync(true)));

            MessagingCenter.Subscribe<CheckDraftmaniFests>(this, "CheckDraftmaniFests", _ => Device.BeginInvokeOnMainThread(() => CheckDraftmaniFests()));
        }

        private async void MoveCommandRecieverAsync()
        {
            var result = await NavigationService.NavigateAsync("MoveView", new NavigationParameters
                {
                    { "ManifestId", _uuidManager.GetUuId() }
                }, useModalNavigation : true, animated: false);
        }

        private async void InUsePartnerCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("DashboardPartnersView", null, useModalNavigation: true, animated: false);
        }

        private async void KegsCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("KegSearchView", null, useModalNavigation: true, animated: false);
        }

        private async void PartnerCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("DashboardPartnersView", null, useModalNavigation: true, animated: false);
        }

        private async void StockCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("InventoryView", new NavigationParameters
                    {
                        { "currentPage", 0 }
                    }, useModalNavigation: true, animated: false);
        }

        private async void EmptyCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("InventoryView", new NavigationParameters
                    {
                        { "currentPage", 1 }
                    }, useModalNavigation: true, animated: false);
        }

        private async void ManifestCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("ManifestsView", null, useModalNavigation: true, animated: false);
        }

        internal void CheckDraftmaniFests()
        {
            var RealmDb = Realm.GetInstance(RealmDbManager.GetRealmDbConfig());
            var manifests = RealmDb.All<ManifestModel>().Where(x => x.IsDraft || x.IsQueue).ToList();
            var pallets = RealmDb.All<PalletRequestModel>().Where(x => x.IsQueue).ToList();

            var draft = manifests.Where(x => x.IsDraft).ToList();
            var queue = manifests.Where(x => x.IsQueue).ToList();
            string queueMsg = queue.Count > 1 ? "queued manifests" : "queued manifest";
            string draftMsg = draft.Count > 1 ? "draft manifests" : "draft manifest";
            string palletMsg = pallets.Count > 1 ? "queued pallets" : "queued pallet";

            var current = Connectivity.NetworkAccess;
            if (current == NetworkAccess.Internet)
            {
                if (manifests.Count > 0)
                {
                    if (draft.Count > 0)
                    {
                        if (queue.Count > 0)
                        {
                            DraftmaniFests = pallets.Count > 0
                                ? string.Format("{0} " + draftMsg + ", {1} " + queueMsg + ", {2} " + palletMsg, draft.Count, queue.Count, pallets.Count)
                                : string.Format("{0} " + draftMsg + ", {1} " + queueMsg, draft.Count, queue.Count);
                        }
                        else
                        {
                            DraftmaniFests = pallets.Count > 0
                                ? string.Format("{0} " + draftMsg + ", {1} " + palletMsg, draft.Count, pallets.Count)
                                : string.Format("{0} " + draftMsg, draft.Count);
                        }
                    }
                    else if (queue.Count > 0)
                    {
                        if (pallets.Count > 0)
                        {
                            DraftmaniFests = string.Format("{0} " + queueMsg + ", {1} " + palletMsg, queue.Count, pallets.Count);
                        }
                        DraftmaniFests = string.Format("{0} " + queueMsg, queue.Count);
                    }

                    IsVisibleDraftmaniFestsLabel = true;
                }
                else if (pallets.Count > 0)
                {
                    DraftmaniFests = string.Format("{0} " + palletMsg, pallets.Count);
                }
                else
                {
                    DraftmaniFests = string.Empty;
                    IsVisibleDraftmaniFestsLabel = false;
                }
            }
            else
            {
                if (manifests.Count > 0)
                {
                    if (draft.Count > 0)
                    {
                        if (queue.Count > 0)
                        {
                            DraftmaniFests = pallets.Count > 0
                                ? string.Format("Lost communication with KegID.com, {0} " + draftMsg + ", {1} " + queueMsg + ", {2} " + palletMsg, draft.Count, queue.Count, pallets.Count)
                                : string.Format("Lost communication with KegID.com, {0} " + draftMsg + ", {1} " + queueMsg, draft.Count, queue.Count);
                        }
                        else
                        {
                            DraftmaniFests = pallets.Count > 0
                                ? string.Format("Lost communication with KegID.com, {0} " + draftMsg + ", {1} " + palletMsg, draft.Count, pallets.Count)
                                : string.Format("Lost communication with KegID.com, {0} " + draftMsg, draft.Count);
                        }
                    }
                    else
                    {
                        if (queue.Count > 0)
                        {
                            DraftmaniFests = pallets.Count > 0
                                ? string.Format("Lost communication with KegID.com, {0} " + queueMsg + ", {1} " + palletMsg, queue.Count, pallets.Count)
                                : string.Format("Lost communication with KegID.com, {0} " + queueMsg, queue.Count);
                        }
                    }

                    IsVisibleDraftmaniFestsLabel = true;
                }
                else if (pallets.Count > 0)
                {
                    DraftmaniFests = string.Format("Lost communication with KegID.com, {0} " + palletMsg, pallets.Count);
                }
                else
                {
                    DraftmaniFests = string.Format("Lost communication with KegID.com");
                    IsVisibleDraftmaniFestsLabel = true;
                }
            }
        }

        private async void FillCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("FillView", new NavigationParameters { { "UuId", _uuidManager.GetUuId() } }, useModalNavigation: true, animated: false);
        }

        private async void PalletizeCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("PalletizeView", new NavigationParameters
                    {
                        { "GenerateManifestIdAsync", "GenerateManifestIdAsync" }
                    }, useModalNavigation: true, animated: false);
        }

        private async void PalletsCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("SearchPalletView", null, useModalNavigation: true, animated: false);
        }

        private async void MaintainCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("MaintainView", new NavigationParameters
                    {
                        { "LoadMaintenanceTypeAsync", "LoadMaintenanceTypeAsync" }
                    }, useModalNavigation: true, animated: false);
        }

        public async void RefreshDashboardRecieverAsync(bool refresh = false)
        {
            if (refresh)
                await NavigationService.ClearPopupStackAsync(animated: false);
            var result = await ApiManager.GetDeshboardDetail(Settings.SessionId);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var model = await Task.Run(() => JsonConvert.DeserializeObject<DashboardResponseModel>(response, GetJsonSetting()));
                var total = model.Stock + model.Empty + model.InUse;

                if (TargetIdiom.Tablet == Device.Idiom)
                {
                    Stock = model.Stock.ToString("0,0", CultureInfo.InvariantCulture);
                    Empty = model.Empty.ToString("0,0", CultureInfo.InvariantCulture);
                    InUse = model.InUse.ToString("0,0", CultureInfo.InvariantCulture);
                    Total = total.ToString("0,0", CultureInfo.InvariantCulture);
                    AverageCycle = model.AverageCycle.ToString() + " days";
                    Atriskegs = model.InactiveKegs.ToString();
                }
                else
                {
                    Dashboards.LastOrDefault().Stock = model.Stock.ToString("0,0", CultureInfo.InvariantCulture);
                    Dashboards.LastOrDefault().Empty = model.Empty.ToString("0,0", CultureInfo.InvariantCulture);
                    Dashboards.LastOrDefault().InUse = model.InUse.ToString("0,0", CultureInfo.InvariantCulture);
                    Dashboards.LastOrDefault().Total = total.ToString("0,0", CultureInfo.InvariantCulture);
                    Dashboards.LastOrDefault().AverageCycle = model.AverageCycle.ToString() + " days";
                    Dashboards.LastOrDefault().Atriskegs = model.InactiveKegs.ToString();
                }
            }
        }

        private async void MoreCommandRecieverAsync()
        {
            await NavigationService.NavigateAsync("SettingView", null, useModalNavigation: true, animated: false);
        }

        public override async void OnNavigatedTo(INavigationParameters parameters)
        {
            if (Dashboards.Count == 0 && TargetIdiom.Tablet != Device.Idiom)
            {
                Dashboards.Add(new Dashboard());
                Dashboards.Add(new Dashboard()
                {
                    Stock = "0",
                    Empty = "0",
                    InUse = "0",
                    Total = "0",
                    AverageCycle = "0 day",
                    Atriskegs = "0"
                });
            }
            CheckDraftmaniFests();
            await LoadMetadData();
            base.OnNavigatedTo(parameters);
        }

        public override Task InitializeAsync(INavigationParameters parameters)
        {
            RefreshDashboardRecieverAsync();

            Stock = "0";
            Empty = "0";
            InUse = "0";
            Total = "0";
            AverageCycle = "0 day";
            Atriskegs = "0";

            HandleUnsubscribeMessages();
            HandleReceivedMessages();

            if (Device.RuntimePlatform != Device.UWP)
            {
                StartPrinterSearch();
            }

            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            APIBase = ConstantManager.BaseUrl.Contains("Prod") ? string.Empty : ConstantManager.BaseUrl;

            switch (Device.RuntimePlatform)
            {
                case Device.Android:
                    ScanditService.ScanditLicense.AppKey = Resources["scanditAndroidKey"];
                    break;
                case Device.iOS:
                    ScanditService.ScanditLicense.AppKey = Resources["scanditiOSKey"];
                    break;
            }

            return base.InitializeAsync(parameters);
        }

        #endregion

        private class DiscoveryHandlerImplementation : DiscoveryHandler
        {
            public void DiscoveryError(string message)
            {
                //Device.BeginInvokeOnMainThread(async () => {
                //    await Application.Current.MainPage.DisplayAlert("Discovery Error", message, "OK");
                //});
            }

            public void DiscoveryFinished()
            {
                //Device.BeginInvokeOnMainThread(() =>
                //{
                //});
            }

            public void FoundPrinter(DiscoveredPrinter printer)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (printer.Address == Settings.PrinterAddress)
                    {
                        ConstantManager.PrinterSetting = printer;
                    }
                });
            }
        }
    }
}
