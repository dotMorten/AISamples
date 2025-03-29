using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AI.Generative;
using ChatApp.Controls;
using Microsoft.UI.Dispatching;

namespace ChatApp
{
    public sealed partial class MainWindow : Window
    {
        private const string systemPrompt = "You are an AI assistant that helps the user and talks like a pirate.";
        private LanguageModelContext? context; // maintains chat context across multiple calls
        private Microsoft.Windows.AI.ContentModeration.ContentFilterOptions filter = new Microsoft.Windows.AI.ContentModeration.ContentFilterOptions();

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.SetIcon("icon.png");
            ExtendsContentIntoTitleBar = true;
            PromptBox.Loaded += (s,e) => PromptBox.Focus(FocusState.Programmatic);
        }

        private void AskButton_Click(object sender, RoutedEventArgs e) => Ask(PromptBox.Text);

        private void prompt_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Submit query on enter
            if (e.Key == Windows.System.VirtualKey.Enter && AskButton.IsEnabled)
                Ask(PromptBox.Text);
        }

        private async void Ask(string prompt)
        {
            prompt = prompt.Trim();
            if (string.IsNullOrEmpty(prompt))
                return;
            this.PromptBox.Text = "";

            chatView.Messages.Add(new ChatMessage() { Text = prompt, IsUser = true });

            AskButton.IsEnabled = false;
            ProcessingRing.IsActive = true;
            if (!LanguageModel.IsAvailable())
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Loading model...",
                    Content = new ProgressBar() {  Maximum = 1 },
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                var op = LanguageModel.MakeAvailableAsync();
                op.Progress = (s, e) =>
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => ((ProgressBar)dialog.Content).Value = e.Progress);
                };
                await op;
                dialog.Hide();
            }

            using LanguageModel languageModel = await LanguageModel.CreateAsync();
            
            if (context is null)
            {
                context = languageModel.CreateContext(systemPrompt, filter);
            }

            if (languageModel.IsPromptLargerThanContext(context, prompt))
            {
                AskButton.IsEnabled = true;
                ProcessingRing.IsActive = false;
                chatView.Messages.Add(new ChatMessage() { Text = "Context length exceeded. Restart the chat" });
                return;
            }

            var msg = new ChatMessage() { };
            chatView.Messages.Add(msg);
            AsyncOperationProgressHandler<LanguageModelResponse, string>
            progressHandler = (asyncInfo, delta) =>
            {
                _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    ProcessingRing.IsActive = false;
                    msg.Text += delta;
                });
            };
            var generateOperation = languageModel.GenerateResponseWithProgressAsync(new LanguageModelOptions(), prompt, filter, context );
            generateOperation.Progress = progressHandler;

            var result = await generateOperation;
            AskButton.IsEnabled = true;
        }

        private void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            context = null;
            chatView.Messages.Clear();
        }
    }
}
