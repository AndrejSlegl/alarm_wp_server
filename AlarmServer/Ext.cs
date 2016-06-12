using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace AlarmServer
{
    public class Ext : DependencyObject
    {
        public static readonly DependencyProperty EventTypeProperty = DependencyProperty.RegisterAttached("EventType", typeof(EventType), typeof(Ext),
            new PropertyMetadata(EventType.Message));

        public static void SetEventType(DependencyObject obj, EventType value)
        {
            obj.SetValue(EventTypeProperty, value);
        }

        public static EventType GetEventType(DependencyObject obj)
        {
            return (EventType)obj.GetValue(EventTypeProperty);
        }

        public static readonly DependencyProperty UIColorProperty = DependencyProperty.RegisterAttached("UIColor", typeof(UIColorValue), typeof(Ext),
            new PropertyMetadata(UIColorValue.None));

        public static void SetUIColor(DependencyObject obj, UIColorValue value)
        {
            obj.SetValue(UIColorProperty, value);
        }

        public static UIColorValue GetUIColor(DependencyObject obj)
        {
            return (UIColorValue)obj.GetValue(UIColorProperty);
        }
    }
}
