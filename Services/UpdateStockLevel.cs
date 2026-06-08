namespace ZororoMemorialPark.Services
{
    public class UpdateStockLevel
    {
        public string GetStockStatus(double quantity)
        {
            if (quantity <= 0)
                return "Out";

            if (quantity > 10)
                return "InStock";

            return "Low";
        }
    }
}
