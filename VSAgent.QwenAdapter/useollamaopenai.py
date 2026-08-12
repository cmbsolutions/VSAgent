import json
from openai import OpenAI

from vsagent_pipe import VSAgentPipeClient


client = OpenAI(
    base_url="http://127.0.0.1:11434/v1",
    api_key="ollama"
)

pipe = VSAgentPipeClient()
pipe.connect()


def build_openai_tools():
    descriptors = pipe.get_available_tools()

    tools = []

    for descriptor in descriptors:
        if descriptor["Name"].lower() == "getavailabletools":
            continue

        schema = descriptor.get("Parameters") or {}

        tools.append({
            "type": "function",
            "function": {
                "name": descriptor["Name"],
                "description": descriptor["Description"],
                "parameters": {
                    "type": "object",
                    "properties": schema.get("Properties", {}),
                    "required": schema.get("Required", [])
                }
            }
        })

    return tools


tools = build_openai_tools()

messages = [
    {
        "role": "system",
        "content": """
You are an AI software development assistant connected
to a running Visual Studio instance.

Use the supplied Visual Studio tools to inspect the loaded
solution. Do not guess about source code that you have not
retrieved using the tools.
"""
    }
]

while True:

    user_input = input("You > ").strip()

    if user_input.lower() in {"exit", "quit"}:
        break

    if not user_input:
        continue

    messages.append({
        "role": "user",
        "content": user_input
    })

    while True:

        response = client.chat.completions.create(
            model="qwen3.6:35b",
            messages=messages,
            tools=tools,
            tool_choice="auto"
        )

        message = response.choices[0].message

        messages.append(message)

        if not message.tool_calls:
            print()
            print("Qwen >", message.content)
            print()
            break

        for tool_call in message.tool_calls:

            tool_name = tool_call.function.name

            arguments = json.loads(
                tool_call.function.arguments or "{}"
            )

            print(
                f"Tool > {tool_name}"
            )

            print(
                f"Args > {json.dumps(arguments, ensure_ascii=False)}"
            )

            try:
                result = pipe.call_tool(
                    tool_name,
                    arguments
                )

                print("Tool result received")

                tool_result = json.dumps(
                    result,
                    ensure_ascii=False
                )

            except Exception as ex:
                tool_result = json.dumps({
                    "error": str(ex)
                })

            messages.append({
                "role": "tool",
                "tool_call_id": tool_call.id,
                "content": tool_result
            })