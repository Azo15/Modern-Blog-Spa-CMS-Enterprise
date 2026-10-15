using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

/// <summary>Neutral, entity-specific artwork for content without an uploaded image.</summary>
[AllowAnonymous]
public sealed class PublicMediaController : Controller
{
    [HttpGet("/media/placeholder/{kind}/{key}.svg")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult Placeholder(string kind, string key)
    {
        if (kind is not ("blog" or "service" or "gallery") || key.Length != 24 ||
            key.Any(c => !Uri.IsHexDigit(c))) return NotFound();
        // Only numeric values from a validated hash enter the SVG; no user markup is rendered.
        var hue = int.Parse(key[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture) % 35 + 140;
        var x = int.Parse(key[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture) % 500 + 350;
        var y = int.Parse(key[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture) % 250 + 230;
        var rotation = int.Parse(key[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture) % 180;
        var svg = FormattableString.Invariant($"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="830" viewBox="0 0 1200 830">
              <defs><radialGradient id="a"><stop stop-color="hsl({hue},28%,26%)"/><stop offset="1" stop-color="#081c15"/></radialGradient></defs>
              <rect width="1200" height="830" fill="url(#a)"/>
              <g transform="translate({x} {y}) rotate({rotation})" fill="none" stroke="#b89550" stroke-width="2" opacity=".65">
                <ellipse rx="210" ry="90"/><ellipse rx="210" ry="90" transform="rotate(60)"/><ellipse rx="210" ry="90" transform="rotate(120)"/>
                <circle r="240" opacity=".3"/>
              </g>
              <path d="M80 750h1040" stroke="#b89550" opacity=".35"/>
            </svg>
            """);
        return Content(svg, "image/svg+xml", Encoding.UTF8);
    }
}
