namespace EnterpriseAIAssistant.Infrastructure.AI.Plugins
{
    public class DateTimePlugin
    {
        public static string GetDateTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
