using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace AlarmServer
{

    public class EventTypeToBrushConverter : IValueConverter
    {
        public Collection<Brush> Brushes { get; }

        public EventTypeToBrushConverter()
        {
            Brushes = new Collection<Brush>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            EventType type = (EventType)value;

            foreach (var brush in Brushes)
            {
                if (Ext.GetEventType(brush) == type)
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
