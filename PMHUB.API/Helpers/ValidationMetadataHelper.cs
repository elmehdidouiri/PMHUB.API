using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PMHUB.Application.DTOs;

namespace PMHUB.API.Helpers
{
    public static class ValidationMetadataHelper
    {
        private static readonly Dictionary<Type, IReadOnlyCollection<RequiredFieldGroup>> ConditionalRequiredGroups = new()
        {
            [typeof(CreateFullProjectDto)] =
            [
                new RequiredFieldGroup(["departmentId", "departmentIds"], "At least one department is required.")
            ],
            [typeof(UpdateProjectDto)] =
            [
                new RequiredFieldGroup(["departmentId", "departmentIds"], "At least one department is required.")
            ]
        };

        public static Type? GetRequestBodyType(ActionContext context)
        {
            return context.ActionDescriptor.Parameters
                .OfType<ControllerParameterDescriptor>()
                .FirstOrDefault(parameter =>
                    parameter.ParameterInfo.GetCustomAttribute<FromBodyAttribute>() is not null)
                ?.ParameterType;
        }

        public static IReadOnlyCollection<RequiredField> GetRequiredFields(Type? requestBodyType)
        {
            if (requestBodyType is null)
            {
                return Array.Empty<RequiredField>();
            }

            return GetRequiredFields(requestBodyType, string.Empty, new HashSet<Type>()).ToList();
        }

        public static IReadOnlyCollection<RequiredFieldGroup> GetRequiredFieldGroups(Type? requestBodyType)
        {
            if (requestBodyType is null)
            {
                return Array.Empty<RequiredFieldGroup>();
            }

            return ConditionalRequiredGroups.TryGetValue(requestBodyType, out var groups)
                ? groups
                : Array.Empty<RequiredFieldGroup>();
        }

        public static IDictionary<string, string[]> NormalizeErrors(ModelStateDictionary modelState)
        {
            return modelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(error => new
                {
                    Field = NormalizeFieldName(kvp.Key),
                    Message = NormalizeErrorMessage(kvp.Key, error.ErrorMessage)
                }))
                .GroupBy(error => error.Field)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Message).Distinct().ToArray());
        }

        private static IEnumerable<RequiredField> GetRequiredFields(Type type, string prefix, ISet<Type> visitedTypes)
        {
            if (!visitedTypes.Add(type))
            {
                yield break;
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var jsonName = GetJsonName(property);
                var fieldPath = string.IsNullOrWhiteSpace(prefix) ? jsonName : $"{prefix}.{jsonName}";
                var required = property.GetCustomAttribute<RequiredAttribute>();

                if (required is not null)
                {
                    yield return new RequiredField(fieldPath, ErrorMessageTranslator.Translate(required.ErrorMessage));
                }

                var nestedType = GetNestedDtoType(property.PropertyType);
                if (nestedType is null)
                {
                    continue;
                }

                var nestedPrefix = typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string)
                    ? $"{fieldPath}[]"
                    : fieldPath;

                foreach (var nestedField in GetRequiredFields(nestedType, nestedPrefix, visitedTypes))
                {
                    yield return nestedField;
                }
            }

            visitedTypes.Remove(type);
        }

        private static Type? GetNestedDtoType(Type type)
        {
            var candidateType = type;

            if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
            {
                candidateType = type.IsGenericType
                    ? type.GetGenericArguments().FirstOrDefault()
                    : null;
            }

            if (candidateType is null || candidateType == typeof(string) || candidateType.IsPrimitive || candidateType.IsEnum)
            {
                return null;
            }

            candidateType = Nullable.GetUnderlyingType(candidateType) ?? candidateType;

            return candidateType.Namespace == typeof(CreateKpiDto).Namespace
                ? candidateType
                : null;
        }

        private static string NormalizeFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return "requestBody";
            }

            return fieldName == "$" || fieldName.Equals("dto", StringComparison.OrdinalIgnoreCase)
                ? "requestBody"
                : fieldName.TrimStart('$').TrimStart('.');
        }

        private static string NormalizeErrorMessage(string fieldName, string message)
        {
            if (fieldName.Equals("dto", StringComparison.OrdinalIgnoreCase) &&
                message.Contains("field is required", StringComparison.OrdinalIgnoreCase))
            {
                return "Request body is required.";
            }

            if (message.Contains("The JSON value could not be converted", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedField = NormalizeFieldName(fieldName);
                var expectedType = ResolveExpectedType(message);
                return $"{normalizedField} must be a valid {expectedType}.";
            }

            return ErrorMessageTranslator.Translate(message);
        }

        private static string ResolveExpectedType(string message)
        {
            if (message.Contains("System.Decimal", StringComparison.OrdinalIgnoreCase))
            {
                return "number";
            }

            if (message.Contains("System.Guid", StringComparison.OrdinalIgnoreCase))
            {
                return "GUID";
            }

            if (message.Contains("System.DateTime", StringComparison.OrdinalIgnoreCase))
            {
                return "date";
            }

            if (message.Contains("System.Int32", StringComparison.OrdinalIgnoreCase))
            {
                return "integer";
            }

            if (message.Contains("System.Boolean", StringComparison.OrdinalIgnoreCase))
            {
                return "boolean";
            }

            return "value";
        }

        private static string GetJsonName(PropertyInfo property)
        {
            return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name);
        }
    }

    public sealed record RequiredField(string Field, string? Message);

    public sealed record RequiredFieldGroup(IReadOnlyCollection<string> Fields, string Message);
}
