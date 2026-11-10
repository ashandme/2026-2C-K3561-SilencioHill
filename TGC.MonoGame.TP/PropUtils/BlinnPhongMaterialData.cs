namespace TGC.MonoGame.TP.PropUtils;

// Optional per-prop Blinn-Phong coefficients ("Material" in props.json).
// Any value left out falls back to the active level's BlinnPhongSettings.
public class BlinnPhongMaterialData
{
    public float? KAmbient { get; set; }
    public float? KDiffuse { get; set; }
    public float? KSpecular { get; set; }
    public float? Shininess { get; set; }
}
