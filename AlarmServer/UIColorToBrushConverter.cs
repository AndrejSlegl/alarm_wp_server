using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace AlarmServer
{
    public class UIColorToBrushConverter : IValueConverter
    {
        public Collection<Brush> Brushes { get; }

        public UIColorToBrushConverter()
        {
            Brushes = new Collection<Brush>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            UIColorValue type = (UIColorValue)value;

            foreach (var brush in Brushes)
            {
                if (Ext.GetUIColor(brush) == type)
                    return brush;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
