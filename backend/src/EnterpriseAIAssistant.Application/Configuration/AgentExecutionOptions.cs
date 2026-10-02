namespace EnterpriseAIAssistant.Application.Configuration
{
    public class AgentExecutionOptions
    {
        public const string SectionName = "AgentExecution";

        public int MaxToolCalls { get; set; } = 3;
    }
}
