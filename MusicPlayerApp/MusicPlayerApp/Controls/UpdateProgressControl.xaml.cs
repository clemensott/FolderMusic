using MusicPlayer.UpdateLibrary;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Die Elementvorlage "Benutzersteuerelement" ist unter http://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace FolderMusic.Controls
{
    public sealed partial class UpdateProgressControl : UserControl
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source),
            typeof(BaseUpdateProgress), typeof(UpdateProgressControl), new PropertyMetadata(default(BaseUpdateProgress)));

        public BaseUpdateProgress Source
        {
            get { return (BaseUpdateProgress)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public UpdateProgressControl()
        {
            this.InitializeComponent();
        }

        private object IsIndeterminateCon_ConvertEvent(object value, Type targetType, object parameter, string language)
        {
            return (int)value == 0;
        }
    }
}
