using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

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

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(SqlServerName.Text))
            {
                MessageBox.Show("Please enter a server name", "No Server", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _vm.Connect();           
        }

        private void btnAnalyzeAll_Click(object sender, RoutedEventArgs e)
        {
            _vm.RunFullAnalysis();

            // not possible : backgroundworker -> dependency injection of treeview .... ??
            // http://stackoverflow.com/questions/834081/wpf-treeview-where-is-the-expandall-method
            //foreach (object item in tvCorpus.Items)
            //{
            //    TreeViewItem treeItem = tvCorpus.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            //    if (treeItem != null)
            //        treeItem.IsExpanded = true;
            //}
        }

        //private void tvCorpus_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    var tvi = (e.NewValue as TreeViewItem);
        //    if (tvi == null || tvi.HasItems) return;

        //    tcDisplay.SelectedIndex = 1;
        //    var output = $"{_vm.OutputPath}{tvi.Name}.html";
        //    try
        //    {
        //        _htmlPanel.Text = File.ReadAllText(output);
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        //MessageBox.Show($"File {output} not found !", "file error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        var html = $"<html><body><font color='red'>File {output} not found !</font></body></html>";
        //        _htmlPanel.Text = html;
        //    }
        //}

        private void btnSendResultsByEmail_Click(object sender, RoutedEventArgs e)
        {
            _vm.SendResultsByEmail();
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
                var html = $"<html><body><font color='red'>not processed yet !</font></body></html>";
                _htmlPanel.Text = html;
                return;
            }
            string output = "";

            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                output = $"{_vm.OutputPath}{tb.Tag.ToString()}.html";
                _htmlPanel.Text = File.ReadAllText(output);
            }
            catch (FileNotFoundException)
            {
                //MessageBox.Show($"File {output} not found !", "file error", MessageBoxButton.OK, MessageBoxImage.Error);
                var html = $"<html><body><font color='red'>File {output} not found !</font></body></html>";
                _htmlPanel.Text = html;
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
    }
}
