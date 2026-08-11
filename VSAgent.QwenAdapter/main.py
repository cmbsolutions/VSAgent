import json
from qwen_agent.agents import Assistant

from vsagent_pipe import VSAgentPipeClient
from vsagent_tool import VSAgentTool


def create_vsagent_tools(pipe_client):

    descriptors = pipe_client.get_available_tools()

    tools = []

    for descriptor in descriptors:

        if descriptor["Name"].lower() == "getavailabletools":
            continue

        print(
            f"Discovered tool: "
            f"{descriptor['Name']}"
        )

        tools.append(
            VSAgentTool(
                pipe_client,
                descriptor
            )
        )

    return tools


def main():

    pipe_client = VSAgentPipeClient()
    pipe_client.connect()

    tools = create_vsagent_tools(pipe_client)

    llm_cfg = {
        "model": "qwen3.6:35b",
        "model_server":
            "http://127.0.0.1:11434/v1",
        "api_key": "EMPTY",

        "generate_cfg": {
            "top_p": 0.8
        }
    }

    system_message = """
You are an AI software development assistant connected
to Visual Studio.

Use the supplied Visual Studio tools to inspect the
currently loaded solution.

Never assume project contents. Retrieve relevant
information using the tools before answering.
"""

    bot = Assistant(
        llm=llm_cfg,
        name="VSAgent",
        description=(
            "Visual Studio coding assistant"
        ),
        system_message=system_message,
        function_list=tools
    )

    messages = []

    print()
    print("VSAgent Qwen adapter connected.")
    print(
        f"{len(tools)} Visual Studio tools available."
    )
    print()
    print()
    print("Registered Qwen tools:")
    for tool in tools:
        print(tool.name)
        print(tool.description)
        print(tool.parameters)
        print()


    try:

        while True:

            query = input("You > ").strip()

            if not query:
                continue

            if query.lower() in {
                "exit",
                "quit"
            }:
                break

            messages.append({
                "role": "user",
                "content": query
            })

            final_response = None

            for response in bot.run(
                messages=messages
            ):
                final_response = response

            if final_response:

                messages.extend(
                    final_response
                )

                for message in final_response:

                    if message.get(
                        "role"
                    ) == "assistant":

                        print()
                        print(
                            "Qwen > "
                            + message.get(
                                "content",
                                ""
                            )
                        )

                print()

    finally:
        pipe_client.close()


if __name__ == "__main__":
    main()