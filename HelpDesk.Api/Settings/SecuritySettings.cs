using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Settings;

internal class SecuritySettings
{
    public const string SectionName = "Security";

    [Required]
    public string ApiKey { get; init; } = string.Empty;
}