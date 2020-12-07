using System.Threading.Tasks;

namespace KegID.Services
{
    public interface IInitializeMetaData
    {
        Task LoadBatchAsync();
        Task LoadPartnersAsync();
        Task LoadOperators();
        Task LoadMaintainTypeAsync();
        Task LoadAssetSizeAsync();
        Task LoadAssetTypeAsync();
        Task LoadAssetVolumeAsync();
        Task LoadOwnerAsync();
        Task LoadDashboardPartnersAsync();
        Task LoadBrandAsync();
        Task LoadPartnerTypeAsync();
        Task LoadGetSkuListAsync();
        void DeleteInitializeMetaData();
    }
}
