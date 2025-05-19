using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AI;
using ChatApp.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AI.Text;
using Microsoft.Windows.AI.ContentSafety;

namespace ChatApp
{
    public sealed partial class MainWindow : Window
    {
        private const string systemPrompt = "You are an AI assistant named Black Beard that helps the user and talks like a pirate.";
        private LanguageModelContext? context; // maintains chat context across multiple calls
        private ContentFilterOptions filter = new ContentFilterOptions();

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.SetIcon("icon.png");
            ExtendsContentIntoTitleBar = true;
            PromptBox.Loaded += (s,e) => PromptBox.Focus(FocusState.Programmatic); // Start app with focus on the prompt box
            chatView.Messages.Add(new ChatMessage() { Text = "Ahoy matey! What treasure ye seek, or perhaps assistance I can provide on yer grand voyage across the digital seas?" });
        }

        private void AskButton_Click(object sender, RoutedEventArgs e) => Ask(PromptBox.Text);

        private void PromptBox_KeyDown(object sender, KeyRoutedEventArgs e)
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

            // Check if the system supports using the language model.
            var readyState = LanguageModel.GetReadyState();
            if (readyState == Microsoft.Windows.AI.AIFeatureReadyState.NotSupportedOnCurrentSystem)
            {
                chatView.Messages.Add(new ChatMessage() { Text = "Language model not supported on this system. A Copilot PC is required." });
                return;
            }

            if (readyState == Microsoft.Windows.AI.AIFeatureReadyState.DisabledByUser)
            {
                chatView.Messages.Add(new ChatMessage() { Text = "Language model was disabled by the user." });
                return;
            }

            ProcessingRing.IsActive = true;
            if (readyState == Microsoft.Windows.AI.AIFeatureReadyState.NotReady)
            {
                // Model is not ready, show a loading dialog while we provision it. This is only needed the first time.
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Loading model...",
                    Content = new ProgressBar() { Maximum = 1 },
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                IAsyncOperationWithProgress<AIFeatureReadyResult, double> readyOperation = LanguageModel.EnsureReadyAsync();
                readyOperation.Progress = (s, e) => // Update the progress bar as the model loads
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => ((ProgressBar)dialog.Content).Value = e);
                var readyResult = await readyOperation;
                dialog.Hide();
                if (readyResult.Error != null)
                {
                    var dlg = new ContentDialog() { XamlRoot = this.Content.XamlRoot, Content = readyResult.ErrorDisplayText };
                    _ = await dlg.ShowAsync();
                    return;
                }

            }

            using LanguageModel languageModel = await LanguageModel.CreateAsync();
            
            if (context is null) // First time using the model of after a reset
            {
                context = languageModel.CreateContext(systemPrompt, filter);
            }
            
            if (languageModel.GetUsablePromptLength(context, prompt) < (ulong)prompt.Length)
            {
                AskButton.IsEnabled = true;
                ProcessingRing.IsActive = false;
                chatView.Messages.Add(new ChatMessage() { Text = "Context length exceeded. Restart the chat" });
                return;
            }

            var msg = new ChatMessage() { };
            chatView.Messages.Add(msg);
            AsyncOperationProgressHandler<LanguageModelResponseResult, string> progressHandler = (asyncInfo, delta) =>
            {
                _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    ProcessingRing.IsActive = false;
                    msg.Text += delta;
                });
            };
            
            var generateOperation = languageModel.GenerateResponseAsync(context, prompt, new LanguageModelOptions() { ContentFilterOptions = filter });
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
