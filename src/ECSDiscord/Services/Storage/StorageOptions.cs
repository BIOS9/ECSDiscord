using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Storage;

public class StorageOptions
{
    public static string Name => "Storage";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; }
}