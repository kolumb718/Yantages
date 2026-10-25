using UnityEngine;

// Runtime-built scenery finds its shaders here. Custom shaders live in Resources/Shaders and
// Standard is pinned by Resources/Shaders/StandardReference.mat, so player builds keep them all.
public static class ShipShaders
{
    static Shader standard, unlit, windowCity, hologram;

    public static Shader Standard => Get(ref standard, "Standard");
    public static Shader Unlit => Get(ref unlit, "Orbita/Unlit");
    public static Shader WindowCity => Get(ref windowCity, "Orbita/WindowCity");
    public static Shader Hologram => Get(ref hologram, "Orbita/Hologram");

    static Shader Get(ref Shader cache, string name)
    {
        if (cache != null) return cache;
        cache = Shader.Find(name);
        if (cache == null) throw new MissingReferenceException("Shader is missing from the build: " + name);
        return cache;
    }
}
