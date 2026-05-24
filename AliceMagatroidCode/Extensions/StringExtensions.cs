using MainFile = AliceMagatroid_Mod.MainFile;
using AliceMagatroid_Mod.Utils;

namespace AliceMagatroid_Mod.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", path);
    }

    public static string CardImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Cards", path);
    }

    public static string BigCardImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Cards", "Big", path);
    }

    public static string PowerImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Powers", path);
    }
    public static string BigPowerImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Powers", "Big", path);
    }

    public static string RelicImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Relics", path);
    }

    public static string BigRelicImagePath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Relics", "Big", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return AliceHelper.AssetPath("Images", "Charui", path);
    }
}
