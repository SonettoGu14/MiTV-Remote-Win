using System.Globalization;
using System.Resources;

namespace MiTVRemote;

public static class L
{
    private static readonly ResourceManager _rm = new("MiTVRemote.Localization.Strings", typeof(L).Assembly);

    public static string S(string key) => _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string S(string key, params object[] args)
    {
        var format = _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        return string.Format(format, args);
    }
}
