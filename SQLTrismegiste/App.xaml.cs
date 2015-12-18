using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SQLTrismegiste
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        static internal Dictionary<String, String> Localized { get; private set; }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // we make sure that we stop background threads (??)
            Environment.Exit(Environment.ExitCode);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var localized = new ResourceDictionary();
            localized.Source =
                new Uri("/SqlTrismegiste;component/Resources/FR.xaml",
                    UriKind.RelativeOrAbsolute);

            Localized = localized.Keys.Cast<string>().ToDictionary(x => x, x => (string) localized[x] );
        }
    }
}
