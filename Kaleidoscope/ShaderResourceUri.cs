namespace Kaleidoscope;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/Kaleidoscope;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
