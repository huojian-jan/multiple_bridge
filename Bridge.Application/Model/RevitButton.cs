using System.Collections.Generic;
using Autodesk.Revit.UI;

namespace Bridge.Command.model;
public class RevitButton:RevitSeperator
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Image { get; set; }
    public string LargeImage { get; set; }
    public string ToolTips{get; set; }
    public string LongDescription { get; set; }
}

public class RevitSeperator{}

public class RevitStackButton
{
    public string Name { get; set; }
    public string Text { get; set; }
    public List<RevitButton> Buttons { get; set; }
}

public class RevitPulldownButton
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Image { get; set; }
    public string LargeImage { get; set; }
    public string ToolTips { get; set; }
    public string LongDescription { get; set; }
    public List<RevitButton> Buttons { get; set; }
}

public class RevitPanel
{
    public List<RevitButton> Buttons { get; set; }
    public List<RevitStackButton> StackButtons { get; set; }
    public List<RevitPulldownButton> PulldownButtons { get; set; }

    public string Name { get; set; }
}

public class RevitTab
{
    public string Name { get; set; }
    public List<RevitPanel> Panels { get; set; }
}
