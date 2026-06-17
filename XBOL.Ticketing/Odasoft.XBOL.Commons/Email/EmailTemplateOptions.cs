using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Odasoft.XBOL.Commons.Email;

/// <summary>
/// Branding, asset, and contact settings used by email templates.
/// </summary>
public class EmailTemplateOptions
{
    [Description("Primary brand color used for email CTAs and links")]
    public string Primary { get; set; } = "#d4a12f";

    [Description("Darker primary brand color used for hover states")]
    public string PrimaryDarken { get; set; } = "#bd8817";

    [Description("Lighter primary brand color used for subtle backgrounds")]
    public string PrimaryLighten { get; set; } = "#fff8e0";

    [Description("Secondary brand color used for body copy")]
    public string Secondary { get; set; } = "#556e79";

    [Description("Darker secondary brand color used for headings")]
    public string SecondaryDarken { get; set; } = "#3a4950";

    [Description("Lighter secondary brand color used for subtle borders and backgrounds")]
    public string SecondaryLighten { get; set; } = "#ebeef1";

    [Description("Email page background color")]
    public string Background { get; set; } = "#FAFAFA";

    [Description("Email content surface color")]
    public string Surface { get; set; } = "#FFFFFF";

    [Description("Success status color")]
    public string Success { get; set; } = "#32bd52";

    [Description("Error status color")]
    public string Error { get; set; } = "#e84238";

    [Description("Warning status color")]
    public string Warning { get; set; } = "#f78d26";

    [Description("Information status color")]
    public string Info { get; set; } = "#3a8bec";

    [MinLength(1)]
    [Description("CSS font-family used by email templates")]
    public string FontFamily { get; set; } = "'Open Sans', Helvetica, Arial, sans-serif";

    [MinLength(1)]
    [Description("Relative path to the logo asset copied into the worker output")]
    public string LogoPath { get; set; } = "Assets/logo/PWRTickets-color@3x.png";

    [Url]
    [Description("Public logo URL for templates that use externally hosted images")]
    public string LogoUrl { get; set; } = "https://placehold.co/200x80/EEE/31343C?text=PWR+TICKET";

    [Required]
    [Description("Public help URL or admin-portal-relative path linked from email footers")]
    public string HelpUrl { get; set; } = "";

    [Required]
    [Description("Public support/contact URL or admin-portal-relative path for email templates")]
    public string SupportUrl { get; set; } = "";

    [Required]
    [EmailAddress]
    [Description("Support email address shown in email footers")]
    public string SupportEmail { get; set; } = "";

    [Required]
    [Description("Public terms and conditions URL for email templates")]
    public string PublicTermsAndConditionsUrl { get; set; } = "";
}
