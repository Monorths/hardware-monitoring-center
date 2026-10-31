using System;
using System.Windows.Forms;

namespace Smoke
{
    internal static class SmokeProgram
    {
        [STAThread]
        private static int Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (CleanTempCompare.MainForm form = new CleanTempCompare.MainForm())
                {
                    form.CreateControl();
                    if (form.IsDisposed)
                        return 2;
                }
                Console.WriteLine("FORM_CONSTRUCTION_OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}
