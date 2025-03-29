using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace ChatApp.Controls
{
    public sealed class ChatBubble : Control
    {
        public ChatBubble()
        {
            DefaultStyleKey = typeof(ChatBubble);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateBubble();
        }

        public ChatMessage Message
        {
            get { return (ChatMessage)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(ChatMessage), typeof(ChatBubble), new PropertyMetadata(null, 
                (d, e) => ((ChatBubble)d).OnMessagePropertyChanged(e.OldValue as ChatMessage, e.NewValue as ChatMessage)));

        private void OnMessagePropertyChanged(ChatMessage? oldChatMessage, ChatMessage? newChatMessage)
        {
            UpdateBubble();
        }
        const double cornerRadius = 10;
        private void UpdateBubble()
        {
            var elm = GetTemplateChild("BubbleBorder") as FrameworkElement;
            if (elm != null)
            {
                Grid.SetColumn(elm, Message?.IsUser == true ? 1 : 0);
                elm.HorizontalAlignment = Message?.IsUser == true ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                if(elm is Border b)
                {
                    b.CornerRadius = new CornerRadius(cornerRadius, cornerRadius, Message?.IsUser == true ? 0 : cornerRadius, Message?.IsUser != true ? 0 : cornerRadius);
                    b.Background = Message?.IsUser == true ? UserBackground : Background;
                }
                DrawPath();
            }
            var content = GetTemplateChild("ChatContent") as ContentPresenter;
            if (content != null)
            {
                content.Foreground = Message?.IsUser == true ? UserForeground : Foreground;
            }
        }

        private void DrawPath()
        {

        }

        public double MaxBubbleWidth
        {
            get { return (double)GetValue(MaxBubbleWidthProperty); }
            set { SetValue(MaxBubbleWidthProperty, value); }
        }

        public static readonly DependencyProperty MaxBubbleWidthProperty =
            DependencyProperty.Register("MaxBubbleWidth", typeof(double), typeof(ChatBubble), new PropertyMetadata(double.PositiveInfinity));



        public Brush UserBackground
        {
            get { return (Brush)GetValue(UserBackgroundProperty); }
            set { SetValue(UserBackgroundProperty, value); }
        }

        public static readonly DependencyProperty UserBackgroundProperty =
            DependencyProperty.Register("UserBackground", typeof(Brush), typeof(ChatBubble), new PropertyMetadata(null));

        public Brush UserForeground
        {
            get { return (Brush)GetValue(UserForegroundProperty); }
            set { SetValue(UserForegroundProperty, value); }
        }

        public static readonly DependencyProperty UserForegroundProperty =
            DependencyProperty.Register("UserForeground", typeof(Brush), typeof(ChatBubble), new PropertyMetadata(null));
    }
}
