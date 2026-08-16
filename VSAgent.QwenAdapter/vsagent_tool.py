import json

from qwen_agent.tools.base import BaseTool


class VSAgentTool(BaseTool):

    def __init__(
        self,
        pipe_client,
        descriptor: dict
    ):
        self.name = descriptor["Name"]
        self.description = descriptor["Description"]

        self.parameters = self.normalize_schema(descriptor.get("Parameters"))

        self._pipe_client = pipe_client

        super().__init__()

    def call(self, params, **kwargs):
        parameters = self._verify_json_format_args(params)

        try:
            result = self._pipe_client.call_tool(
                self.name,
                parameters
            )

            return json.dumps(
                result,
                ensure_ascii=False
            )

        except Exception as ex:
            return json.dumps(
                {
                    "error": str(ex)
                },
                ensure_ascii=False
            )

    def normalize_schema(self, schema: dict | None) -> dict:
        if not schema:
            return {
                "type": "object",
                "properties": {},
                "required": []
            }

        properties = {}

        for name, prop in schema.get("Properties", {}).items():
            properties[name] = self.convert_property(prop)

        required = schema.get("Required", [])

        if required is None:
            required = []

        return {
            "type": "object",
            "properties": properties,
            "required": required
        }

    def convert_property(self, prop):
        result = {
            "type": prop.get("Type", "string")
        }

        description = prop.get("Description")
        if description:
            result["description"] = description

        items = prop.get("Items")
        if items:
            result["items"] = self.convert_property(items)

        return result