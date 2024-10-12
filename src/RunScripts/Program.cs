using static RunScripts.Args;
using RunScripts;

var option = FromStringArray(args);

_ = option switch
{
    Options.Run => Functions.RunAll(GetConnectionOverrides(args)),
    Options.Watch => Functions.Watch(),
    Options.Init => Config.Set(),
    Options.Config => Config.Set(),
    Options.ClearCache => Functions.ClearDBCache(),
    Options.Help => UI.PrintHelp(),
    _ => UI.PrintHelp(),
};
