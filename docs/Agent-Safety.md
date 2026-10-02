* An Agent must have boundaries.

Maximum number of iterations : You should fix the number of iterations of an AI Agent to avoid infinity agentic loop if the Agent didn't get the goal.
Maximum tool calls : To avoid DoS attacks if the agent many tools in the same time and reduce the cost of LLM (many tool calls == high tokens use)
Timeout : The timeout of each step in the planning of agent should be fixed (5 sec per step) to avoid bottleneck if a tool had a problem or latency cause.
CancellationToken: It should use cancellation token to liberate ressource when the use cancel the demand before tha agent complete its job.
Allowed Tools : The agent shouldn't be have the access of all tools and it shouldn't have the all freedom to do what it want because it can do something that destroy the system like deleting a table or a database or shut down a server. Besides, when reduce the available tools that the agent can use them, it help it to find the best way and make the best decision to acheive a task.
Maximum execution time : it should be fixed to respect the SLA of an application, if the demand depend much time it can block any next process to be executed.