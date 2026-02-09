public static class HwingMenuPaths
{
    public const string Root = "Tools/Hwing";

    public const string Utilities = Root + "/Utilities";
    public const string Capture = Root + "/Capture";

    public const string Watercolor = Root + "/Watercolor";
    public const string WatercolorMaterials = Watercolor + "/Materials";
    public const string WatercolorRamps = Watercolor + "/Ramps";
    public const string WatercolorScene = Watercolor + "/Scene";
    public const string WatercolorDebug = Watercolor + "/Debug";

    public const string RockTools = Root + "/Rock Tools";
    public const string RockToolsGenerator = RockTools + "/Generator";
    public const string RockToolsExport = RockTools + "/Export";
    public const string Legacy = Root + "/Legacy";

    public const int UtilitiesPriority = 10;
    public const int CapturePriority = 30;
    public const int RockToolsPriority = 100;
    public const int WatercolorScenePriority = 200;
    public const int WatercolorMaterialsPriority = 220;
    public const int WatercolorRampsPriority = 300;
    public const int WatercolorDebugPriority = 400;
    public const int LegacyPriority = 500;
}
