namespace Backend.Application.Categories;

/// <summary>
/// Valid category icon identifiers. The frontend lib/icon-registry.ts must mirror this list; drift between the two is a bug.
/// </summary>
public static class AllowedCategoryIcons
{
    public static IReadOnlySet<string> Values { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "DeliveryTruck01Icon",
        "Mortarboard02Icon",
        "Wrench01Icon",
        "ShoppingBag03Icon",
        "Bone01Icon",
        "SparklesIcon",
        "RunningShoesIcon",
        "MoreHorizontalCircle01Icon",
        "Briefcase01Icon",
        "BubbleChatIcon",
        "HandHelpingIcon",
        "FavouriteIcon",
        "StarIcon",
        "ToolboxIcon",
        "Calendar01Icon",
        "BabyBottleIcon"
    };
}
