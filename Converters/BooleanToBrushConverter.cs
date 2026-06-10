using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace XrayUI.Converters
{
    public partial class BooleanToBrushConverter : IValueConverter
    {
        public Brush TrueBrush { get; set; } = new SolidColorBrush();

        public Brush FalseBrush { get; set; } = new SolidColorBrush();

        public object Convert(object value, Type targetType, object parameter, string language)
            => value is true ? TrueBrush : FalseBrush;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
