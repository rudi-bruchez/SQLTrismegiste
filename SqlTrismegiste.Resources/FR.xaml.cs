using System.ComponentModel.Composition;
using System.Windows;

namespace SQLTrismegiste.Resources
{
    /// <summary>
    /// Logique d'interaction pour FR.xaml
    /// </summary>
    [ExportMetadata("Culture", "fr-FR")]
    [Export(typeof(ResourceDictionary))]
    public partial class FR : ResourceDictionary
    {
        public FR()
        {
            InitializeComponent();
        }
    }
}
