using System.ComponentModel.DataAnnotations;

namespace ECSDiscord.Services.Enrollments;

public class EnrollmentsOptions
{
    public static string Name => "Enrollments";

    [Required] public bool RequireVerificationToJoin { get; init; }
}