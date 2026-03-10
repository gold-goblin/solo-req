using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SoloReq.Services;

public class JsonFormatterService
{
    public string Format(string json)
    {
        try
        {
            var obj = JToken.Parse(json);
            return obj.ToString(Formatting.Indented);
        }
        catch
        {
            return json;
        }
    }

    public string Minify(string json)
    {
        try
        {
            var obj = JToken.Parse(json);
            return obj.ToString(Formatting.None);
        }
        catch
        {
            return json;
        }
    }

    public bool IsValid(string json)
    {
        try
        {
            JToken.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
