//#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Microsoft.SemanticKernel;
using System.Text;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ImageToText;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.SemanticKernel.Connectors.Onnx;
using Phi4ConsoleApp.Connectors.Phi4;

// Path to huggingface model downloaded from https://huggingface.co/microsoft/Phi-4-multimodal-instruct-onnx
const string modelPath = @"e:\AI\Phi-4-multimodal-instruct-onnx\gpu\gpu-int4-rtn-block-32";

// Initialize the Semantic kernel
var kernelBuilder = Kernel.CreateBuilder();

// Use Semantic Kernel OpenAI API
//kernelBuilder = kernelBuilder
//    .AddAzureOpenAIChatCompletion(                        // We use Semantic Kernel OpenAI API
//        deploymentName: "gpt-4o",
//        apiKey: "API Key here",
//        endpoint: "https://yourendpointhere.openai.azure.com/"); // With Ollama OpenAI API endpoint

// Use Semantic Kernel's Onnx adapter:
// kernelBuilder = kernelBuilder.AddOnnxRuntimeGenAIChatCompletion("phi4", modelPath);

// Use my Phi4 adapter
kernelBuilder = kernelBuilder.AddPhi4AIChatCompletion("phi4", modelPath);

kernelBuilder.Plugins.AddFromType<Phi4ConsoleApp.Functions>();
// kernelBuilder.Plugins.AddFromType<HAFunctions>();
var kernel = kernelBuilder.Build();

// Create a new chat
Console.WriteLine("Loading model..."); 
var ai = kernel.GetRequiredService<IChatCompletionService>();
Console.WriteLine("Model loaded");

ChatHistory chat = new("You are a helpful assistant with some tools."); 
StringBuilder builder = new();

// User question & answer loop
while (true)
{
    Console.Write("Question: ");
    chat.AddUserMessage(Console.ReadLine()!);
    Debug.WriteLine("*********");
    builder.Clear();
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    OnnxRuntimeGenAIPromptExecutionSettings settings = new OnnxRuntimeGenAIPromptExecutionSettings();
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(null, true, new FunctionChoiceBehaviorOptions() { AllowConcurrentInvocation = true, AllowParallelCalls = true });
    // Get the AI response streamed back to the console
    var role = AuthorRole.Assistant;
    await foreach (var message in ai.GetStreamingChatMessageContentsAsync(chat, kernel: kernel, executionSettings: settings))
    {
        if(role != message.Role)
        {
            if (builder.Length > 0)
                chat.AddMessage(role, builder.ToString());
            builder.Clear();
            role = message.Role ?? AuthorRole.Assistant;
        }
        Console.Write(message);
        builder.Append(message.Content);
    }
    Console.WriteLine();
    if(builder.Length > 0)
        chat.AddMessage(role, builder.ToString());
    

    Console.WriteLine();
}
public static class Extensions {
    public static IKernelBuilder AddPhi4AIChatCompletion(this IKernelBuilder builder, string serviceId, string modelPath)
    {
        Func<IServiceProvider, object, Phi4ChatCompletionService> implementationFactory = 
            (IServiceProvider serviceProvider, object _) => new Phi4ChatCompletionService(modelPath);
        builder.Services.AddKeyedSingleton((object?)serviceId, (Func<IServiceProvider, object?, IChatCompletionService>)implementationFactory);
        builder.Services.AddKeyedSingleton((object?)serviceId, (Func<IServiceProvider, object?, ITextGenerationService>)implementationFactory);
        // builder.Services.AddKeyedSingleton((object?)serviceId, (Func<IServiceProvider, object?, IImageToTextService>)implementationFactory);
        return builder;
    }
}
//#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
