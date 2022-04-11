namespace RealisticLoans
{
    public struct TaxClass
    {
        public static readonly TaxClass[] All =
        {
            new TaxClass(ItemClass.Service.Residential, ItemClass.SubService.ResidentialLow),
            new TaxClass(ItemClass.Service.Residential, ItemClass.SubService.ResidentialHigh),
            new TaxClass(ItemClass.Service.Commercial, ItemClass.SubService.CommercialLow),
            new TaxClass(ItemClass.Service.Commercial, ItemClass.SubService.CommercialHigh),
            new TaxClass(ItemClass.Service.Industrial, ItemClass.SubService.None),
            new TaxClass(ItemClass.Service.Office, ItemClass.SubService.None),
        };

        public TaxClass(ItemClass.Service service, ItemClass.SubService subService)
        {
            Service = service;
            SubService = subService;
        }

        public ItemClass.Service Service;
        public ItemClass.SubService SubService;
    }
}