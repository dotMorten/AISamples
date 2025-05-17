#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextGeneration;
using System.Text.Json.Serialization;
using Connectors.Phi4;

namespace Phi4ConsoleApp.Connectors.Phi4
{
    public class Phi4ChatCompletionService : IChatCompletionService, ITextGenerationService, IFunctionInvocationFilter, IAutoFunctionInvocationFilter //, IImageToTextService

    {
        private static Model? model;
        private static MultiModalProcessor? processor;

        public Phi4ChatCompletionService(string modelPath, string provider = "cuda")
        {
            using Config config = new Config(modelPath);
            config.AppendProvider(provider);
            if (provider == "cuda")
                config.SetProviderOption("cuda", "enable_cuda_graph", "0");
            model = new Model(config);
            processor = new MultiModalProcessor(model);
        }
        
            private async IAsyncEnumerable<string> Answer(ChatHistory history, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                //history.Add(new ChatMessageContent(AuthorRole.Tool,
                //[
                //    new FunctionResultContent(new FunctionCallContent("GetCurrentWeather", "MyPlugin", "1", new KernelArguments() { ["location"] = "Boston, MA" }), "rainy"),
                //]));
                if (processor is not null)
                {
                    await Task.Delay(1).ConfigureAwait(false);
                    using var tokenizerStream = processor!.CreateStream();

                    StringBuilder prompt = new StringBuilder();
                    foreach (var item in history)
                    {
                        prompt.Append($"<|{item.Role}|>{item.Content}");

                        if (item.Role == AuthorRole.System)
                        {
                            if (kernel is not null)
                            {
                                StringBuilder sb = new StringBuilder();
                                sb.Append("<|tool|>[");
                                foreach (var plugin in kernel.Plugins)
                                {
                                    foreach (var f in plugin.GetFunctionsMetadata())
                                    {
                                        var phi4fnc = Phi4Function.FromKernelFunction(f).ToJson();
                                        sb.Append(phi4fnc);
                                        sb.Append(",");
                                    }
                                }
                                sb.Remove(sb.Length - 1, 1);
                                sb.Append("]<|/tool|>");
                                prompt.Append(sb.ToString());

                            }
                            //prompt.Append("<|tool|>[{\"name\": \"get_weather_updates\", \"description\": \"Fetches weather updates for a given city using the RapidAPI Weather API.\", \"parameters\": {\"city\": {\"description\": \"The name of the city for which to retrieve weather information.\", \"type\": \"str\", \"default\": \"London\"}}}]<|/tool|>");
                        }
                        prompt.Append("<|end|>");
                    }

                    //prompt.Append($"<|tool|>The current time is {DateTime.Now.ToString("R")}<|end>");
                    prompt.Append("<|assistant|>");

                    var fullPrompt = prompt.ToString();
                    Debug.WriteLine(fullPrompt);
                    var inputTensors = processor.ProcessImages(fullPrompt, null);
                    using GeneratorParams generatorParams = new GeneratorParams(model);
                    generatorParams.SetSearchOption("max_length", 3072);
                    generatorParams.SetInputs(inputTensors);

                    // generate response
                    using var generator = new Generator(model, generatorParams);
                    bool isToolCall = false;
                    string toolcall = "";
                    while (!generator.IsDone())
                    {
                        // generator.ComputeLogits();
                        generator.GenerateNextToken();
                        var seq = generator.GetSequence(0)[^1];
                        var str = tokenizerStream.Decode(seq);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (str == "<|tool_call|>")
                        {
                            isToolCall = true;
                            continue;
                        }
                        else if (str == "<|/tool_call|>")
                        {
                            Debug.WriteLine("");
                            isToolCall = false;
                            history.Add(new ChatMessageContent(AuthorRole.Tool, toolcall));
                            var call = ParseToolcall(toolcall);
                            var function = kernel.Plugins.GetFunction(null, call.Item1);
                            var result = await function.InvokeAsync(kernel, new KernelArguments(call.Item2));
                            history.Add(new ChatMessageContent(AuthorRole.Tool, result.ToString()));
                            toolcall = "";
                            // TODO: We should probably not do that until the end
                            await foreach (var respone in Answer(history, executionSettings, kernel, cancellationToken))
                            {
                                yield return respone;
                            }
                        continue;
                        }
                        if (isToolCall)
                        {
                        Debug.Write(str);
                        toolcall += str;
                        }
                        else
                        {
                            yield return str;
                        }
    ;
                        await Task.Yield();
                        if (cancellationToken.IsCancellationRequested)
                            break;
                    }
                }
            }
        private Tuple<string,Dictionary<string, object?>>? ParseToolcall(string strJson)
        {
            Debug.WriteLine("Received tool call: " + strJson);
            var json = System.Text.Json.JsonDocument.Parse(strJson);
            foreach (var call in json.RootElement.EnumerateArray())
            {
                var name = call.GetProperty("name").GetString();
                var arguments = new Dictionary<string, object?>();
                if (call.TryGetProperty("parameters", out var parameters))
                {
                    foreach(var p in parameters.EnumerateObject())
                    {
                        var pname = p.Name;
                        var value = p.Value.ToString();
                        arguments.Add(pname, value);
                    }
                }
                return new Tuple<string, Dictionary<string, object?>>(name!, arguments);
            }
            return null;
        }
            // object? IServiceProvider.GetService(Type serviceType) => typeof(Phi3ChatCompletionService);

            IReadOnlyDictionary<string, object?> IAIService.Attributes => throw new NotImplementedException();

            Task<IReadOnlyList<ChatMessageContent>> IChatCompletionService.GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            async IAsyncEnumerable<StreamingChatMessageContent> IChatCompletionService.GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                if (executionSettings is Phi4PromptExecutionSettings settings)
                {
                    var behavior = settings.ToolCallBehavior;
                }
                await foreach (var token in Answer(chatHistory, executionSettings, kernel, cancellationToken))
                {
                    yield return new StreamingChatMessageContent(AuthorRole.Assistant, token);
                }
            }

            Task<IReadOnlyList<TextContent>> ITextGenerationService.GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings, Kernel? kernel, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            async IAsyncEnumerable<StreamingTextContent> ITextGenerationService.GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings, Kernel? kernel, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var chat = new ChatHistory("You're a helpful AI assistent");
                chat.AddUserMessage(prompt);
                var filters = kernel.AutoFunctionInvocationFilters;
                var filters2 = kernel.FunctionInvocationFilters;
                await foreach (var token in Answer(chat, executionSettings, kernel, cancellationToken))
                    yield return new StreamingTextContent(token);
            }/*
        /*
        Task<IReadOnlyList<TextContent>> IImageToTextService.GetTextContentsAsync(ImageContent content, PromptExecutionSettings? executionSettings, Kernel? kernel, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/
            public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
            {
                throw new NotImplementedException();
            }

            public Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
            {
                throw new NotImplementedException();
            }
        }
    }
public class Phi4PromptExecutionSettings : PromptExecutionSettings
{
    [JsonConstructor]
    public Phi4PromptExecutionSettings() { }
    private ToolCallBehavior? _toolCallBehavior;

    [JsonPropertyName("tool_call_behavior")]
    public ToolCallBehavior? ToolCallBehavior
    {
        get => _toolCallBehavior;
        set
        {
            ThrowIfFrozen();
            _toolCallBehavior = value;
        }
    }
}
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
