using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SQLTrismegiste
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel.Main _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ViewModel.Main();
            _vm.PopulateFolders();
            DataContext = _vm;
        }

        private void Connection_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(SqlServerName.Text))
            {
                MessageBox.Show(App.Localized["msgPleaseEnterServerName"], App.Localized["msgNoServer"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_vm.Connect())
            {
                btnAnalyzeAll.IsEnabled = true;
                btnCacheExplorer.IsEnabled = true;
            }           
        }

        private void btnAnalyzeAll_Click(object sender, RoutedEventArgs e)
        {
            _vm.RunFullAnalysis();
        }

        private void _this_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _vm.SaveConfig();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("not implemented yet", "soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCheckAll_Click(object sender, RoutedEventArgs e)
        {
            _vm.CheckAllDatabases();
        }

        private void btnValidateHermeticus_Click(object sender, RoutedEventArgs e)
        {
            _vm.ValidateHermeticus();
        }

        private void BtnSaveLastAnalysis_OnClick(object sender, RoutedEventArgs e)
        {
            _vm.SaveZip();
        }

        private void LoadHermeticus(object sender)
        {
            var tb = (sender as TextBlock);
            if (tb == null) return;

            tcDisplay.SelectedIndex = 1;
            if (_vm?.OutputPath == null)
            {
                var html = $"<html><header><meta charset='UTF-8'></header><body><font color='red'>{App.Localized["msgNotProcessedYet"]} !</font></body></html>";
                htmlBrowser.NavigateToString(html);
                return;
            }
            string output = "";

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                output = $"{_vm.OutputPath}{tb.Tag.ToString()}.html";
                htmlBrowser.Navigate(output);
            }
            catch (FileNotFoundException)
            {
                //MessageBox.Show($"File {output} not found !", "file error", MessageBoxButton.OK, MessageBoxImage.Error);
                var html = $"<html><header><meta charset='UTF-8'></header><body><font color='red'>{App.Localized["msgFile"]} {output} {App.Localized["msgNotFound"]} !</font></body></html>";
                htmlBrowser.NavigateToString(html);
            }
            Mouse.OverrideCursor = null;
        }

        private void TextBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoadHermeticus(sender);
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutBox.About
            {
                HyperlinkText = "http://www.babaluga.com/contact",
                AdditionalNotes = "Apache License (see licence.txt)"
            };
            about.Show();
        }

        private void btnCacheExplorer_Click(object sender, RoutedEventArgs e)
        {
            tabCacheExplorer.Visibility = Visibility.Visible;
            tcDisplay.SelectedIndex = 2;
            _vm.CacheExplorer();
        }

        private void btnViewPlan_Click(object sender, RoutedEventArgs e)
        {
            _vm.CE_ViewPlan(((FrameworkElement)sender).DataContext);
        }

        private void btnViewQuery_Click(object sender, RoutedEventArgs e)
        {
            _vm.CE_ViewQuery(((FrameworkElement)sender).DataContext);
        }

        private void btnClearStats_Click(object sender, RoutedEventArgs e)
        {
            _vm.ClearStats();
        }

        private void btnFilterCache_Click(object sender, RoutedEventArgs e)
        {
            _vm.CE_FilterPlanCache(txtFilterCache.Text);
        }

    }
}
