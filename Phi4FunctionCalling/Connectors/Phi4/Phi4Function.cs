using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;

namespace Connectors.Phi4
{
    internal class Phi4Function
    {
        public required string Name { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //[System.ComponentModel.DefaultValue(null)]
        public string? Description { get; set; }
        public Dictionary<string, Phi4Parameter> Parameters { get; set; } = new Dictionary<string, Phi4Parameter>();
        public static Phi4Function FromKernelFunction(KernelFunctionMetadata function)
        {
            var f = new Phi4Function()
            {
                Name = function.Name,
                Description = string.IsNullOrEmpty(function.Description) ? null : function.Description,
                Parameters = new Dictionary<string, Phi4Parameter>()
            };
            foreach (var p in function.Parameters)
            {
                f.Parameters.Add(p.Name, new Phi4Parameter()
                {
                    //Name = p.Name,
                    Description = string.IsNullOrEmpty(p.Description) ? null : p.Description,
                    Type = p.ParameterType,
                    Default = p.DefaultValue
                });
            }
            return f;
        }
        public string ToJson()
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };
            string jsonString = JsonSerializer.Serialize(this, serializeOptions);
            return jsonString;
        }
    }
    internal class Phi4Parameter
    {
        //public required string Name { get; init; }
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        //[System.ComponentModel.DefaultValue(null)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(Phi4TypeConverter))]
        public Type? Type { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Default { get; set; }
    }
    internal sealed class Phi4TypeConverter : JsonConverter<Type>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(Type);
        }
        public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
            //return reader.GetString();
        }
        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
//            base.WriteAsPropertyName(writer, value, options);
//        }
//        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
//        {
            if (value as Type == typeof(string))
            {
                writer.WriteStringValue("str");
            }
            else if (value as Type == typeof(double))
            {
                writer.WriteStringValue("dbl");
            }
            else writer.WriteStringValue((value as Type).Name);
        }
    }
}
