namespace ZororoMemorialPark.Data.Models
{
    public class tbl_RequisitionActionToken
    {
        public int Id { get; set; }

        public Guid Token { get; set; }

        public int RequisitionId { get; set; }
        public int? WorkflowStepId { get; set; }

        public string UserId { get; set; }

        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; }
    }
}
