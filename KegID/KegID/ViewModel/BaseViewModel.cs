﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acr.UserDialogs;
using KegID.Localization;
using KegID.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Mvvm;
using Prism.Navigation;

namespace KegID.ViewModel
{
    public abstract class BaseViewModel : BindableBase, INavigationAware, IInitializeAsync, IDestructible
    {
        public IUserDialogs PageDialog = UserDialogs.Instance;
        public IApiManager ApiManager;
        public IApiService<IAccountApi> _accountApi = new ApiService<IAccountApi>(ConstantManager.BaseUrl);
        public IApiService<IDashboardApi> _dashboardApi = new ApiService<IDashboardApi>(ConstantManager.BaseUrl);
        public IApiService<IFillApi> _fillApi = new ApiService<IFillApi>(ConstantManager.BaseUrl);
        public IApiService<IMaintainApi> _maintainApi = new ApiService<IMaintainApi>(ConstantManager.BaseUrl);
        public IApiService<IMoveApi> _moveApi = new ApiService<IMoveApi>(ConstantManager.BaseUrl);
        public IApiService<IPalletApi> _palletApi = new ApiService<IPalletApi>(ConstantManager.BaseUrl);

        /*
         * Define Fields
         */

        public bool IsBusy { get; set; }
        protected INavigationService NavigationService { get; private set; }

        public LocalizedResources Resources
        {
            get;
        }

        protected BaseViewModel(INavigationService navigationService)
        {
            NavigationService = navigationService;
            ApiManager = new ApiManager(_accountApi, _dashboardApi, _fillApi, _maintainApi, _moveApi, _palletApi);
            Resources = new LocalizedResources(typeof(KegIDResource), App.CurrentLanguage);
        }

        public async Task RunSafe(Task task/*, bool ShowLoading = true, string loadinMessage = null*/)
        {
            try
            {
                if (IsBusy) return;
                IsBusy = true;
                await task;
            }
            catch(Exception ex) //TODO: restrict this
            {
                IsBusy = false;
                UserDialogs.Instance.HideLoading();
                await Prism.PrismApplicationBase.Current.MainPage.DisplayAlert("Eror", "Something goes wrong while making service call", "Ok");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {

        }


        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {

        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public virtual async Task InitializeAsync(INavigationParameters parameters)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {

        }

        public virtual void Destroy()
        {

        }

        public JsonSerializerSettings GetJsonSetting()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Converters = new List<JsonConverter> { new CustomIntConverter() }
            };
        }

        public class CustomIntConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => (objectType == typeof(int));

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JValue jsonValue = serializer.Deserialize<JValue>(reader);

                if (jsonValue.Type == JTokenType.Float)
                {
                    return (int)Math.Round(jsonValue.Value<double>());
                }
                else if (jsonValue.Type == JTokenType.Integer)
                {
                    return jsonValue.Value<int>();
                }

                throw new FormatException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {

            }
        }
    }
}
