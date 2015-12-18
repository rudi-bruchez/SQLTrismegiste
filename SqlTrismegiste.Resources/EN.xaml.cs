using System.ComponentModel.Composition;
using System.Windows;

namespace SQLTrismegiste.Resources
{
    /// <summary>
    /// Logique d'interaction pour FR.xaml
    /// </summary>
    [ExportMetadata("Culture", "en-US")]
    [Export(typeof(ResourceDictionary))]
    public partial class EN : ResourceDictionary
    {
        public EN()
        {
            InitializeComponent();
        }
    }
}
