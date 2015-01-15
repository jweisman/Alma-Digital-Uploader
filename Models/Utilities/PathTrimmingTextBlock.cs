using System;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AlmaDUploader.Utils
{
    // From http://www.codeproject.com/Tips/467054/WPF-PathTrimmingTextBlock
    public class PathTrimmingTextBlock : TextBlock, INotifyPropertyChanged
    {

        FrameworkElement _container;


        public PathTrimmingTextBlock()
        {
            this.Loaded += new RoutedEventHandler(PathTrimmingTextBlock_Loaded);
        }

        void PathTrimmingTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent == null) throw new InvalidOperationException("PathTrimmingTextBlock must have a container such as a Grid.");

            _container = (FrameworkElement)this.Parent;
            _container.SizeChanged += new SizeChangedEventHandler(container_SizeChanged);

            Text = GetTrimmedPath(_container.ActualWidth);
        }

        void container_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Text = GetTrimmedPath(_container.ActualWidth);
        }

        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Path.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register("Path", typeof(string), typeof(PathTrimmingTextBlock), new UIPropertyMetadata(""));

        string GetTrimmedPath(double width)
        {
            string filename = System.IO.Path.GetFileName(Path);
            string directory = System.IO.Path.GetDirectoryName(Path);
            FormattedText formatted;
            bool widthOK = false;
            bool changedWidth = false;

            if (filename.IndexOf("\\") < 0)
                return filename;

            do
            {
                formatted = new FormattedText(
                    "{0}...\\{1}".FormatWith(directory, filename),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    FontFamily.GetTypefaces().First(),
                    FontSize,
                    Foreground
                    );

                widthOK = formatted.Width < width;

                if (!widthOK)
                {
                    changedWidth = true;
                    directory = directory.Substring(0, directory.Length - 1);

                    if (directory.Length == 0) return "...\\" + filename;
                }

            } while (!widthOK);

            if (!changedWidth)
            {
                return Path;
            }
            return "{0}...{1}".FormatWith(directory, filename);
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
