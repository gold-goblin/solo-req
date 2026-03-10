using SoloReq.Models;

namespace SoloReq.Services;

public class CookieService
{
    private readonly List<CookieItem> _cookies = new();
    public bool AutoSendEnabled { get; set; } = true;

    public IReadOnlyList<CookieItem> Cookies => _cookies.AsReadOnly();

    public void AddCookie(CookieItem cookie)
    {
        var existing = _cookies.FindIndex(c => c.Name == cookie.Name && c.Domain == cookie.Domain);
        if (existing >= 0)
            _cookies[existing] = cookie;
        else
            _cookies.Add(cookie);
    }

    public void RemoveCookie(CookieItem cookie) => _cookies.Remove(cookie);

    public void Clear() => _cookies.Clear();

    public List<CookieItem> GetCookiesForDomain(string domain)
    {
        return _cookies.Where(c => domain.EndsWith(c.Domain) || c.Domain == domain).ToList();
    }

    public void ParseSetCookieHeaders(IEnumerable<string> setCookieHeaders, string requestDomain)
    {
        foreach (var header in setCookieHeaders)
        {
            var cookie = ParseSetCookie(header, requestDomain);
            if (cookie != null)
                AddCookie(cookie);
        }
    }

    private CookieItem? ParseSetCookie(string header, string defaultDomain)
    {
        var parts = header.Split(';', StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        var nameValue = parts[0].Split('=', 2);
        if (nameValue.Length < 2) return null;

        var cookie = new CookieItem
        {
            Name = nameValue[0].Trim(),
            Value = nameValue[1].Trim(),
            Domain = defaultDomain
        };

        for (int i = 1; i < parts.Length; i++)
        {
            var attr = parts[i].Split('=', 2);
            var attrName = attr[0].Trim().ToLowerInvariant();
            var attrValue = attr.Length > 1 ? attr[1].Trim() : "";

            switch (attrName)
            {
                case "domain": cookie.Domain = attrValue.TrimStart('.'); break;
                case "path": cookie.Path = attrValue; break;
                case "expires":
                    if (DateTime.TryParse(attrValue, out var exp)) cookie.Expires = exp;
                    break;
                case "httponly": cookie.HttpOnly = true; break;
                case "secure": cookie.Secure = true; break;
            }
        }

        return cookie;
    }
}
