namespace Aether.PluginSDK.UI;

public class WidgetBuilder
{
    // Helper methods to build JSON layout
    public static string CreateButon(string label, string actionId)
    {
        return $@"{{ ""type"": ""button"", ""label"": ""{label}"", ""action_id"": ""{actionId}"" }}";
    }
}
