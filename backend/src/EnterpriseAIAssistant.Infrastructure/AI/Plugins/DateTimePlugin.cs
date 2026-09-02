using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EnterpriseAIAssistant.Infrastructure.AI.Plugins
{
    public class DateTimePlugin
    {
        [KernelFunction]
        [Description("Returns the current date and time in the format 'yyyy-MM-dd HH:mm:ss'.")]
        public static string GetDateTime()
        {
            Console.WriteLine("======================================");
            Console.WriteLine(">>> GetDateTime() WAS INVOKED <<<");
            Console.WriteLine("======================================");

            var result = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Console.WriteLine($">>> FUNCTION RESULT: {result}");

            return result;
        }
    }
}
