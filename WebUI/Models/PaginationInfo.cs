namespace WebUI.Models
{
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }



        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public int TotalPages => (int)System.Math.Ceiling((decimal)TotalItems / ItemsPerPage);
    }
}
