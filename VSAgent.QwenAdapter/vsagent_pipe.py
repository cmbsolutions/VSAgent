import json
import uuid


class VSAgentPipeClient:
    PIPE_PATH = r"\\.\pipe\VSAgent"

    def __init__(self):
        self._pipe = None

    def connect(self):
        if self._pipe is not None:
            return

        self._pipe = open(
            self.PIPE_PATH,
            "r+b",
            buffering=0
        )

    def close(self):
        if self._pipe is not None:
            self._pipe.close()
            self._pipe = None

    def call_tool(self, tool_name: str, parameters: dict | None = None):
        self.connect()

        request = {
            "Id": str(uuid.uuid4()),
            "Tool": tool_name,
            "Parameters": parameters or {}
        }

        request_json = json.dumps(
            request,
            ensure_ascii=False
        ) + "\n"

        self._pipe.write(
            request_json.encode("utf-8")
        )

        response_bytes = self._read_line()

        if not response_bytes:
            raise RuntimeError(
                "VSAgent closed the named pipe."
            )

        response = json.loads(
            response_bytes.decode("utf-8")
        )

        if not response.get("Success", False):
            raise RuntimeError(
                response.get(
                    "ErrorMessage",
                    f"VSAgent tool '{tool_name}' failed."
                )
            )

        return response.get("Result")

    def get_available_tools(self):
        return self.call_tool(
            "getAvailableTools",
            {}
        )

    def _read_line(self):
        data = bytearray()

        while True:
            ch = self._pipe.read(1)

            if not ch:
                break

            if ch == b"\n":
                break

            data.extend(ch)

        return bytes(data)

    def __enter__(self):
        self.connect()
        return self

    def __exit__(
        self,
        exc_type,
        exc_value,
        traceback
    ):
        self.close()