namespace BDTechMarket.Models.ViewModels
{
    public class SalesSummaryVM
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}