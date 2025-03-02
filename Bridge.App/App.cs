using System.Reflection;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Bridge.Command;
using Bridge.Command.model;
using adwin = Autodesk.Windows;

[Transaction(TransactionMode.Manual)]
public class App : IExternalApplication
{
    private const string _tabName = "木拱廊桥";
    private string _logPath;
    private readonly object _lock = new object();

    public Result OnStartup(UIControlledApplication application)
    {
        InitLogger();
        CreateUIView(application);
        RegisterClickEvent();

        return Result.Succeeded;
    }

    private void RegisterClickEvent()
    {
        var ribbons = adwin.ComponentManager.Ribbon;
        foreach (var tab in ribbons.Tabs)
        {
            if (tab.Panels == null) continue;
            foreach (var panel in tab.Panels)
            {
                if (panel.Source.Items == null) continue;
                foreach (var item in panel.Source.Items)
                {
                    if (item == null) continue;
                }
            }
        }

        try
        {
            adwin.ComponentManager.UIElementActivated += new
                EventHandler<adwin.UIElementActivatedEventArgs>(ComponentManager_UIElementActivated);
        }
        catch (Exception e)
        {
            Log(e.Message);
        }
    }

    private void ComponentManager_UIElementActivated(object sender, adwin.UIElementActivatedEventArgs e)
    {
        try
        {
            if (e.Item == null) return;
            if (string.IsNullOrEmpty(e.Item.Text)) return;
            TransferModel.Data = e.Item.Text;
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreateUIView(UIControlledApplication application)
    {
        var versionNumber = application.ControlledApplication.VersionNumber;
        var path = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), $"Autodesk\\Revit\\Addins\\{versionNumber}\\Bridge\\UIView.xml"));
        if (!File.Exists(path))
        {
            Log("UIView.Xml不存在，跳过创建");
            return;
        }

        var tabs = ReadUIDataFromXml(path);
        if (tabs.Count == 0)
        {
            Log("读取本地UIView失败，请检查XML格式是否正确");
            return;
        }

        foreach (var tab in tabs)
        {
            try
            {
                CreateTab(application, tab);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }
    }

    private List<RevitTab> ReadUIDataFromXml(string filePath)
    {
        var result = new List<RevitTab>();
        try
        {
            XmlSerializer ser = new XmlSerializer(result.GetType());
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                result = (List<RevitTab>)ser.Deserialize(reader);
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }

        return result;
    }

    private void CreateTab(UIControlledApplication application, RevitTab tabData)
    {
        try
        {
            application.CreateRibbonTab(tabData.Name);
            tabData.Panels.ForEach(panelData => { CreatePanel(application, tabData.Name, panelData); });
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreatePanel(UIControlledApplication application, string tabName, RevitPanel panelData)
    {
        try
        {
            var panel = application.CreateRibbonPanel(tabName, panelData.Name);
            CreateButtons(panel, panelData.Buttons);
            CreateStackPanels(panel, panelData.StackButtons);
            CreatePulldownButtons(panel, panelData.PulldownButtons);
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreateStackPanels(RibbonPanel panel, List<RevitStackButton> stackPanelDatas)
    {
        try
        {
            foreach (var stackPanelData in stackPanelDatas)
            {
                var buttons = new List<RibbonItemData>();
                stackPanelData.Buttons.ForEach(btn =>
                {
                    var pushButtonData = new PushButtonData(btn.Name, btn.Text,
                        Assembly.GetExecutingAssembly().Location, typeof(Command).FullName);
                    pushButtonData.Name = btn.Name;
                    pushButtonData.Text = btn.Text;
                    pushButtonData.ToolTip = btn.ToolTips;
                    pushButtonData.LongDescription = btn.LongDescription;
                    if (!string.IsNullOrEmpty(btn.Image))
                    {
                        pushButtonData.Image = new BitmapImage(new Uri(btn.Image));
                    }

                    if (!string.IsNullOrEmpty(btn.LargeImage))
                    {
                        pushButtonData.LargeImage = new BitmapImage(new Uri(btn.LargeImage));
                    }

                    if (!string.IsNullOrEmpty(btn.ToolTipImage))
                    {
                        pushButtonData.ToolTipImage = new BitmapImage(new Uri(btn.ToolTipImage));
                    }
                    buttons.Add(pushButtonData);
                });
                if (buttons.Count == 2)
                {
                    panel.AddStackedItems(buttons[0], buttons[1]);
                }
                else if (buttons.Count == 3)
                {
                    panel.AddStackedItems(buttons[0], buttons[1], buttons[2]);
                }
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreatePulldownButtons(RibbonPanel panel, List<RevitPulldownButton> pulldownButtonDatas)
    {
        try
        {
            foreach (var pulldownButtonData in pulldownButtonDatas)
            {
                var pulldownButton =
                    panel.AddItem(new PulldownButtonData(pulldownButtonData.Name, pulldownButtonData.Text)) as
                        PulldownButton;
                pulldownButtonData.Buttons.ForEach(btn =>
                {
                    if (btn is RevitButton)
                    {
                        var pushButtonData = new PushButtonData(btn.Name, btn.Text,
                            Assembly.GetExecutingAssembly().Location, typeof(Command).FullName);
                        pushButtonData.Name = btn.Name;
                        pushButtonData.Text = btn.Text;
                        pushButtonData.ToolTip = btn.ToolTips;
                        pushButtonData.LongDescription = btn.LongDescription;
                        if (!string.IsNullOrEmpty(btn.Image))
                        {
                            pushButtonData.Image = new BitmapImage(new Uri(btn.Image));
                        }

                        if (!string.IsNullOrEmpty(btn.LargeImage))
                        {
                            pushButtonData.LargeImage = new BitmapImage(new Uri(btn.LargeImage));
                        }

                        if (!string.IsNullOrEmpty(btn.ToolTipImage))
                        {
                            pushButtonData.ToolTipImage = new BitmapImage(new Uri(btn.ToolTipImage));
                        }
                        pulldownButton.AddPushButton(pushButtonData);
                    }
                    else if (btn is RevitSeperator)
                    {
                        pulldownButton.AddSeparator();
                    }
                });
                pulldownButton.ItemText = pulldownButtonData.Text;
                pulldownButton.ToolTip = pulldownButtonData.ToolTips;
                pulldownButton.LongDescription = pulldownButtonData.LongDescription;
                if (!string.IsNullOrEmpty(pulldownButtonData.Image))
                {
                    pulldownButton.Image = new BitmapImage(new Uri(pulldownButtonData.Image));
                }

                if (!string.IsNullOrEmpty(pulldownButtonData.LargeImage))
                {
                    pulldownButton.LargeImage = new BitmapImage(new Uri(pulldownButtonData.LargeImage));
                }
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CreateButtons(RibbonPanel panel, List<RevitButton> buttons)
    {
        try
        {
            buttons.ForEach(btn =>
            {
                if (btn is RevitButton)
                {
                    var pushButtonData = new PushButtonData(btn.Name, btn.Text,
                        Assembly.GetExecutingAssembly().Location, typeof(Command).FullName);
                    pushButtonData.Name = btn.Name;
                    pushButtonData.Text = btn.Text;
                    pushButtonData.ToolTip = btn.ToolTips;
                    pushButtonData.LongDescription = btn.LongDescription;
                    
                    if (!string.IsNullOrEmpty(btn.Image))
                    {
                        pushButtonData.Image = new BitmapImage(new Uri(btn.Image));
                    }

                    if (!string.IsNullOrEmpty(btn.LargeImage))
                    {
                        pushButtonData.LargeImage = new BitmapImage(new Uri(btn.LargeImage));
                    }

                    if (!string.IsNullOrEmpty(btn.ToolTipImage))
                    {
                        pushButtonData.ToolTipImage = new BitmapImage(new Uri(btn.ToolTipImage));
                    }
                    panel.AddItem(pushButtonData);
                    panel.AddSeparator();
                }
                else if (btn is RevitSeperator)
                {
                    panel.AddSeparator();
                }
            });
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }

    private void InitLogger()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Bridge\\logs");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
        _logPath = Path.Combine(path, fileName);
        if (!File.Exists(_logPath))
        {
            File.Create(_logPath);
        }
    }

    private void Log(string message)
    {
        lock (_lock)
        {
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(_logPath, $"[{time}] {message}\n");
        }
    }
}