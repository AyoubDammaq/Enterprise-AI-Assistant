using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EnterpriseAIAssistant.Infrastructure.AI.Plugins
{
    public class CalculatorPlugin
    {
        [KernelFunction]
        [Description(
            "Evaluates a mathematical arithmetic expression. " +
            "Use this function ONLY when the user explicitly asks to calculate " +
            "a numerical mathematical expression. " +
            "Do NOT use this function for general questions, explanations, " +
            "programming concepts, or natural language."
        )]
        public static double Calculate(
            [Description("The first number.")]
            double left,

            [Description("The second number.")]
            double right,

            [Description(
                "The arithmetic operation to perform. " +
                "Supported values: add, subtract, multiply, divide.")]
            string operation)
        {
            Console.WriteLine("======================================");
            Console.WriteLine(">>> Calculate() WAS INVOKED <<<");
            Console.WriteLine("======================================");

            // Input validations
            if (double.IsNaN(left) || double.IsInfinity(left))
            {
                throw new ArgumentOutOfRangeException(nameof(left), "The left operand must be a finite number.");
            }

            if (double.IsNaN(right) || double.IsInfinity(right))
            {
                throw new ArgumentOutOfRangeException(nameof(right), "The right operand must be a finite number.");
            }

            if (string.IsNullOrWhiteSpace(operation))
            {
                throw new ArgumentException("The operation must be provided and cannot be empty.", nameof(operation));
            }

            var op = operation.Trim().ToLowerInvariant();

            var result = op switch
            {
                "add" => left + right,

                "subtract" => left - right,

                "multiply" => left * right,

                "divide" => right != 0
                    ? left / right
                    : throw new DivideByZeroException(
                        "Cannot divide by zero."),

                // Accept a few common synonyms/symbols
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" => right != 0 ? left / right : throw new DivideByZeroException("Cannot divide by zero."),

                _ => throw new ArgumentException(
                    $"Unsupported operation: {operation}",
                    nameof(operation))
            };

            Console.WriteLine($">>> CALCULATION: {left} {op} {right} = {result}");

            return result;
        }
    }
}
