using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EnterpriseAIAssistant.Infrastructure.AI.Plugins
{
    public class DateTimePlugin
    {
        [KernelFunction]
        [Description("Returns the current date and time in the format 'yyyy-MM-dd - HH:mm:ss'.")]
        public static string GetDateTime()
        {
            Console.WriteLine("======================================");
            Console.WriteLine(">>> GetDateTime() WAS INVOKED <<<");
            Console.WriteLine("======================================");

            var result = FormatDateTime(DateTime.Now);

            Console.WriteLine($">>> FUNCTION RESULT: {result}");

            return result;
        }

        // Internal helper to allow deterministic unit testing
        internal static string FormatDateTime(DateTime now)
        {
            // Validation: ensure system clock is within a reasonable range
            if (now.Year < 1900 || now.Year > 3000)
            {
                throw new InvalidOperationException("System date/time is outside the supported range.");
            }

            return now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
