using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace ChatApp.Controls
{
    public sealed partial class ChatView : Control
    {
        public ChatView()
        {
            DefaultStyleKey = typeof(ChatView);
            Messages = new ObservableCollection<ChatMessage>();
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(ChatView), new PropertyMetadata(null));

        protected override void OnApplyTemplate()
        {
            var presenter = GetTemplateChild("ChatList") as ItemsControl;
            var scrollView = GetTemplateChild("ScrollView") as ScrollView;
            if (scrollView != null)
                scrollView.ExtentChanged += ScrollView_ExtentChanged;
            base.OnApplyTemplate();
        }

        private void ScrollView_ExtentChanged(ScrollView sender, object args)
        {
            sender.ScrollTo(0, sender.ScrollableHeight);
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get { return (ObservableCollection<ChatMessage>)GetValue(MessagesProperty); }
            set { SetValue(MessagesProperty, value); }
        }

        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register(nameof(Messages), typeof(ObservableCollection<ChatMessage>), typeof(ChatView), new PropertyMetadata(null));
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public partial class ChatMessage : INotifyPropertyChanged
    {
        private string _text = string.Empty;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        public string? Name { get; set; }

        public bool IsUser { get; set; }

        public DateTimeOffset Timestamp {get; set;}

        private bool _isTyping;

        public bool IsTyping
        {
            get { return _isTyping; }
            set { _isTyping = value; OnPropertyChanged(nameof(IsTyping)); }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
