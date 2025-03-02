using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Bridge.Command.model;

namespace Bridge.Command
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var versionNumber = commandData.Application.Application.VersionNumber;
            var path = Path.Combine(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                $"Autodesk\\Revit\\Addins\\{versionNumber}\\Bridge\\resources"));
            path = Path.Combine(path, TransferModel.Data.ToString() + ".rfa");
            if (!File.Exists(path))
            {
                Log(path);
                TaskDialog.Show("警告", $"{TransferModel.Data.ToString()}不存在,请正确配置插件");
                return Result.Failed;
            }

            // 获取 Revit 应用程序和文档
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            string familyPath = path;
            var fileName = TransferModel.Data.ToString();

            if (!File.Exists(familyPath))
            {
                TaskDialog.Show("错误", "族文件不存在：" + familyPath);
                return Result.Failed;
            }

            using (TransactionGroup tg = new TransactionGroup(doc, "Load and Place Family"))
            {
                tg.Start();

                try
                {
                    // 检查族是否已经加载
                    // Family existingFamily = new FilteredElementCollector(doc)
                    //     .OfClass(typeof(Family))
                    //     .Cast<Family>()
                    //     .FirstOrDefault(f => f.Name ==fileName );  // 族名称应与 rfa 内部名称匹配

                    // 获取族符号
                    var familySymbols = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .Cast<FamilySymbol>()
                        .Where(fs => fs.Family.Name == fileName);

                    FamilySymbol familySymbol = null;
                    if (familySymbols?.Count() > 0)
                    {
                        familySymbol = familySymbols.FirstOrDefault();
                    }

                    if (familySymbol == null)
                    {
                        // 加载族
                        using (Transaction tx = new Transaction(doc, "Load Family"))
                        {
                            tx.Start();
                            if (!doc.LoadFamily(familyPath, out var family))
                            {
                                TaskDialog.Show("错误", "无法加载族文件：" + familyPath);
                                tx.RollBack();
                                return Result.Failed;
                            }

                            tx.Commit();
                        }
                    }

                    // 获取族符号
                    familySymbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .Cast<FamilySymbol>()
                        .Where(fs => fs.Family.Name == fileName)
                        .FirstOrDefault();

                    if (familySymbol == null)
                    {
                        TaskDialog.Show("错误", "未找到族符号");
                        return Result.Failed;
                    }

                    using (Transaction tx = new Transaction(doc, "Activate Family Symbol"))
                    {
                        tx.Start();
                        if (!familySymbol.IsActive)
                        {
                            familySymbol.Activate();
                            doc.Regenerate();
                        }

                        tx.Commit();
                    }

                    // 允许用户放置族
                    try
                    {
                    uiDoc.PromptForFamilyInstancePlacement(familySymbol);
                    }catch (Exception e)
                    {
                        Log(e.Message);
                    }

                    tg.Assimilate();
                }
                catch (OperationCanceledException)
                {
                    tg.Assimilate();
                }
                catch (Exception ex)
                {
                    tg.RollBack();
                    Log(ex.GetType().Name);
                    message = "错误：" + ex.Message;
                    return Result.Failed;
                }
            }

            return Result.Succeeded;
        }

        private string _logPath;
        private readonly object _lock = new object();

        private void Log(string message)
        {
            InitLogger();
            lock (_lock)
            {
                var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.AppendAllText(_logPath, $"[{time}] {message}\n");
            }
        }

        private void InitLogger()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Bridge\\logs");
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
    }
}