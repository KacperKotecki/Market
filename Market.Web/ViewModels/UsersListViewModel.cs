namespace Market.Web.ViewModels;

using Market.Web.Models.Admin;
public class UsersListViewModel
{
    public List<AdminUserDto> Users { get; set; } = new();
    
    public string? SearchString { get; set; }
    public string? SortOrder { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}