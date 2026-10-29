using System.Globalization;
using SmartPricing.Domain.Enums;

namespace SmartPricing.Application.Helpers;

public static class PricingCalculator
{
    /// <summary>Gross Margin % = (SellPrice - Cost) / SellPrice × 100</summary>
    public static decimal GrossMargin(decimal sellPrice, decimal cost)
    {
        if (sellPrice <= 0) return 0;
        return Math.Round((sellPrice - cost) / sellPrice * 100, 2);
    }

    /// <summary>Markup % = (SellPrice - Cost) / Cost × 100</summary>
    public static decimal Markup(decimal sellPrice, decimal cost)
    {
        if (cost <= 0) return 0;
        return Math.Round((sellPrice - cost) / cost * 100, 2);
    }

    /// <summary>Suggested price from cost and target margin %</summary>
    public static decimal SuggestPriceFromMargin(decimal cost, decimal targetMarginPercent)
    {
        if (targetMarginPercent >= 100) throw new ArgumentException("Margin cannot be >= 100%");
        return Math.Round(cost / (1 - targetMarginPercent / 100), 4);
    }

    /// <summary>Suggested price from cost and markup %</summary>
    public static decimal SuggestPriceFromMarkup(decimal cost, decimal markupPercent)
    {
        return Math.Round(cost * (1 + markupPercent / 100), 4);
    }

    /// <summary>
    /// Apply rounding rule pattern (.99, .50, .05, etc.) to a price. Always rounds UP, so the
    /// rounded price never falls below the margin the rule targeted.
    /// Small patterns that divide 1 evenly (.05, .10, .25) are steps: 12.37 → 12.40 for ".05".
    /// Other patterns are price endings: 12.37 → 12.50 for ".50", 12.37 → 12.99 for ".99".
    /// </summary>
    public static decimal ApplyRounding(decimal price, string pattern)
    {
        // Invariant culture: the server may run with a culture that uses ',' as decimal separator (e.g. el-GR).
        if (!decimal.TryParse(pattern, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
            value <= 0 || value >= 1)
            return Math.Ceiling(price * 100) / 100;   // Unknown pattern: whole cents

        if (value < 0.5m && 1 % value == 0)
            return Math.Ceiling(price / value) * value;

        var candidate = Math.Floor(price) + value;
        return candidate >= price ? candidate : candidate + 1;
    }

    /// <summary>Determine color indicator relative to target margin</summary>
    public static ColorIndicator GetColorIndicator(decimal actualMargin, decimal targetMargin)
    {
        if (actualMargin >= targetMargin) return ColorIndicator.Green;
        if (actualMargin >= targetMargin - 2) return ColorIndicator.Orange;
        return ColorIndicator.Red;
    }

    /// <summary>Price change percent (for history)</summary>
    public static decimal? ChangePercent(decimal? oldPrice, decimal newPrice)
    {
        if (oldPrice is null or 0) return null;
        return Math.Round((newPrice - oldPrice.Value) / oldPrice.Value * 100, 2);
    }
}
