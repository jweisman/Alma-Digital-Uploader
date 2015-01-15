using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace AlmaDUploader.Utils
{
    public static class Utilities
    {
        /// <summary>
        /// Returns a display friendly expression of file size.
        /// </summary>
        /// <param name="fileSize">Size in bytes</param>
        /// <returns></returns>
        public static string FileSizeDisplay (long fileSize)
        {
            if (fileSize > 1000000000)
                return String.Format("{0} GB", Math.Round(((double)fileSize / 1000000000), 2));
            else if (fileSize > 1000000)
                return String.Format("{0} MB", Math.Round(((double)fileSize / 1000000), 2));
            else if (fileSize > 1000)
                return String.Format("{0} KB", Math.Round(((double)fileSize / 1000), 2));
            else
                return String.Format("{0} bytes", Math.Round((double)fileSize, 2));
        }

        /// <summary>
        /// Returns the relative path of a file based on a starting point
        /// </summary>
        /// <param name="fullPath">Full path of the file</param>
        /// <param name="currentDir">Starting point</param>
        /// <returns></returns>
        public static string RelativePath(string fullPath, string currentDir)
        {
            if (currentDir == "")
                return fullPath.Substring(fullPath.LastIndexOf("\\") +1);

            DirectoryInfo directory = new DirectoryInfo(currentDir);
            FileInfo file = new FileInfo(fullPath);

            string fullDirectory = directory.FullName;
            string fullFile = file.FullName;

            if (!fullFile.StartsWith(fullDirectory))
            {
                return fullPath.Substring(fullPath.LastIndexOf("\\") +1);
            }
            else
            {
                // The +1 is to avoid the directory separator
                return fullFile.Substring(fullDirectory.Length + 1);
            }
        }

        /// <summary>
        /// Returns true if all the properties of the object are null
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <param name="exclude">Optional. Array of properties to exclude from check.</param>
        /// <returns></returns>
        public static bool AllPropertiesNull(object obj, string[] exclude = null)
        {
            bool ret = true;
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (!exclude.Contains(prop.Name) && prop.GetValue(obj, null) != null)
                { ret = false; break; }
            }

            return ret;
        }

        /// <summary>
        /// Returns a byte array of a file's thumbnail
        /// </summary>
        /// <param name="FilePath">Path of the file</param>
        /// <returns></returns>
        public static byte[] GetThumbnail(string FilePath)
        {
            if (String.IsNullOrEmpty(FilePath))
                return null;

            // TODO: Check if ShellFile will work on Windows 8
            System.Drawing.Bitmap bm;
            using (ShellFile shellFile = ShellFile.FromFilePath(FilePath))
            {
                ShellThumbnail thumbnail = shellFile.Thumbnail;

                thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly;

                try
                {
                    bm = thumbnail.MediumBitmap;
                }
                catch // errors can occur with windows api calls so just skip
                {
                    bm = null;
                }
                if (bm == null)
                {
                    thumbnail.FormatOption = ShellThumbnailFormatOption.IconOnly;
                    bm = thumbnail.MediumBitmap;
                    // make icon transparent
                    bm.MakeTransparent(System.Drawing.Color.Black);
                }
            }

            using (MemoryStream stream = new MemoryStream())
            {
                bm.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Serializes an object and transforms it using a specified XSL
        /// </summary>
        /// <param name="data">Object to serialize</param>
        /// <param name="xslFileName">File path of the XSL</param>
        /// <returns></returns>
        public static string TransformXml(object data, string xslFileName)
        {

            XmlSerializer xs = new XmlSerializer(data.GetType());
            string xmlString;
            using (StringWriter swr = new StringWriter())
            {
                xs.Serialize(swr, data);
                xmlString = swr.ToString();
            }

            var xd = new XmlDocument();
            xd.LoadXml(xmlString);

            var xslt = new System.Xml.Xsl.XslCompiledTransform();
            xslt.Load(xslFileName);
            using (var stm = new MemoryStream())
            {
                xslt.Transform(xd, null, stm);
                stm.Position = 0;
                using (var sr = new StreamReader(stm))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }

    #region Converters

    public class BooleanValueInverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is IValueConverter))
            {
                // No second converter is given as parameter:
                // Just invert and return, if boolean input value was provided
                if (value is bool)
                    return !(bool)value;
                else
                    return DependencyProperty.UnsetValue; // Fallback for non-boolean input values
            }
            else
            {
                // Second converter is provided:
                // Retrieve this converter...
                IValueConverter converter = (IValueConverter)parameter;

                if (value is bool)
                {
                    // ...and invert and then convert boolean input value!
                    bool input = !(bool)value;
                    return converter.Convert(input, targetType, null, culture);
                }
                else
                {
                    return DependencyProperty.UnsetValue; // Fallback for non-boolean input values
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(parameter is IValueConverter))
            {
                // No second converter is given as parameter:
                // Just invert and return, if boolean input value was provided
                if (value is bool)
                    return !(bool)value;
                else
                    return DependencyProperty.UnsetValue; // Fallback for non-boolean input values
            }
            else
            {
                // Second converter is provided:
                // Retrieve this converter...
                IValueConverter converter = (IValueConverter)parameter;

                if (value is bool)
                {
                    // ...and invert and then convert boolean input value!
                    bool input = !(bool)value;
                    return converter.Convert(input, targetType, null, culture);
                }
                else
                {
                    return DependencyProperty.UnsetValue; // Fallback for non-boolean input values
                }
            }
        }
    }

    public class FileSizeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Utilities.FileSizeDisplay((long)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

    public class BinaryImageConverter : IValueConverter
    {
        object IValueConverter.Convert(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value != null && value is byte[])
            {
                byte[] bytes = value as byte[];

                BitmapImage image = new BitmapImage();

                MemoryStream stream = new MemoryStream(bytes);
                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();

                return image;
            }

            return null;
        }

        object IValueConverter.ConvertBack(object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }


    #endregion

    #region Other utility classes

    public static class Extensions
    {
        public static string FormatWith(this string s, params object[] args)
        {
            return string.Format(s, args);
        }
    }

    public class ObservableHashSet<T> : ObservableCollection<T>
    {
    
        protected override void InsertItem(int index, T item)
        {
            if (Contains(item) || (this.SingleOrDefault<T>(i => i.ToString() == item.ToString()) != null))
            {
                throw new ItemExistsException(item);
            }
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            int i = IndexOf(item);
            if (i >= 0 && i != index)
            {
                throw new ItemExistsException(item);
            }
            base.SetItem(index, item);
        }
    }

    public class ItemExistsException : Exception
    {
        public ItemExistsException() : base()
        {
        }

        public ItemExistsException(String message)
            : base(message)
        {
        }

        public ItemExistsException(object item)
            : base(String.Format("The item {0} already exists", item.ToString()))
        { }

        public ItemExistsException(String message, Exception innerException)
            : base(message, innerException)
        {
        }

    } 

    #endregion

}
