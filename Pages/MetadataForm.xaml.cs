using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AlmaDUploader
{
    /// <summary>
    /// Interaction logic for MetadataForm.xaml
    /// </summary>
    public partial class MetadataForm : UserControl
    {
        public MetadataForm()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // select first control on the form
            Keyboard.Focus(this.txtTitle);
        }
    }
}
