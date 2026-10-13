using System.ComponentModel.DataAnnotations;
using BusinessLayer.Dtos;

namespace WebUI.Models;

public sealed class ProfileEditModel
{
    [Required, StringLength(100), Display(Name = "FirstName")] public string FirstName { get; set; } = "";
    [Required, StringLength(100), Display(Name = "LastName")] public string LastName { get; set; } = "";
    public IFormFile? Avatar { get; set; }
}

public sealed class ProfilePasswordModel
{
    [Required, DataType(DataType.Password), Display(Name = "CurrentPassword")] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password), Display(Name = "NewPassword")] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword), ErrorMessage = "PasswordMismatch"), DataType(DataType.Password), Display(Name = "ConfirmPassword")] public string ConfirmPassword { get; set; } = "";
}

public sealed record ProfileViewModel(ProfileEditModel Profile, string? Email, string? Avatar, PaginatedList<FavoriteBlogDto> Favorites);
public sealed record BlogInteractionViewModel(int BlogId, string? Slug, BlogInteractionDto Interaction, bool CanInteract);
