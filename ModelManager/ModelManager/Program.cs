namespace ModelManager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var mainForm = new Form1();
            mainForm.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            Application.Run(mainForm);
        }
    }
}