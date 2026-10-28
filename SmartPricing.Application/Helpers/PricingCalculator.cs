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

    /// <summary>Apply rounding rule pattern (.99, .50, .05, etc.) to a price</summary>
    public static decimal ApplyRounding(decimal price, string pattern)
    {
        var floor = Math.Floor(price);
        var suffix = pattern.TrimStart('0');  // ".99" → ".99", ".50" → ".50"

        if (decimal.TryParse(suffix, out var dec))
        {
            var candidate = floor + dec;
            return candidate > price ? candidate - 1 : candidate;
        }

        // Fallback: round to nearest .05
        return Math.Round(price * 20, MidpointRounding.AwayFromZero) / 20;
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
