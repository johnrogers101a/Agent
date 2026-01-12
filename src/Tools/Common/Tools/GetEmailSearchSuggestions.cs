#nullable enable

using AgentFramework.Attributes;

namespace Common.Tools;

/// <summary>
/// Provides email search suggestions for finding receipts, orders, and confirmations from various categories of senders.
/// </summary>
public class GetEmailSearchSuggestions
{
    // Region-specific sender data
    private static readonly Dictionary<string, RegionData> s_regions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["US"] = new RegionData
        {
            Food = new CategoryData
            {
                Senders = ["doordash.com", "ubereats.com", "grubhub.com", "eat.grubhub.com", "postmates.com", "caviar.com", "seamless.com", "instacart.com"],
                FastFood = ["tacobell.com", "info.tacobell.com", "mcdonalds.com", "chipotle.com", "chickfila.com", "wendys.com", "subway.com", "dominos.com", "e-confirmation.dominos.com", "papajohns.com", "pizzahut.com", "burgerking.com", "jackinthebox.com", "deltaco.com", "innout.com", "carls.com", "sonic.com", "arbys.com", "kfc.com", "popeyes.com", "panerabread.com", "jimmyjohns.com", "jerseymikes.com", "firehousesubs.com", "qdoba.com"],
                Coffee = ["starbucks.com", "dunkin.com", "peets.com", "coffeebean.com"],
                Dining = ["applebees.com", "chilis.com", "olivegarden.com", "redlobster.com", "outback.com", "buffalowildwings.com", "tgifridays.com", "dennys.com", "ihop.com", "crackerbarrel.com"],
                SearchTerms = ["order confirmation", "your order", "delivery", "receipt"]
            },
            Shopping = new CategoryData
            {
                Senders = ["amazon.com", "ship-confirm@amazon.com", "auto-confirm@amazon.com", "walmart.com", "target.com", "costco.com", "samsclub.com", "bestbuy.com", "homedepot.com", "lowes.com", "kohls.com", "macys.com", "nordstrom.com", "jcpenney.com"],
                Online = ["ebay.com", "etsy.com", "wish.com", "newegg.com", "overstock.com", "wayfair.com", "zappos.com", "chewy.com"],
                Clothing = ["gap.com", "oldnavy.com", "nike.com", "adidas.com", "hm.com", "zara.com", "uniqlo.com", "forever21.com", "asos.com"],
                Electronics = ["apple.com", "microsoft.com", "samsung.com", "dell.com", "hp.com", "lenovo.com"],
                Pharmacy = ["cvs.com", "walgreens.com", "riteaid.com"],
                Grocery = ["instacart.com", "freshdirect.com", "shipt.com", "safeway.com", "kroger.com", "publix.com", "wholefoodsmarket.com", "traderjoes.com"],
                SearchTerms = ["order", "shipped", "delivery", "confirmation", "receipt", "invoice"]
            },
            Travel = new CategoryData
            {
                Senders = ["united.com", "delta.com", "aa.com", "southwest.com", "jetblue.com", "spirit.com", "frontier.com", "alaska-air.com"],
                Hotels = ["marriott.com", "hilton.com", "ihg.com", "hyatt.com", "wyndham.com", "choicehotels.com", "airbnb.com", "vrbo.com", "booking.com", "expedia.com"],
                RentalCars = ["enterprise.com", "hertz.com", "avis.com", "budget.com", "nationalcar.com"],
                Rideshare = ["uber.com", "lyft.com"],
                SearchTerms = ["confirmation", "itinerary", "reservation", "booking", "trip"]
            },
            Bills = new CategoryData
            {
                Senders = ["att.com", "verizon.com", "tmobile.com", "xfinity.com", "spectrum.com", "cox.com"],
                Utilities = ["pge.com", "sce.com", "sdge.com", "duke-energy.com", "coned.com"],
                Streaming = ["netflix.com", "hulu.com", "disneyplus.com", "hbomax.com", "spotify.com", "apple.com", "youtube.com", "amazonprime.com"],
                Insurance = ["geico.com", "progressive.com", "statefarm.com", "allstate.com", "usaa.com"],
                SearchTerms = ["bill", "statement", "payment", "invoice", "due"]
            }
        },
        ["UK"] = new RegionData
        {
            Food = new CategoryData
            {
                Senders = ["deliveroo.com", "just-eat.com", "ubereats.com"],
                FastFood = ["mcdonalds.com", "kfc.com", "burgerking.com", "dominos.com", "pizzahut.com", "subway.com", "greggs.com", "nandos.com"],
                Coffee = ["starbucks.com", "costa.co.uk", "caffenero.com", "pret.com"],
                Dining = ["wagamama.com", "pizza-express.com", "zizzi.co.uk", "prezzo.co.uk", "frankie-and-bennys.com"],
                SearchTerms = ["order confirmation", "your order", "delivery", "receipt"]
            },
            Shopping = new CategoryData
            {
                Senders = ["amazon.co.uk", "tesco.com", "sainsburys.co.uk", "asda.com", "morrisons.com", "argos.co.uk", "johnlewis.com", "currys.co.uk", "ao.com"],
                Online = ["ebay.co.uk", "etsy.com", "asos.com", "boohoo.com", "prettylittlething.com", "next.co.uk", "very.co.uk"],
                Clothing = ["hm.com", "zara.com", "primark.com", "topshop.com", "riverisland.com", "newlook.com", "marksandspencer.com"],
                Electronics = ["apple.com", "samsung.com", "currys.co.uk", "ao.com"],
                Pharmacy = ["boots.com", "superdrug.com", "lloydspharmacy.com"],
                Grocery = ["ocado.com", "tesco.com", "sainsburys.co.uk", "asda.com", "waitrose.com"],
                SearchTerms = ["order", "dispatched", "delivery", "confirmation", "receipt"]
            },
            Travel = new CategoryData
            {
                Senders = ["britishairways.com", "easyjet.com", "ryanair.com", "jet2.com", "virginatlantic.com", "tui.co.uk"],
                Hotels = ["booking.com", "hotels.com", "expedia.co.uk", "airbnb.com", "laterooms.com", "premierinn.com", "travelodge.co.uk"],
                RentalCars = ["enterprise.co.uk", "hertz.co.uk", "avis.co.uk", "europcar.co.uk"],
                Rideshare = ["uber.com", "bolt.eu", "freenow.com"],
                SearchTerms = ["confirmation", "itinerary", "reservation", "booking"]
            },
            Bills = new CategoryData
            {
                Senders = ["ee.co.uk", "vodafone.co.uk", "three.co.uk", "o2.co.uk", "bt.com", "sky.com", "virginmedia.com"],
                Utilities = ["britishgas.co.uk", "edf.com", "eon.com", "scottishpower.co.uk", "octopus.energy"],
                Streaming = ["netflix.com", "nowtv.com", "disneyplus.com", "spotify.com", "apple.com", "amazonprime.com"],
                Insurance = ["admiral.com", "directline.com", "aviva.co.uk", "comparethemarket.com"],
                SearchTerms = ["bill", "statement", "payment", "invoice", "direct debit"]
            }
        }
    };

    /// <summary>
    /// Gets email search suggestions for a category like "food", "shopping", "travel", or "bills".
    /// Returns sender domains and search terms optimized for Gmail and Hotmail search.
    /// </summary>
    /// <param name="category">Category: "food", "shopping", "travel", "bills", or "all"</param>
    /// <param name="region">Region code: "US", "UK", etc. Defaults to "US"</param>
    [McpTool]
    public SearchSuggestionsResponse Execute(string category, string region = "US")
    {
        if (!s_regions.TryGetValue(region, out var regionData))
        {
            regionData = s_regions["US"]; // Default to US
        }

        var suggestions = new SearchSuggestionsResponse();

        var categoryLower = category.ToLowerInvariant();
        
        if (categoryLower is "food" or "restaurant" or "delivery" or "all")
        {
            AddCategorySuggestions(suggestions, "Food & Delivery", regionData.Food);
        }
        
        if (categoryLower is "shopping" or "retail" or "orders" or "purchases" or "all")
        {
            AddCategorySuggestions(suggestions, "Shopping & Retail", regionData.Shopping);
        }
        
        if (categoryLower is "travel" or "flights" or "hotels" or "trips" or "all")
        {
            AddCategorySuggestions(suggestions, "Travel", regionData.Travel);
        }
        
        if (categoryLower is "bills" or "utilities" or "subscriptions" or "all")
        {
            AddCategorySuggestions(suggestions, "Bills & Subscriptions", regionData.Bills);
        }

        if (suggestions.Categories.Count == 0)
        {
            // Unknown category - return all
            AddCategorySuggestions(suggestions, "Food & Delivery", regionData.Food);
            AddCategorySuggestions(suggestions, "Shopping & Retail", regionData.Shopping);
            AddCategorySuggestions(suggestions, "Travel", regionData.Travel);
            AddCategorySuggestions(suggestions, "Bills & Subscriptions", regionData.Bills);
        }

        suggestions.Region = region;
        suggestions.GmailSearchTip = "Use OR to combine: from:amazon.com OR from:walmart.com. Add date filter: after:2025/12/01";
        suggestions.HotmailSearchTip = "Use simple terms: 'amazon' or 'order shipped'. Complex queries may fail.";

        return suggestions;
    }

    private static void AddCategorySuggestions(SearchSuggestionsResponse response, string name, CategoryData data)
    {
        var allSenders = new List<string>();
        allSenders.AddRange(data.Senders);
        if (data.FastFood != null) allSenders.AddRange(data.FastFood);
        if (data.Coffee != null) allSenders.AddRange(data.Coffee);
        if (data.Dining != null) allSenders.AddRange(data.Dining);
        if (data.Online != null) allSenders.AddRange(data.Online);
        if (data.Clothing != null) allSenders.AddRange(data.Clothing);
        if (data.Electronics != null) allSenders.AddRange(data.Electronics);
        if (data.Pharmacy != null) allSenders.AddRange(data.Pharmacy);
        if (data.Grocery != null) allSenders.AddRange(data.Grocery);
        if (data.Hotels != null) allSenders.AddRange(data.Hotels);
        if (data.RentalCars != null) allSenders.AddRange(data.RentalCars);
        if (data.Rideshare != null) allSenders.AddRange(data.Rideshare);
        if (data.Utilities != null) allSenders.AddRange(data.Utilities);
        if (data.Streaming != null) allSenders.AddRange(data.Streaming);
        if (data.Insurance != null) allSenders.AddRange(data.Insurance);

        response.Categories.Add(new CategorySuggestion
        {
            Name = name,
            Senders = allSenders.Distinct().ToList(),
            SearchTerms = data.SearchTerms.ToList()
        });
    }

    private class RegionData
    {
        public CategoryData Food { get; set; } = new();
        public CategoryData Shopping { get; set; } = new();
        public CategoryData Travel { get; set; } = new();
        public CategoryData Bills { get; set; } = new();
    }

    private class CategoryData
    {
        public string[] Senders { get; set; } = [];
        public string[]? FastFood { get; set; }
        public string[]? Coffee { get; set; }
        public string[]? Dining { get; set; }
        public string[]? Online { get; set; }
        public string[]? Clothing { get; set; }
        public string[]? Electronics { get; set; }
        public string[]? Pharmacy { get; set; }
        public string[]? Grocery { get; set; }
        public string[]? Hotels { get; set; }
        public string[]? RentalCars { get; set; }
        public string[]? Rideshare { get; set; }
        public string[]? Utilities { get; set; }
        public string[]? Streaming { get; set; }
        public string[]? Insurance { get; set; }
        public string[] SearchTerms { get; set; } = [];
    }
}

public class SearchSuggestionsResponse
{
    public string Region { get; set; } = "US";
    public List<CategorySuggestion> Categories { get; set; } = [];
    public string GmailSearchTip { get; set; } = "";
    public string HotmailSearchTip { get; set; } = "";
}

public class CategorySuggestion
{
    public string Name { get; set; } = "";
    public List<string> Senders { get; set; } = [];
    public List<string> SearchTerms { get; set; } = [];
}
